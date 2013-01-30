using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace UnderlyingModel
{
	public class ParamElement
	{
		public string Type { get; private set; }
		public string Name { get; private set; }
		public string Modifiers { get; private set; }
		
		public ParamElement (string name, string type, string mods = "")
		{
			Name = name;
			Type = type;
			Modifiers = mods;
		}

		public string Formatted ()
		{
			return string.Format("{0} : {1}", Name, Type);
		}
		
		public string FormattedHTML ()
		{
			return string.Format("<b>{0} : </b>{1}", Name, Type);
		}
	}

	public class AsmEntry
	{
		public string Name { get; private set; }
		public AssemblyType EntryType { get; private set; }
		public bool Private { get; internal set; }
		public List<ParamElement> ParamList { get; internal set; }
		public List<string> GenericParamList { get; internal set; }
		public string ReturnType { get; internal set; }
		public string Modifiers { get; internal set; }
		public TypeDefinition CecilType { get; set; }

		public AsmEntry (string name, AssemblyType type, string mods = "")
		{
			Name = name;
			EntryType = type;
			ParamList = new List<ParamElement> ();
			GenericParamList = new List<string> ();
			Modifiers = mods;
		}
		
		public string Formatted { get { return GetFormatted (false); } }
		public string FormattedHTML { get { return GetFormatted (true); } }
		
		public string GetFormatted (bool useHTML)
		{
			string begin = useHTML ? "<b>" : "";
			string end = useHTML ? "</b>" : "";
			string str = StringConvertUtils.AssemblyTypeNameForSignature (EntryType) + " " + begin + Name + end;
			if (EntryType == AssemblyType.Method || EntryType == AssemblyType.Constructor)
			{
				if (GenericParamList.Count > 0)
				{
					str += begin + "<";
					str += string.Join (", ", GenericParamList.ToArray ());
					str += ">" + end;
				}
				
				str += begin + " (" + end;
				string[] paramList = ParamList.Select (p => (useHTML ? p.FormattedHTML () : p.Formatted ())).ToArray ();
				str += string.Join (begin + ", " + end, paramList);
				str += begin + ")" + end;
			}
			if (ReturnType != null)
				str += begin + " : " + end + ReturnType;
			return str;
		}
	}

	public class SignatureEntry
	{
		public string Name { get; private set; }
		public AsmEntry Asm { get; private set; }
		public bool InAsm { get { return Asm != null; } }
		public bool InDoc { get; internal set; }
		public bool InDocTranslated { get; internal set; }
		public bool InBothAsmAndDoc { get { return InAsm && InDoc; } }
		public bool InAsmOrDoc { get { return InAsm || InDoc; } }
		public bool InBothDocAndTranslated { get { return InDoc && InDocTranslated; } }
		public bool InDocOrTranslated { get { return InDoc || InDocTranslated; } }
		
		public SignatureEntry (string name)
		{
			Name = name;
		}
		
		public void AddFromAssembly (AsmEntry entry)
		{
			Asm = entry;
		}
		
		public string Formatted
		{
			get
			{
				if (!InAsm)
					return Name.Replace ("(", " (").Replace (",", ", ");
				return Asm.Formatted;
			}
		}
		
		public string FormattedHTML
		{
			get
			{
				if (!InAsm)
					return Name.Replace ("(", " (").Replace (",", ", ");;
				return Asm.FormattedHTML;
			}
		}
	}
}
