using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace UnderlyingModel
{

	internal static class AssemblyDataBuilderUtil
	{

		internal static void BuildAsmChildOfClass(AssemblyDataItem asmDataItem, ICollection<AssemblyDataItem> a)
		{
			ProcessNestedTypes(asmDataItem, a);
			ProcessMethods(asmDataItem, a);
			ProcessProperties(asmDataItem, a);
			ProcessFields(asmDataItem, a);
		}

		private static void ProcessNestedTypes(AssemblyDataItem asmDataItem, ICollection<AssemblyDataItem> a)
		{
			foreach (var nested in asmDataItem.cecilType.NestedTypes)
			{
				if (!nested.IsNestedPublic)
					continue;
				if (nested.IsClass)
				{
					var nestedClassAsm = new AssemblyDataItem(AssemblyType.Class, nested, asmDataItem);

					var memberName = nested.ToString().SimplifyTypesAndArrays();
					memberName = StringConvertUtils.LowerCaseNeedsUnderscore(memberName);

					nestedClassAsm.UniqueName = memberName;
					AddAssemblyDataItem(a, nestedClassAsm);
					BuildAsmChildOfClass(nestedClassAsm, a);
				}
				else
				{
					int k = 0;
				}
			}	
		}

		internal static void BuildAsmChildOfEnum(AssemblyDataItem asmDataItem, ICollection<AssemblyDataItem> a)
		{
			foreach (FieldDefinition field in asmDataItem.cecilType.Fields)
			{
				if (field.Name == "value__")
					continue;
				var enumAsm = new AssemblyDataItem(AssemblyType.EnumValue, field.DeclaringType, asmDataItem);
				
				var parentName = field.DeclaringType.ToString().SimplifyTypesAndArrays();
				var memberName = StringConvertUtils.LowerCaseNeedsUnderscore(field.Name);
				enumAsm.UniqueName = string.Format("{0}.{1}", parentName, memberName);
				AddAssemblyDataItem(a, enumAsm);
			}

		}

		private static void AddAssemblyDataItem(ICollection<AssemblyDataItem> a, AssemblyDataItem item)
		{
			item.Parent.AddChild(item);
			a.Add(item);
		}


		private static void ProcessFields(AssemblyDataItem asmDataItem, ICollection<AssemblyDataItem> a)
		{
			foreach (var field in asmDataItem.cecilType.Fields)
			{
				if (field.Attributes != FieldAttributes.Public)
					continue;
				var fieldAsm = new AssemblyDataItem(AssemblyType.Field, asmDataItem.cecilType, asmDataItem) {FInfo = field};

				var parentName = fieldAsm.Parent.ItemName();
				var memberName = MemberNameFromFieldDefinition(field);

				fieldAsm.UniqueName = string.Format("{0}.{1}", parentName, memberName);

				AddAssemblyDataItem(a, fieldAsm);
			}
		}


		private static void ProcessProperties(AssemblyDataItem asmDataItem, ICollection<AssemblyDataItem> a)
		{
			foreach (PropertyDefinition prop in asmDataItem.cecilType.Properties)
			{
				var propAsm = new AssemblyDataItem(AssemblyType.Property, asmDataItem.cecilType, asmDataItem) {PInfo = prop};
				var parentName = propAsm.Parent.ItemName();
				var memberName = MemberNameFromPropertyDefinition(prop);

				propAsm.UniqueName = string.Format("{0}.{1}", parentName, memberName);

				AddAssemblyDataItem(a, propAsm);
			}
		}

		private static void ProcessMethods(AssemblyDataItem asmDataItem, ICollection<AssemblyDataItem> a)
		{
			foreach (var method in asmDataItem.cecilType.Methods)
			{
				if (!method.IsPublic)
					continue;
				// we'll ignore property getter/setter
				if (method.Name.StartsWith("set") || method.Name.StartsWith("get"))
					continue;
				var methodAsm = new AssemblyDataItem(AssemblyType.Method, asmDataItem.cecilType, asmDataItem) {MInfo = method};
				var parentName = methodAsm.Parent.ItemName();
				var memberName = MemberNameFromMethodDefinition(method);

				methodAsm.UniqueName = string.Format("{0}.{1}", parentName, memberName);

				AddAssemblyDataItem(a, methodAsm);
			}
		}

		private static string MemberNameFromMethodDefinition(MethodDefinition methodInfo)
		{
			var sb = new StringBuilder();
			if (methodInfo.Name.Contains("ctor"))
				sb.Append("ctor");
			else if (methodInfo.Name.Contains("op_Implicit"))
			{
				sb.Append("implop_");
				var retString = methodInfo.ReturnType.ToString();
				sb.Append(retString.SimplifyTypesAndArrays());
			}
			else if (methodInfo.Name.Contains("op_"))
			{
				sb.Append(StringConvertUtils.ConvertOperatorFromAssembly(methodInfo.Name));
				sb.Replace("op_", "");
			}

			else
				sb.Append(methodInfo.Name);

			if (methodInfo.HasGenericParameters)
				foreach (var generic in methodInfo.GenericParameters)
				{
					sb.AppendFormat("_{0}_", generic.Name); 
				}
			foreach (var param in methodInfo.Parameters)
			{
				var paramString = param.ParameterType.ToString();
				sb.Append("_");
				sb.Append(paramString.SimplifyTypesAndArrays());
			}
			return sb.ToString();
		}

		private static string MemberNameFromFieldDefinition(FieldDefinition fieldDefinition)
		{
			return StringConvertUtils.LowerCaseNeedsUnderscore(fieldDefinition.Name);
		}

		private static string MemberNameFromPropertyDefinition(PropertyDefinition propDefinition)
		{
			return StringConvertUtils.LowerCaseNeedsUnderscore(propDefinition.Name);
		}
	}
}
