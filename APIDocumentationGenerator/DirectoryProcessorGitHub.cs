using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

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

		#endregion


		private static void EnsureDirectoryExists(string scriptApiOutputDirectory)
		{
			if (Directory.Exists(scriptApiOutputDirectory))
				return;

			EnsureDirectoryExists(Path.GetDirectoryName(scriptApiOutputDirectory));
			Directory.CreateDirectory(scriptApiOutputDirectory);
		}

		public override void Process()
		{
			var assemblies = new[]
				                 {
					                 AssemblyDefinition.ReadAssembly(DocumentationRoot + "/content/UnityEngine.dll"), 
									 AssemblyDefinition.ReadAssembly(DocumentationRoot + "/content/UnityEditor.dll")
				                 };

			var namespaces = new HashSet<string>();

			EnsureDirectoryExists(ScriptApiOutputDirectory);

			File.Copy(DocumentationRoot + "/layout/docs.css", ScriptApiOutputDirectory + "/docs.css", true);

			ProcessAssemblies(assemblies, namespaces);

			ProcessNamespaces(namespaces, assemblies);
		}

		
	}
}
