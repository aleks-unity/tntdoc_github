using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using NUnit.Framework;

namespace UnderlyingModel
{
	public class UnmatchedRegexException : Exception
	{
		public UnmatchedRegexException(string fe)
		{
			FaultyExpression = fe;
		}
		public string FaultyExpression { private set; get; }
	}

	/// <summary>
	/// This class is used for generating unique IDs for members. 
	/// These IDs are then used for generating physical file paths.
	/// </summary>
	public class MemberNameGenerator
	{
		static private RegexMatcher[] _regexList;
		private Action<string> _unmatchedRegexHandler; //suppress / error / external
	   
		public MemberNameGenerator()
		{
			_unmatchedRegexHandler = s => Console.Error.WriteLine("unmatched regex: {0}", s);

			//Note the order of these is important. Rule of thumb: more specific ones first
			SyntaxMatching.InitRegexList(out _regexList);
		}

		public void SetUnmatchedRegexHandler(Action<string> handler)
		{
			_unmatchedRegexHandler = handler;
		}
		
		public static string GetSignatureID(string codeContent, TypeKind t)
		{
			return GetMemberOrSignatureID (codeContent, t, false);
		}
		
		//gets a directory-free, language-free filename for a member
		public static string GetMemberID(string codeContent, TypeKind t)
		{
			return GetMemberOrSignatureID (codeContent, t, true);
		}

		private static string GetMemberOrSignatureID(string codeContent, TypeKind t, bool simplifiedSignature)
		{
			switch (t)
			{
				case TypeKind.Class:
					string c1 = codeContent;
					if (c1.Contains(":"))
						c1 = c1.Remove(c1.IndexOf(':')).Trim();
					c1 = StringConvertUtils.StripCommonModifiers(c1);
					return c1;
				case TypeKind.Enum:
					var memberName = codeContent.Split(default(Char[]), StringSplitOptions.RemoveEmptyEntries)[0];
					memberName = memberName.TrimEnd(new[] {'='});

					memberName = StringConvertUtils.LowerCaseNeedsUnderscore(memberName);
					return memberName;
				case TypeKind.AutoProp:
					var memberName1 = codeContent.Split(' ')[1];
					memberName1 = StringConvertUtils.LowerCaseNeedsUnderscore(memberName1);
					return memberName1;
				case TypeKind.Struct:
					string s = codeContent;
					s = s.Replace("internal ", "");
					return s.Trim();
				case TypeKind.PureSignature:
					string sig = codeContent;
					var indexOfBrace = sig.IndexOf("{", StringComparison.Ordinal);
					if (indexOfBrace >= 0) // remove any body
						sig = sig.Remove(indexOfBrace).Trim();

					if (simplifiedSignature)
						return GetMemberNameFromPureSignature(sig);
					else
						return GetSignatureFromPureSignature(sig);
				default:
					return "Error";
			}
		}

		//covered by unit tests

		internal static string GetMemberNameFromPureSignature (string sig)
		{
			sig = StringConvertUtils.StripCommonModifiers(sig);
			sig = Regex.Replace (sig, "<.*>", "");
			
			RegexMatcher matcher;
			var found = TryMatchSignature(sig, out matcher);
			if (found)
			{
				string exp = matcher.GetSimplifiedMatch(sig);
				return (exp == "this") ? exp : StringConvertUtils.LowerCaseNeedsUnderscore(exp);
			}
			return "";
		}
		
		internal static string GetSignatureFromPureSignature (string sig)
		{
			sig = StringConvertUtils.StripCommonModifiers(sig);
			
			RegexMatcher matcher;
			var found = TryMatchSignature(sig, out matcher);
			if (found)
				return matcher.GetFullMatch(sig);
			return "";
		}

		//covered by unit tests
		public static bool TryMatchSignature(string sig, out RegexMatcher matcher)
		{
			if (_regexList == null)
				SyntaxMatching.InitRegexList(out _regexList);

			matcher = _regexList.FirstOrDefault(reg => reg.IsMatchSuccessful(sig));

			return matcher!=null;
		}

