using System;
using System.Collections.Generic;
using System.Text;

namespace UnderlyingModel
{
	public static class StringConvertUtils
	{
		//note the spaces after each word!
		static readonly string[] Modifiers = { "private ", "internal ", "protected ", "public ", "new ", "delegate ", "abstract ", "const ", "static ", "virtual ", "override "};
		static readonly string[] ParamModifiers = { "ref ", "params ", "out " };

		public static string ConvertArgsToString(string args, char encloseStart = '(', char encloseEnd = ')', char separator = ',')
		{
			var ret = new StringBuilder();

			if (args.Trim().Length > 0)
			{
				var sb = new StringBuilder(args);

				string[] argList = sb.ToString().Split(',');
				argList = FixGenericArgList(argList);
				foreach (var oneArg in argList)									// foreach arg
				{
					string simplifiedArg = StripParamModifiers(oneArg.Trim());			// remove any method param		
					int indexOf = simplifiedArg.IndexOfAny(new char[]{' ', '\t'});
					if (indexOf > 0)
						simplifiedArg = simplifiedArg.Substring(0, indexOf).Trim();	// grab just type

					ret.AppendFormat(separator+"{0}", SimplifyTypes(simplifiedArg));
				}
				ret[0] = encloseStart;
				ret.Append (encloseEnd);
			}
			else
				ret.Append (""+encloseStart+encloseEnd);

			return ret.ToString();
		}

		private static string[] FixGenericArgList(string[] argList)
		{
			int startGenericIndex = -1, endGenericIndex = 0;
			for (int i = 0; i < argList.Length; i++)
			{
				if (argList[i].Contains("<"))
					startGenericIndex = i;
				if (argList[i].Contains(">"))
					endGenericIndex = i+1;
			}

			var newList = new List<string>();

			if (startGenericIndex >= 0)
			{
				for (int i = 0; i < startGenericIndex; i++)
					newList.Add(argList[i]);
				var sb = new StringBuilder();
				for (int i = startGenericIndex; i < endGenericIndex; i++)
				{
					sb.Append(argList[i].Trim());
					if (i != endGenericIndex - 1)
						sb.Append(",");
				}
				newList.Add(sb.ToString());
			}

			for (int i = endGenericIndex; i < argList.Length; i++)
				newList.Add(argList[i]);

			string[] retList = new string[newList.Count];
			for (int i = 0; i < newList.Count; i++)
				retList[i] = newList[i];
			return retList;
		}

		public static string ConvertOperator(string o)
		{
			var sb = new StringBuilder(o.TrimStart());
			sb.Replace("+", "Plus");
			sb.Replace("-", "Minus");
			sb.Replace("*", "Multiply");
			sb.Replace("/", "Divide");
			sb.Replace("!=", "NotEqual");
			sb.Replace("==", "Equal");
			return sb.ToString();
		}

		public static string ConvertOperatorFromAssembly(string o)
		{
			var sb = new StringBuilder(o.TrimStart());
			sb.Replace("Addition", "Plus");
			sb.Replace("Subtraction", "Minus");
			sb.Replace("Multiplication", "Multiply");
			sb.Replace("Division", "Divide");
			sb.Replace("Inequality", "NotEqual");
			sb.Replace("Equality", "Equal");
			sb.Replace("UnaryNegation", "Minus");
			return sb.ToString();
		}
		
		public static string AssemblyTypeNameForSignature (AssemblyType t)
		{
			switch (t)
			{
				case AssemblyType.Class: return "class";
				case AssemblyType.Constructor: return "function";
				case AssemblyType.Enum: return "enumeration";
				case AssemblyType.EnumValue: return "enumeration value";
				case AssemblyType.Field: return "var";
				case AssemblyType.Interface: return "interface";
				case AssemblyType.Method: return "function";
				case AssemblyType.Primitive: return "primitive type";
				case AssemblyType.Property: return "var";
				case AssemblyType.Struct: return "struct";
				case AssemblyType.Unknown: return "(not in assembly)";
				default: return "";
			}
		}
		
		
		//to manage name collision in Windows where test.txt is equivalent to Test.txt
		public static string LowerCaseNeedsUnderscore(string s)
		{
			if (s.StartsWith("ctor") || s.StartsWith("dtor") || s.StartsWith("implop") || s.StartsWith("op_"))
				return s;
			if (s.Length > 0 && Char.IsLower(s[0]))
				return String.Format("_{0}", s);
			return s;
		}

