using NUnit.Framework;
using UnderlyingModel;

namespace APIDocumentationGenerator.Tests
{
	[TestFixture]
	public class HtmlNamingTests
	{
		NewDataItemProject m_NewDataItemProject = null;
		
		[TestFixtureSetUp]
		public void Init()
		{
			m_NewDataItemProject = new NewDataItemProject();
			m_NewDataItemProject.ReloadAllProjectData();
			DirectoryUtil.DeleteAllFiles("Generated");
		}
		
		[TestCase("DragAndDrop.AcceptDrag", "DragAndDrop.AcceptDrag.html")]
		[TestCase("DragAndDrop.GetGenericData", "DragAndDrop.GetGenericData.html")]
		[TestCase("DragAndDrop._paths", "DragAndDrop-paths.html")]
		[TestCase("DragAndDropVisualMode", "DragAndDropVisualMode.html")]
		[TestCase("DragAndDropVisualMode.Generic", "DragAndDropVisualMode.Generic.html")]
		[TestCase("GenericMenu.MenuFunction", "GenericMenu.MenuFunction.html")]
		[TestCase("Color._r", "Color-r.html")]
		[TestCase("Color.op_Plus", "Color-operator_add.html")]
		[TestCase("Color.ctor", "Color.Color.html")]
		[TestCase("Color.implop_Vector4(Color)", "Color-operator_Color.html")]
		public void HtmlNames(string memberName, string expectedHTMLName)
		{
			Assert.IsTrue(m_NewDataItemProject.ContainsMember(memberName));
			MemberItem memberItem = m_NewDataItemProject.GetMember(memberName);
			Assert.AreEqual(memberItem.HtmlName(), expectedHTMLName);
		}
	}
}
