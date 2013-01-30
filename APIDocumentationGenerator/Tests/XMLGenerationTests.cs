using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using UnderlyingModel;

namespace APIDocumentationGenerator.Tests
{
	[TestFixture]
	public class XmlGenerationTests
	{
		NewDataItemProject m_NewDataItemProject = null;
		private SnippetConverter _converter;
		
		[TestFixtureSetUp]
		public void Init()
		{
			m_NewDataItemProject = new NewDataItemProject();
			m_NewDataItemProject.ReloadAllProjectData();
			DirectoryUtil.DeleteAllFiles("Generated");
			Directory.SetCurrentDirectory("../Tests");
			DirectoryUtil.CreateDirectoryIfNeeded("ActualXML");
			_converter = new SnippetConverter();
		}
		
		[TestCase("DragAndDrop.AcceptDrag", "DragAndDrop.AcceptDrag.xml")] //method (static, void return, no params)
		[TestCase("GameObject.SetActive", "GameObject.SetActive.xml")] //method (not static, void return, 1 param)
		[TestCase("Vector3.ToString", "Vector3.ToString.xml")] //method
		[TestCase("Vector3._x", "Vector3-x.xml")] //field
		[TestCase("Animator._animatePhysics", "Animator-animatePhysics.xml")] //property, boolean
		[TestCase("Vector3._sqrMagnitude", "Vector3.sqrMagnitude.xml")] //property, float
		[TestCase("ADError._code", "ADError-code.xml")] //property, float
		[TestCase("ADErrorCode", "ADErrorCode.xml")]
		[TestCase("GameObject._guiTexture", "GameObject-guiTexture.xml")] //property with a very simple example
		[TestCase("EditorGUILayout.IntSlider", "EditorGUILayout.IntSlider.xml")] //function with 2 member subsections, an image and a non-convert example
		[TestCase("Color._red", "Color-red.xml")] //property with a simple CONVERTEXAMPLE example
		public void VerifyAgainstExpectedXML(string memberName, string expectedXMLFile)
		{
			MemberItem member = m_NewDataItemProject.GetMember(memberName);

			Assert.IsNotNull(member, "could not find member for {0}", memberName);

			var xdoc = new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				new XElement("MemberItem",
				             new XAttribute("id", member.ItemName),
				             new XAttribute("kind", member.ItemType),
				             new XMLFormatter(_converter).FormattedXmlFor(member)));

			var expectedPath = Path.Combine("ExpectedXML", expectedXMLFile);
			var actualPath = Path.Combine("ActualXML", expectedXMLFile);
			Assert.IsTrue(File.Exists(expectedPath), "expected XML file not found");
			
			string actualXML = OutputXML(xdoc);
			File.WriteAllText(actualPath, actualXML);
		}

		private string OutputXML(XDocument xdoc)
		{
			string ret;
			using (var mem = new MemoryStream())
			{
				var settings = new XmlWriterSettings {NewLineChars = "\n", Encoding = Encoding.UTF8, Indent = true};
				XmlWriter writer = XmlWriter.Create(mem, settings);
				{
					xdoc.WriteTo(writer);
					writer.Flush();
					mem.Flush();
					mem.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(mem))
					{
						ret = reader.ReadToEnd();
					}
				}
			}
			return ret;
		}
	}
}
