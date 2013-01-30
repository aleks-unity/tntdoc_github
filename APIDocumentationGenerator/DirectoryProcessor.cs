using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Mono.Cecil;
using UnderlyingModel;

namespace APIDocumentationGenerator
{
	abstract public class DirectoryProcessor
	{
		
		public abstract string ScriptApiOutputDirectory { get; }
		public abstract string DocumentationRoot { get; }
		public abstract string CombinedAssembliesDir { get; }
		public abstract string XmlOutputDirectory { get; }

		private readonly XMLFormatter m_XMLFormatter;
		public void Process()
		{
			DirectoryUtil.CreateDirectoryIfNeeded(ScriptApiOutputDirectory);

			var fname = Path.Combine(ExecutingAssemblyLocation, "../../layout/docs.css");
			if (File.Exists(fname))
			{
				File.Copy(fname, ScriptApiOutputDirectory + "/docs.css", true);
			}
			else
			{
				Console.WriteLine("{0} not found", fname);
			}

			Console.WriteLine("Deleting temp files...");

			DirectoryUtil.DeleteAllFiles(ScriptApiOutputDirectory);
			DirectoryUtil.DeleteAllFiles(XmlOutputDirectory);
			DirectoryUtil.CreateDirectoryIfNeeded(XmlOutputDirectory);

			Console.WriteLine("Deleting files done ...");
			var newDataItemProject = new NewDataItemProject();
			newDataItemProject.ReloadAllProjectData();

			var originalTime = DateTime.Now;

			List<MemberItem> membersWithAsm = newDataItemProject.GetAllMembers().Where(m => m.AnyHaveAsm).ToList();
			Parallel.ForEach(membersWithAsm, member => ProcessOneMember(member, originalTime));

			Parallel.ForEach(Directory.GetFiles(XmlOutputDirectory), XMLtoHTML);
		}

		private void ProcessOneMember(MemberItem member, DateTime originalTime)
		{
			DateTime startTime = DateTime.Now;

			Console.Write("Creating XML for {0}...", member.ItemName);
			MemToXML(member);
			DateTime endTime = DateTime.Now;
			double duration = (endTime - startTime).TotalSeconds;
			if (duration > 1.0f)
				Console.WriteLine(" duration {0} secs, time from Start = {1}", duration, (endTime - originalTime));
			else
				Console.WriteLine();
		}

		private void XMLtoHTML(string xmlFileName)
		{
			var shortXmlName = Path.GetFileName(xmlFileName);
			var shortHtmlName = shortXmlName.Replace(".xml", ".html");
			var htmlName = Path.Combine(ScriptApiOutputDirectory, shortHtmlName);
			Console.WriteLine("procucing HTML for {0}", htmlName);
			XslCompiledTransform myXslTrans = new XslCompiledTransform();
			try
			{
				myXslTrans.Load("Tools/UnityTxtParser/APIDocumentationGenerator/memberPage.xsl",
					new XsltSettings(enableDocumentFunction: true, enableScript: false),
					new XmlUrlResolver());

				myXslTrans.Transform(xmlFileName, htmlName);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private void MemToXML(MemberItem member)
		{
			var xml = m_XMLFormatter.FormattedXmlFor(member);
			var xmlFileName = Path.Combine(XmlOutputDirectory, member.ItemName + ".xml");

			File.WriteAllText(xmlFileName, xml.ToString());
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
