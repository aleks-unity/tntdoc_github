using System;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace UnderlyingModel
{
	public partial class NewDataItemProject
	{
		

		private string[] m_DllLocations = new string[] { DirectoryUtil.EngineDllLocation, DirectoryUtil.EditorDllLocation };
		private ModuleDefinition[] m_AsmModules;
		private bool m_ScanForPrivateMembers = false;
		
		private void LoadAsmModules ()
		{
			m_AsmModules = new ModuleDefinition[m_DllLocations.Length];
			try
			{
				for (int i=0; i<m_AsmModules.Length; i++)
					m_AsmModules[i] = ModuleDefinition.ReadModule (m_DllLocations[i]);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine (ex);
			}
		}
			
		private void PopulateFromAsm ()
		{
			m_ScanForPrivateMembers = false;
			foreach (ModuleDefinition t in m_AsmModules)
				PopulateFromDLL (t);
		}

		private void SearchAsmForPrivateApiMatchingOrphanDocs ()
		{
			m_ScanForPrivateMembers = true;
			foreach (ModuleDefinition t in m_AsmModules)
				PopulateFromDLL (t);
		}

		private void PopulateFromDLL (ModuleDefinition moduleDefinition)
		{
			Collection<TypeDefinition> moduleTypes = null;
			
			try
			{
				moduleTypes = moduleDefinition.Types;
			}
			catch (Exception rte)
			{
				Console.Out.WriteLine ("ReflectionTypeLoadException:{0}", rte.Message);
			}

			if (moduleTypes == null)
				return;

			// Display all the types contained in the specified assembly.
			foreach (TypeDefinition objType in moduleTypes)
				PopulateFromTypeDefinition (objType);
		}

		private void PopulateFromTypeDefinition(TypeDefinition typeDefinition)
		{
			if (typeDefinition.IsPrimitive)
				return;
			bool documentIt = m_ScanForPrivateMembers || typeDefinition.IsPublic || typeDefinition.IsNestedPublic;
			if (!documentIt)
				return;

			if (IsDelegate(typeDefinition))
				PopulateDelegate(typeDefinition);
			if (typeDefinition.IsEnum)
				PopulateEnum (typeDefinition);
			else if ((typeDefinition.IsClass || typeDefinition.IsValueType || typeDefinition.IsInterface) && !typeDefinition.IsPrimitive)
				PopulateClassOrStruct (typeDefinition);
			else
				Console.Out.WriteLine ("Unknown Item: {0}", typeDefinition);
		}
		
		private void PopulateEnum (TypeDefinition objType)
		{
			AsmEntry asm = new AsmEntry (objType.Name, AssemblyType.Enum);
			var memberItem = HandleMemberAndSignature (GetTypeName (objType), objType.Name, asm, null);
			PopulateEnumChildren (objType, memberItem);
		}
		
		private void PopulateEnumChildren (TypeDefinition enumTypeDef, MemberItem parentItem)
		{
			foreach (var field in enumTypeDef.Fields)
			{
				if (field.Name == "value__")
					continue;
				
				AsmEntry asm = new AsmEntry (field.Name, AssemblyType.EnumValue);
				HandleMemberAndSignature (GetMemberName (field), field.Name, asm, parentItem);
			}
		}
		
		private void PopulateClassOrStruct (TypeDefinition objType)
		{
			AssemblyType asmType = AssemblyType.Unknown;

			//delegates are seen as Class by Cecil, but we don't handle them here
			if (IsDelegate(objType))
				PopulateDelegate(objType);
			else if (objType.IsClass)
				asmType = AssemblyType.Class;
			else if (objType.IsValueType)
				asmType = AssemblyType.Struct;
			else if (objType.IsInterface)
				asmType = AssemblyType.Interface;

			AsmEntry asm = new AsmEntry(objType.Name, asmType) {CecilType = objType};
			var memberItem = HandleMemberAndSignature (GetTypeName (objType), objType.Name, asm, null);
			PopulateClassOrStructChildren (objType, memberItem);
		}
		
		private void PopulateClassOrStructChildren (TypeDefinition objType, MemberItem parentItem)
		{
			PopulateNestedTypes (objType, parentItem);
			PopulateMethods (objType, parentItem);
			PopulateProperties (objType, parentItem);
			PopulateFields (objType, parentItem);
		}

		private void PopulateFields (TypeDefinition cecilType, MemberItem parentItem)
		{
			foreach (var field in cecilType.Fields)
			{
				bool documentIt = m_ScanForPrivateMembers || (field.Attributes & FieldAttributes.Public) > 0;
				if (!documentIt)
					continue;
				
				AsmEntry asm = new AsmEntry (field.Name, AssemblyType.Field)
					{
						 ReturnType = field.FieldType.ToString().SimplifyTypes()
					};
				HandleMemberAndSignature (GetMemberName (field), field.Name, asm, parentItem);
			}
		}

		private void PopulateProperties(TypeDefinition cecilType, MemberItem parentItem)
		{
			foreach (PropertyDefinition prop in cecilType.Properties)
			{
				if (!m_ScanForPrivateMembers && (prop.GetMethod != null && !prop.GetMethod.IsPublic))
					continue;

				var signatureName = MemberNameGenerator.SignatureNameFromPropertyDefinition(prop);
				AsmEntry asm = new AsmEntry(signatureName, AssemblyType.Property) {ReturnType = prop.PropertyType.ToString().SimplifyTypes()};
				HandleMemberAndSignature(GetMemberName(prop), signatureName, asm, parentItem);
			}
		}

		private void PopulateMethods(TypeDefinition cecilType, MemberItem parentItem)
		{
			foreach (var method in cecilType.Methods)
			{
				if (!m_ScanForPrivateMembers && !method.IsPublic)
					continue;
				
				if (method.IsGetter || method.IsSetter)
					continue;

				AssemblyType asmType = AssemblyType.Method;
	
				var methodName = method.Name;
				if (method.IsConstructor)
				{
					methodName = "constructor";
					asmType = AssemblyType.Constructor;
				}
				else if (methodName.StartsWith("op_Implicit"))
				{
					methodName = method.ReturnType.ToString().SimplifyTypes();
					asmType = AssemblyType.ImplOperator;
				}
				else if (methodName.StartsWith("op_"))
				{
					methodName = StringConvertUtils.ConvertOperatorFromAssembly(methodName).Substring(3); // Skip the op_ part
					asmType = AssemblyType.Operator;
				}

				string modifier = "";
				if (method.IsStatic)
					modifier = "static";
				
				var asmEntry = new AsmEntry (methodName, asmType, modifier);
				foreach (var param in method.Parameters)
				{
					string paramType = param.ParameterType.ToString ().SimplifyTypes ();
					bool paramAttribute = IsParams(param);
					string paramModifiers = paramAttribute? "params": ""; 
					var paramElement = new ParamElement (param.Name, paramType, paramModifiers);
					asmEntry.ParamList.Add (paramElement);
				}
				foreach (GenericParameter param in method.GenericParameters)
				{
					string paramType = param.GetElementType ().ToString ().SimplifyTypes ();
					asmEntry.GenericParamList.Add (paramType);
				}
				asmEntry.ReturnType = method.ReturnType.ToString ().SimplifyTypes ();
				
				var signatureName = MemberNameGenerator.SignatureNameFromMethodDefinition (method);
				HandleMemberAndSignature (GetMemberName (method), signatureName, asmEntry, parentItem);
			}
		}

		private static bool IsParams(ParameterDefinition param)
		{
			return param.HasCustomAttributes && param.CustomAttributes.Any(m => m.AttributeType.FullName.Contains("System.ParamArrayAttribute"));
		}

		private void PopulateNestedTypes(TypeDefinition cecilType, MemberItem parentItem)
		{
			foreach (var nested in cecilType.NestedTypes)
			{
				bool isDelegate = IsDelegate(nested);
				if (isDelegate)
				{
					//treat as a function, even though Cecil sees it as a class
					PopulateDelegate(nested);
				}
				else
				{
					PopulateFromTypeDefinition(nested);
				}
			}
		}

		private void PopulateDelegate(TypeDefinition del)
		{
			var asmEntry = new AsmEntry(del.Name, AssemblyType.Delegate);
	
			var signatureName = MemberNameGenerator.SignatureNameFromDelegate(del);
			HandleMemberAndSignature(GetTypeName(del), signatureName, asmEntry, null);
		}

		private static bool IsDelegate(TypeDefinition typedef)
		{
			return typedef.BaseType != null && typedef.BaseType.FullName == "System.MulticastDelegate";
		}

		private MemberItem HandleMemberAndSignature (string itemName, string signatureName, AsmEntry asmEntry, MemberItem parentItem)
		{
			asmEntry.Private = m_ScanForPrivateMembers;
			
			//HACK SPECIAL CASE 
			if (SkipSpecialCaseMember(itemName))
				return null;

			// Create member item if not already existing
			if (!m_MapNameToItem.ContainsKey (itemName))
			{
				if (!m_ScanForPrivateMembers)
					m_MapNameToItem[itemName] = new MemberItem (itemName, asmEntry.EntryType);
				else
					return null;
			}
			else if (!m_ScanForPrivateMembers && !m_MapNameToItem[itemName].MultipleSignaturesPossible)
				Console.WriteLine ("Same member name found twice for {0} (multiple signatures should not be possible for this type)", itemName);
			
			// Get member item
			MemberItem item = m_MapNameToItem[itemName];
			
			// Create signature
			if (!item.ContainsSignature (signatureName))
			{
				var sigEntry = new SignatureEntry (signatureName);
				sigEntry.AddFromAssembly (asmEntry);
				item.AddSignature (sigEntry);
			}
			else
			{
				if (!m_ScanForPrivateMembers)
					Console.WriteLine ("We should not see the same signature entry twice!! for {0} : {1}.", itemName, signatureName);
				else
				{
					var sigEntry = item.GetSignature (signatureName, true);
					if (!sigEntry.InAsm)
						sigEntry.AddFromAssembly (asmEntry);
				}
			}
			if (item!=null && parentItem!=null && !parentItem.ChildMembers.Contains(item))
				parentItem.ChildMembers.Add(item);
			return item; //the item we just populated, may or may not be used
		}
		
		private string GetTypeName (TypeReference objType)
		{
			string itemName = objType.Name;
			TypeReference pType = objType;
			while (pType.IsNested && pType.DeclaringType != null)
			{
				pType = pType.DeclaringType;
				itemName = pType.Name + "." + itemName;
			}
			return itemName;
		}

		private string GetMemberName (MemberReference memberReference)
		{
			var methodDefinition = memberReference as MethodDefinition;
			string memberName = "";
			if (methodDefinition != null)
			{
				if (methodDefinition.IsConstructor)
					memberName = "ctor";
				else if (methodDefinition.Name.StartsWith("op_Implicit"))
					memberName = "implop_" + methodDefinition.ReturnType.ToString().SimplifyTypes() + "(" +
					             methodDefinition.Parameters[0].ParameterType.ToString().SimplifyTypes() + ")";
				else if (methodDefinition.Name.StartsWith("op_"))
					memberName = StringConvertUtils.ConvertOperatorFromAssembly(memberReference.Name);
				else
					memberName = StringConvertUtils.LowerCaseNeedsUnderscore(memberReference.Name);
			}
			else
			{
				var memName = memberReference.Name;
				memberName = memName == "Item" ? "this" : StringConvertUtils.LowerCaseNeedsUnderscore(memName);
			}

			return GetTypeName (memberReference.DeclaringType) + "." + memberName;
		}
	}
}
