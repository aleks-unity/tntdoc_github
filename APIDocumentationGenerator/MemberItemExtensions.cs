using System;
using System.Linq;
using Mono.Cecil;
using UnderlyingModel;

static internal class MemberItemExtensions
{
	public struct MemberItemNames
	{
		public string m_ClassName;
		public string m_NameWithoutClass;
	}

	public static MemberItemNames GetNames(this MemberItem self)
	{
		MemberItemNames names = new MemberItemNames();
		if (self.ItemType == AssemblyType.Class || self.ItemType == AssemblyType.Struct || self.ItemType == AssemblyType.Enum)
		{
			names.m_ClassName = self.ItemName;
			names.m_NameWithoutClass = "";
		}
		else 
		{
			var splitShit = self.ItemName.Split('.');
	
			names.m_ClassName = splitShit[0];
			names.m_NameWithoutClass = splitShit[1];
			if (names.m_NameWithoutClass.StartsWith("_"))
				names.m_NameWithoutClass = names.m_NameWithoutClass.Substring(1);
		}
		return names;
	}

	public static string HtmlName (this MemberItem self)
	{
		var names = self.GetNames();
		switch (self.ItemType)
		{
			case AssemblyType.EnumValue:
			case AssemblyType.Method:
				return string.Format("{0}.{1}.html", names.m_ClassName, names.m_NameWithoutClass);
			case AssemblyType.Field:
			case AssemblyType.Property:
				return string.Format("{0}-{1}.html", names.m_ClassName, names.m_NameWithoutClass);
			case AssemblyType.Constructor:
				return string.Format("{0}.{1}.html", names.m_ClassName, names.m_ClassName);
			case AssemblyType.Operator:
				return string.Format("{0}-operator_{1}", names.m_ClassName, names.m_NameWithoutClass);
			default:
				return string.Format("{0}.html", self.ItemName);
		}
	}
}