		// MyNameSpace.MyType<OtherNameSpace.Othertype> => MyType<Othertype>
		// MyNameSpace.MyType<OtherNameSpace.Othertype, OtherNameSpace.Type2> => MyType<Othertype,Type2>
		public static string SimplifyTypes (this string str)
		{
			var sb = new StringBuilder ();
			
			// Determine if type is a generic type
			int firstBraceIndex = str.IndexOf ('<');
			int lastBraceIndex = str.LastIndexOf ('>');
			if (firstBraceIndex > lastBraceIndex)
				throw new Exception (String.Format("Encountered type with > bad formatting < {0}", str));
			bool hasGenerics = firstBraceIndex > 0 && lastBraceIndex > 0;
			
			// Seperate type name from generic arguments
			string mainType = str;
			string genericParams = "";
			if (hasGenerics)
			{
				mainType = str.Substring (0, firstBraceIndex);
				genericParams = str.Substring (firstBraceIndex + 1, lastBraceIndex - firstBraceIndex - 1);
				
				// Handle generic arguments
				genericParams = SimplifyGenericParams (genericParams);
				
				// Strip '1 from type name (apparently `1 means  generic argument, `2 means two etc.)
				if (mainType.Length > 2 && mainType[mainType.Length-2] == '`')
					mainType = mainType.Substring (0, mainType.Length-2);
			}
			
			// Handle type name
			
			mainType = mainType.Replace ("0...", ""); // Cecil sometimes puts 0... inside array brackets - remove that
			
			mainType = mainType.Replace ("System.Object", "object"); // System.Object should be lower-case object
			mainType = mainType.Replace ("UnityEngine.Object", "Object"); // UnityEngine.Object should be upper-case Object
			
			// Nested types are seperated with / but should be seperated with dot.
			mainType = mainType.Replace ("/", ".");
			
			// Only keep part after the last dot
			// Important that this is done AFTER handling object vs. Object above.
			if (mainType.Length > 0)
			{
				string[] strs = mainType.Split ('.');
				mainType = mainType.Replace (mainType, strs[strs.Length-1]);
			}
			
			mainType = ReplaceAtomicTypes (mainType);
			
			// Assemble type name and generic arguments if present
			sb.Append (mainType);
			if (hasGenerics)
				sb.AppendFormat ("<{0}>", genericParams);
			
			return sb.ToString ();
		}
		
		public static string SimplifyGenericParams (string genericParamsCommaSeparated)
		{
			List<string> types = new List<string>();
			int currentParamStartIndex = 0;
			int nestedLevel = 0;
			for (int i=0; i<genericParamsCommaSeparated.Length; i++)
			{
				char c = genericParamsCommaSeparated[i];
				if (c == '<')
					nestedLevel++;
				if (c == '>')
					nestedLevel--;
				if (c == ',' && nestedLevel == 0)
				{
					types.Add (genericParamsCommaSeparated.Substring (currentParamStartIndex, i));
					currentParamStartIndex = i+1;
				}
			}
			types.Add (genericParamsCommaSeparated.Substring (currentParamStartIndex));
			
			for (int i=0; i<types.Count; i++)
				types[i] = types[i].SimplifyTypes ();
			return String.Join (",", types.ToArray ());
		}

		private static string ReplaceAtomicTypes(string s)
		{
			s = s.Replace ("&", ""); // out parameters have & after the type - remove it
			
			// Alphabetical order
			ReplaceStart (ref s, "Boolean", "bool");
			ReplaceStart (ref s, "Byte", "byte");
			ReplaceStart (ref s, "Char", "char");
			ReplaceStart (ref s, "Double", "double");
			ReplaceStart (ref s, "Float", "float");
			ReplaceStart (ref s, "Int16", "short");
			ReplaceStart (ref s, "Int32", "int");
			ReplaceStart (ref s, "Int64", "long");
			ReplaceStart (ref s, "SByte", "sbyte");
			ReplaceStart (ref s, "Single", "float");
			ReplaceStart (ref s, "String", "string");
			ReplaceStart (ref s, "UInt16", "ushort");
			ReplaceStart (ref s, "UInt32", "uint");
			ReplaceStart (ref s, "UInt64", "ulong");
			ReplaceStart (ref s, "Void", "void");

			return s;
		}

		

		private static string StripFromList(string s, IEnumerable<string> list)
		{
			var ret = s;
			foreach (var mod in list)
				ret = ret.Replace(mod, "");

			return ret.Trim();
		}

		private static string StripParamModifiers(string s)
		{
			return StripFromList(s, ParamModifiers);
		}

		internal static string StripCommonModifiers(string s)
		{
			return StripFromList(s, Modifiers);
		}

		private static void ReplaceStart(ref string self, string oldValue, string newValue)
		{
			if (self.StartsWith(oldValue))
				self = self.Replace(oldValue, newValue);
		}
	}
}
