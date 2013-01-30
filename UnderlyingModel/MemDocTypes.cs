using System.Collections.Generic;

namespace MemDoc
{
	public class ReturnWithDoc
	{
		public string Doc { set; get; }
		public string ReturnType = null;
		public bool HasDoc { get { return !string.IsNullOrEmpty (Doc); } }
		public bool HasAsm { get { return ReturnType != null; } }
		
		public ReturnWithDoc () { Doc = ""; }
		
		public bool Equals(string doc)
		{
			return Doc == doc;
		}

		//note: both @return and @returns are valid syntax, we had to pick one here
		public override string ToString()
		{
			return "@return " + Doc;
		}
	}

	public class ParameterWithDoc
	{
		public string Name { set; get; }
		public string Doc { set; get; }
		private List<string> m_Types = new List<string> ();
		public List<string> Types { get { return m_Types; } set { m_Types = value; } }
		public void AddType (string t)
		{
			if (!m_Types.Contains (t))
				m_Types.Add (t);
		}
		public string TypeString { get { return string.Join (" / ", m_Types.ToArray ()); } }
		public bool HasDoc { get { return !string.IsNullOrEmpty (Doc); } }
		public bool HasAsm { get { return m_Types.Count > 0; } }
		
		public ParameterWithDoc (string name)
		{
			Doc = "";
			Name = name;
		}
		
		public bool Equals(string name, string doc)
		{
			return Name == name && Doc == doc;
		}

		public override string ToString()
		{
			return "@param " + Name + " " + Doc;
		}
	}
	
}
