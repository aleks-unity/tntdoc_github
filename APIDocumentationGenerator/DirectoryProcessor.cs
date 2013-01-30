using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace APIDocumentationGenerator
{
	abstract public class DirectoryProcessor
	{
		public abstract void Process();
		public abstract string ScriptApiOutputDirectory { get; }
		public abstract string DocumentationRoot { get; }
		public abstract string CombinedAssembliesDir { get; }
	
		protected void ProcessNamespaces(HashSet<string> namespaces, AssemblyDefinition[] assemblies)
		{
			foreach (var namespaze in namespaces)
			{
				var result = new NamespacePageGenerator().GeneratePageFor(namespaze, assemblies);
				File.WriteAllText(ScriptApiOutputDirectory + "/" + UnityDocumentation.HtmlNameFor(namespaze), result);
			}
		}

		protected void ProcessAssemblies(AssemblyDefinition[] assemblies, HashSet<string> namespaces)
		{
			var allTypes = assemblies.Select(a => a.MainModule).SelectMany(m => m.Types);
			var documentedTypes = allTypes.Where(UnityDocumentation.IsDocumentedType);
			foreach (var t in documentedTypes)
			{
				Console.WriteLine("t:" + t.FullName);
				namespaces.Add(t.Namespace);

				WriteFileForType(assemblies, t);
			}
		}

		private void WriteFileForType(IEnumerable<AssemblyDefinition> assemblies, TypeDefinition t)
		{
			var stringWriter = new StringWriter();
			new TypePageGenerator().GeneratePageFor(t, assemblies, stringWriter);

			File.WriteAllText(OutputFileNameForType(t), stringWriter.ToString());
		}

		public string OutputFileNameForType(TypeDefinition t)
		{
			return ScriptApiOutputDirectory + "/" + UnityDocumentation.HtmlNameFor(t);
		}

		protected static string ExecutingAssemblyLocation
		{
			get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
		}

	}
}
