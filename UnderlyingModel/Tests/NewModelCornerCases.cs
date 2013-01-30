using System;
using System.Linq;
using NUnit.Framework;

namespace UnderlyingModel.Tests
{
	[TestFixture]
	public partial class NewModelTests
	{
		[Test]
		[Ignore("Known to be failing")]
		public void CornerCaseOptionalParams()
		{
			const string kSt = "Animation.AddClip";
			Assert.AreEqual(m_NewDataItemProject.NumDocSignatures(kSt), m_NewDataItemProject.NumAsmSignaturesForMember(kSt));
		}
		
		//1. make sure we store both overload for properties in one file
		//2. Convert Cecil's Item => this in the Asm
		[Test]
		public void CornerCaseAccessor()
		{
			const string kSt = "Matrix4x4.this";
			Assert.IsTrue(m_NewDataItemProject.m_MapNameToItem.ContainsKey(kSt));
			Assert.AreEqual(2, m_NewDataItemProject.m_MapNameToItem[kSt].DocModel.SignatureCount);
		}

		[Test]
		public void CornerCaseObjectImplop()
		{
			const string kSt = "Object.implop_bool(Object)";
			//make sure it contains an asm entry and a doc entry
			Assert.AreEqual(1, m_NewDataItemProject.NumAsmSignaturesForMember(kSt));
			Assert.AreEqual(1, m_NewDataItemProject.NumDocSignatures(kSt));
		}

		[Test]
		public void SameSectionDiffReturnTypes()
		{
			const string kSt = "EditorGUI.IntPopup";
			Assert.IsTrue(m_NewDataItemProject.m_MapNameToItem.ContainsKey(kSt));
			var memberItem = m_NewDataItemProject.GetMember(kSt);
			Assert.IsNull(memberItem.DocModel.ErrorMessage, memberItem.DocModel.ErrorMessage);
		}


		[Test]
		[Ignore("expensive test")]
		public void AllErrorMessages()
		{
			foreach (var st in m_NewDataItemProject.m_MapNameToItem.Keys)
			{
				var memberItem = m_NewDataItemProject.GetMember(st);
				Assert.IsNull(memberItem.DocModel.ErrorMessage, memberItem.DocModel.ErrorMessage);
			}
		}
	}
}
