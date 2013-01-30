using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using UnderlyingModel;

namespace APIDocumentationGenerator
{
	public class DirectoryProcessorHg : DirectoryProcessor
	{
		

		public DirectoryProcessorHg()
		{
			
		}

		#region overridden accessors
		public override string ScriptApiOutputDirectory
		{
			get { return DirectoryUtil.ScriptRefOutput + "/HTML"; }
		}
		public override string DocumentationRoot
		{
			get { return DirectoryUtil.MemberDocsDir; }
		}

		public override string CombinedAssembliesDir
		{
			get { return Path.Combine(DocumentationRoot, "build/CombinedAssemblies"); }
		}

		public override string XmlOutputDirectory
		{
			get { return Path.Combine(ExecutingAssemblyLocation, "../XMLSources"); }
		}

		#endregion

		
		

		

		
	}
}
