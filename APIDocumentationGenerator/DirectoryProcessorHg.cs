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
		private readonly XMLFormatter m_XMLFormatter;

		public DirectoryProcessorHg()
		{
			m_XMLFormatter = new XMLFormatter(new SnippetConverter());
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

		#endregion

		public string XmlOutputDirectory
		{
			get { return Path.Combine(ExecutingAssemblyLocation,"../XMLSources"); }
		}

		public override void Process()
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

			Directory.SetCurrentDirectory(DirectoryUtil.RootDirName);
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

		
	}
}
