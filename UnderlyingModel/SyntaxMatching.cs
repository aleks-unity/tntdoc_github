using System;
using System.Text;
using System.Text.RegularExpressions;

namespace UnderlyingModel
{
	internal static class SyntaxMatching
	{
		internal static readonly Regex IndexerMarker = new Regex(@"(.*)\sthis\s\[(.*)\]");
		internal static readonly Regex FieldAssignmentMarker = new Regex(@"^(\w+)\s(\w+)\s=");
		internal static readonly Regex EnumEntryAssignmentMarker = new Regex(@"(\w+)\s*=\s*(\w+)");
		internal static readonly Regex EnumEntryNoAssignment = new Regex(@"(\w+)\s*");
		internal static readonly Regex ConstMarker = new Regex(@"^const\s(\w+)\s(\w+)\s=");
		internal static readonly Regex ConstructorMarker = new Regex(@"^(\w+)\s*\((.*?)\)");
		internal static readonly Regex DestructorMarker = new Regex(@"^~(\w+)\s*\((.*?)\)");
		internal static readonly Regex MethodMarker = new Regex(@"^([\w\.]+(?:\[,*\])?)\s+(\w+)\s*(<([\w\.,]+)>)?\s*\((.*)\)");
		internal static readonly Regex FieldMarker = new Regex(@"^([\w\.]+(?:\[\])?)\s+(\w+)\s*");
		internal static readonly Regex OperatorMarker = new Regex(@"^(\w+)\soperator\s(.+)\s\((.*)\)");
		internal static readonly Regex ImplOperatorMarker = new Regex(@"^implicit\soperator\s(\w+)\s*\((.*)\)");
		
		internal static void InitRegexList(out RegexMatcher[] regexList)
		{
			regexList = new[]
						{
							new RegexMatcher(ImplOperatorMarker,
								m =>
									{
										string ret = m.Groups[1].ToString();
										string extras = StringConvertUtils.ConvertArgsToString(m.Groups[2].ToString());
										return String.Concat("implop_", ret, extras);
									}, "IMPLICIT OPERATOR"),

							new RegexMatcher(IndexerMarker,
								m =>
									{
										string extras = StringConvertUtils.ConvertArgsToString(m.Groups[2].ToString());
										return String.Concat("this", extras);
									}, "INDEXER",
								m => "this"),
							new RegexMatcher(MethodMarker,
								m =>
									{
										string extras = StringConvertUtils.ConvertArgsToString(m.Groups[5].ToString());
										if (m.Groups[4].ToString () != "")
											extras = StringConvertUtils.ConvertArgsToString(m.Groups[4].ToString(), '<', '>') + extras;
										return String.Concat(m.Groups[2].ToString(), extras);
									}, "METHOD", 
								m => m.Groups[2].ToString()),
							new RegexMatcher(OperatorMarker,
								m =>
									{
										string name = StringConvertUtils.ConvertOperator(m.Groups[2].ToString());
										string extras = StringConvertUtils.ConvertArgsToString(m.Groups[3].ToString());
										return String.Concat(name, extras);
									}, "OPERATOR",
								m => "op_" + StringConvertUtils.ConvertOperator(m.Groups[2].ToString())),
							new RegexMatcher(FieldAssignmentMarker, DefaultMatcher, "FIELD ASSIGNMENT"),
							new RegexMatcher(FieldMarker, DefaultMatcher, "FIELD"),
							
							new RegexMatcher(ConstMarker, DefaultMatcher, "CONST"),
							new RegexMatcher(ConstructorMarker, ConstructorDestructorMatcher, "CONSTRUCTOR", ConstructorDestructorMatcherSimplified),

							new RegexMatcher(DestructorMarker, ConstructorDestructorMatcher, "DESTRUCTOR", ConstructorDestructorMatcherSimplified),
							new RegexMatcher(EnumEntryAssignmentMarker,
								m => m.Groups[1].ToString(), "ENUM_ENTRY"),
							new RegexMatcher(EnumEntryNoAssignment,
								m => m.Groups[1].ToString(), "ENUM_ENTRY_NOASS"),
								
						};
		}

		//field, fieldassignment, const
		private static string DefaultMatcher(Match m)
		{
			return m.Groups[2].ToString();
		}

		
		private static string ConstructorDestructorMatcher(Match m)
		{
			string extras = StringConvertUtils.ConvertArgsToString(m.Groups[2].ToString());
			string front = m.Groups[0].ToString().StartsWith("~") ? "dtor" : "ctor";
			return String.Concat(front, extras);
		}
		
		private static string ConstructorDestructorMatcherSimplified(Match m)
		{
			return m.Groups[0].ToString().StartsWith("~") ? "dtor" : "ctor";
		}
	}

}