		public string GetNameFromPureSignatureWithChecks(string sig)
		{
			var ret = GetMemberNameFromPureSignature(sig);
			if (ret != "")
				return ret;

			_unmatchedRegexHandler(sig);

			if (sig.StartsWith("/*"))
				Console.WriteLine("multiline comments not supported");
			return "";
		}
		
		//used in unit tests
		public static string PureNameWithTypeStack(string codeContent, TypeKind t, Stack<string> typeStack)
		{
			return PureNameWithTypeStack(codeContent, t, typeStack, false);
		}

		public static string PureNameWithTypeStack(string codeContent, TypeKind t, Stack<string> typeStack, bool simplifiedSignature)
		{
			var memberName = simplifiedSignature? GetMemberID(codeContent, t) : GetSignatureID(codeContent, t);
			var fullName = PrependTypesFromExternalStack(memberName, typeStack);
			return fullName;
		}

		private static string PrependTypesFromExternalStack(string memberName, Stack<string> typeStack)
		{
			var arr = typeStack.ToArray();
			var sb = new StringBuilder();
			for (int i = arr.Length - 1; i >= 0; i--)
				sb.AppendFormat("{0}.", arr[i]);
			sb.Append(memberName);
			return sb.ToString();
		}

		internal static string SignatureNameFromDelegate(TypeDefinition methodInfo)
		{
			var sb = new StringBuilder();

			AppendName(methodInfo.Name, sb);
			foreach (var method in methodInfo.Methods)
			{
				if (method.Name == "Invoke")
					AppendParameters(method, sb);
			}
			return sb.ToString();
		}

		internal static string SignatureNameFromMethodDefinition(MethodDefinition methodInfo)
		{
			var sb = new StringBuilder ();
			var methodInfoName = methodInfo.Name;
			
			if (methodInfoName.StartsWith ("op_Implicit"))
			{
				sb.Append ("implop_");
				sb.Append (methodInfo.ReturnType.ToString ().SimplifyTypes ());
				AppendParameters (methodInfo, sb);
			}
			else
			{
				AppendName (methodInfoName, sb);
				if (methodInfo.HasGenericParameters)
					AppendGenerics (methodInfo, sb);
				AppendParameters (methodInfo, sb);
			}
			
			return sb.ToString();
		}

		internal static string SignatureNameFromPropertyDefinition(PropertyDefinition propInfo)
		{
			var sb = new StringBuilder();
			var propInfoName = propInfo.Name;

			if (propInfoName == "Item")
			{
				sb.Append("this");
				AppendParameters(propInfo, sb);
			}
			else
			{
				sb.Append(propInfoName);
				AppendParameters(propInfo, sb);
			}

			return sb.ToString();
		}

		private static void AppendParameters(PropertyDefinition propInfo, StringBuilder sb)
		{
			if (propInfo.Parameters.Count > 0)
				sb.AppendFormat("({0})", string.Join(",", propInfo.Parameters.Select(e => e.ParameterType.ToString().SimplifyTypes()).ToArray()));
		}

		private static void AppendParameters(MethodDefinition methodInfo, StringBuilder sb)
		{
			
			if (methodInfo.Parameters.Count > 0)
				sb.AppendFormat ("({0})", string.Join (",", methodInfo.Parameters.Select (e => e.ParameterType.ToString ().SimplifyTypes ()).ToArray ()));
			else
				sb.Append ("()");
		}

		private static void AppendGenerics(MethodDefinition methodInfo, StringBuilder sb)
		{
			sb.AppendFormat ("<{0}>", string.Join (",", methodInfo.GenericParameters.Select (e => e.ToString ().SimplifyTypes ()).ToArray ()));
		}

		private static void AppendName(string methodInfoName, StringBuilder sb)
		{
			if (methodInfoName.Equals(".ctor"))
				sb.Append("ctor");
			else if (methodInfoName.Contains("op_Implicit"))
			{
				Assert.Fail("We are handling this as a special case");
				//sb.Append("implop_");
				//sb.Append(retString.SimplifyTypesAndArrays());
			}
			else if (methodInfoName.Contains("op_"))
			{
				sb.Append(StringConvertUtils.ConvertOperatorFromAssembly(methodInfoName));
				sb.Replace("op_", "");
			}
			else
				sb.Append(methodInfoName);
		}
	}
}
