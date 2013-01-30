using System.Collections.Generic;

namespace UnityHtmlGenerator
{
    //for testing
    public class MyDataItem
    {
        public string m_name;
        public int m_value;
    }

    public class MyData
    {
        public string Title { get; set; }
        public List<MyDataItem> Items;
    }

    // enum for script languaged
    public enum ScriptLanguageID
    {
        JavaScript,
        CSharp,
        Boo
	};

    public class Example
    {
        public ScriptLanguageID m_languageID { get; set; }
        public string m_expression { get; set; }
    }

    public class TemplateData_Base
    {
        public string m_class { get; set; }
        public string m_member { get; set; }
        public string m_type { get; set; }
        public string m_description { get; set; }
		public List<Example> m_examples;
    }


    public class TemplateData_Class : TemplateData_Base
    {
        public List<TemplateData_Base> m_properties;
        public List<TemplateData_Base> m_functions;
        public List<TemplateData_Base> m_inheritedProperties;
        public List<TemplateData_Base> m_finheritedFunctions;

    }

}

