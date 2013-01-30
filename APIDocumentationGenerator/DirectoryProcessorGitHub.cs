using System.IO;

namespace APIDocumentationGenerator
{
	public class DirectoryProcessorGitHub : DirectoryProcessor
	{
		#region overridden accessors
		public override string ScriptApiOutputDirectory
		{
			get { return DocumentationRoot + "output/api"; }
		}

		public override string DocumentationRoot
		{
			get { return Path.Combine(ExecutingAssemblyLocation, "..\\..\\..\\"); }
		}

		public override string CombinedAssembliesDir
		{
			get { return Path.Combine(DocumentationRoot, "content"); }
		}

		public override string XmlOutputDirectory
		{
			get { return Path.Combine(ExecutingAssemblyLocation, "../XMLSources"); }
		}

		#endregion
	}
}
