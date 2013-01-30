using System.Linq;
using NUnit.Framework;

namespace UnderlyingModel.Tests
{
	[TestFixture]
	public partial class NewModelTests
	{
		NewDataItemProject m_NewDataItemProject = null;
		
		[TestFixtureSetUp]
		public void Init()
		{
			m_NewDataItemProject = new NewDataItemProject();
			m_NewDataItemProject.ReloadAllProjectData();
		}
		
		[Test]
		public void ProjectNotEmpty()
		{
			Assert.IsTrue(m_NewDataItemProject.ItemCount > 0);
		}
		

		[TestCase("PlayerSettings.Android")]
		[TestCase("PlayerSettings.Android._bundleVersionCode")]
		[TestCase("Color.ctor")]
		[TestCase("Color._g")]
		[TestCase("ADErrorCode")]
		[TestCase("ADErrorCode.ServerFailure")]
		[TestCase("AndroidJavaObject.ctor")]
		[TestCase("EditorGUI.DropShadowLabel")]
		[TestCase("EditorGUIUtility.SetIconSize")]
		[TestCase("AndroidJavaObject.Dispose")]
		[TestCase("Animation.AddClip")]
		[TestCase("AndroidInput.GetSecondaryTouch")]
		[TestCase("AccelerationEvent")]
		[TestCase("AccelerationEvent._deltaTime")]
		[TestCase("AndroidSdkVersions.AndroidApiLevel9")]
		[TestCase("iOSTargetOSVersion._iOS_4_0")]
		[TestCase("iOSTargetOSVersion")]
		[TestCase("AndroidSdkVersions")]
		[TestCase("LayerMask.implop_LayerMask(int)")]
		[TestCase("LayerMask.implop_int(LayerMask)")]
		[TestCase("Color32.implop_Color(Color32)")]
		[TestCase("ADBannerView.ctor")]
		[TestCase("X360Achievements.OnAward")] //public field
		[TestCase("X360Achievements.BasicDelegate")] //public delegate
		[TestCase("CharacterInfo._flipped")]
		[TestCase("AssetPostprocessor")] //Editor/Mono/AssetPipeline
		[TestCase("EditorStyles")] //Editor/Mono/GUI
		[TestCase("PivotMode")] //Editor/Mono/GUI
		[TestCase("ViewTool")] //Editor/Mono/GUI
		public void VerifyMemberPresent(string st)
		{
			Assert.IsTrue(m_NewDataItemProject.ContainsMember(st));
			Assert.IsTrue(m_NewDataItemProject.m_MapNameToItem[st].ItemName == st);
		}

		[TestCase("ADBannerView..ctor")] //incorrect constructor name
		[TestCase("Matrix4x4._op_Multiply")] //incorrect multiply
		[TestCase("iPhoneKeyboard.dtor")] //no mem files should exist for destructors		
		[TestCase("GUIView._void")]
		[TestCase("View._void")]
		[TestCase("ContainerWindow.ctor")]
		[TestCase("EditorGUIUtility._string")]
		public void VerifyMemberAbsent(string st)
		{
			Assert.IsFalse(m_NewDataItemProject.ContainsMember(st), "Member {0} shouldn't be in the assembly", st);
		}

		[TestCase("Quaternion.this", "Quaternion.Item")] //indexer
		[TestCase("Quaternion.this", "Quaternion._this(int)")] //incorrect indexer naming
		[TestCase("EditorLook.LikeInspector", "LikeInspector")] //internal enum
		[TestCase("X360AchievementType.Tournament", "Tournament")] // an Xbox enum member
		[TestCase("Application._persistentDataPath", "Application._endif")] //CSRAW #endif
		public void ThisButNotThat(string dis, string dat)
		{
			Assert.IsTrue(m_NewDataItemProject.ContainsMember(dis), "Member {0} SHOULD be in the assembly", dis);
			Assert.IsFalse(m_NewDataItemProject.ContainsMember(dat), "Member {0} shouldn't be in the assembly", dat);
		}


		[TestCase("AccelerationEvent", 1, 1)]
		[TestCase("AccelerationEvent._deltaTime", 1, 1)]
		[TestCase("AccelerationEvent._acceleration", 1, 1)]
		[TestCase("ActionScript", 1, 1)]
		[TestCase("ActionScript.Import", 1, 1)]
		[TestCase("ADBannerView.ctor", 1, 1)]
		[TestCase("ADErrorCode", 1, 1)]
		[TestCase("ADErrorCode.ServerFailure", 1, 1)]
		[TestCase("AndroidJavaObject.ctor", 2, 1)] //one of the constructors not present in the mem file
		[TestCase("AndroidJavaObject.Dispose", 1, 1)]
		[TestCase("AndroidInput.GetSecondaryTouch", 1, 1)]
		[TestCase("AndroidSdkVersions", 1, 1)]
		[TestCase("AndroidSdkVersions.AndroidApiLevel9", 1, 1)]
		[TestCase("Color.ctor", 2, 2)] // R,G,B and R,G,B,A
		[TestCase("Color._g", 1, 1)]
		[TestCase("Color32.implop_Color(Color32)", 1, 1)]
		[TestCase("iOSTargetOSVersion", 1, 1)]
		[TestCase("iOSTargetOSVersion._iOS_4_0", 1, 1)]
		[TestCase("EditorGUI.DropShadowLabel", 4, 4)]
		[TestCase("EditorGUIUtility.SetIconSize", 1, 1)]		
		[TestCase("PlayerSettings.Android", 1, 1)]
		[TestCase("PlayerSettings.Android._bundleVersionCode", 1, 1)]		
		[TestCase("Quaternion.this", 1, 1)]
		[TestCase("X360Achievements.OnAward", 1, 1)]
		[TestCase("X360Achievements.BasicDelegate", 1, 1)]
		[TestCase("AssetPostprocessor", 1, 1)] //Editor/Mono/AssetPipeline
		[TestCase("EditorStyles", 1, 1)] //Editor/Mono/GUI
		[TestCase("PivotMode", 1, 1)] //Editor/Mono/GUI
		[TestCase("ViewTool", 1, 1)] //Editor/Mono/GUI
		[TestCase("CustomPropertyDrawer", 1, 1)]
		public void VerifySignatureCount(string st, int expectedAsm, int expectedDoc)
		{
			Assert.IsTrue(m_NewDataItemProject.m_MapNameToItem[st].Signatures.Count > 0);
			//make sure it contains an asm entry and a doc entry
			var actualAsm = m_NewDataItemProject.NumAsmSignaturesForMember(st);
			Assert.AreEqual(expectedAsm, actualAsm, "expectedAsm={0}, actualAsm={1}", expectedAsm, actualAsm);
			var actualDoc = m_NewDataItemProject.NumDocSignatures(st);
			Assert.AreEqual(expectedDoc, actualDoc, "expectedDoc={0}, actualDoc={1}", expectedDoc, actualDoc);
		}

		[TestCase("AccelerationEvent")]
		[TestCase("AccelerationEvent._acceleration")]
		public void EverythingPresent(string st)
		{
			Assert.IsTrue(m_NewDataItemProject.ContainsMember(st));

			var item = m_NewDataItemProject.m_MapNameToItem[st];
			Assert.IsTrue(item.AnyHaveDoc);
			Assert.IsTrue(item.AllThatHaveAsmHaveDoc);
			Assert.IsTrue(item.AnyHaveAsm);
			Assert.IsTrue(item.AllThatHaveDocHaveAsm);
		}

		[Test]
		public void OutParameter()
		{
			const string kSt = "Collider.Raycast"; // has an out parameter
			Assert.IsTrue(m_NewDataItemProject.m_MapNameToItem.ContainsKey(kSt));
			Assert.AreEqual(1, m_NewDataItemProject.m_MapNameToItem[kSt].Signatures.Count);
			Assert.AreEqual(true, m_NewDataItemProject.m_MapNameToItem[kSt].Signatures[0].InBothAsmAndDoc);
		}


		//[TestCase("EditorGUIUtility.SetIconSize_Vector2")]
		//[TestCase("EditorGUI.DropShadowLabel_Rect_string")]
		//[TestCase("AndroidJavaObject.ctor_string_objectArray")]
		//[TestCase("Color.ctor_float_float_float")]
		//[TestCase("AndroidJavaObject.Set_FieldType__string_FieldType")]
		//[TestCase("Animation.AddClip_AnimationClip_string_int_int")]
		//[TestCase("AndroidInput.GetSecondaryTouch_int")]
		//[TestCase("Vector2.Divide_Vector2_float")]
		//[TestCase("Vector2.Multiply_Vector2_float")]
		//[TestCase("Vector2.Plus_Vector2_Vector2")]
		//[TestCase("Vector2.Minus_Vector2")]
		//[TestCase("Vector2.NotEqual_Vector2_Vector2")]
		//[TestCase("Vector2.Equal_Vector2_Vector2")]
		//[TestCase("Vector2.Minus_Vector2_Vector2")]
		//[TestCase("AndroidJavaObject.Set_FieldType__string_FieldType")]
		////[TestCase("ParticleSystem.isPlaying")] //SYNC_JOB_AUTO_PROP
		////[TestCase("ParticleSystem.loop")] //SYNC_JOB AUTO_PROP
		//[TestCase("ParticleSystem.GetParticles")] //SYNC_JOB CUSTOM

		//[TestCase("AccelerationEvent._m_TimeDelta")]
		//[TestCase("AndroidJavaObject._m_jobject")]
		//[TestCase("AndroidJavaObject.Set")]
		//[TestCase("AndroidJavaObject.Set_string_FieldType")]
		//[TestCase("iOSTargetOSVersion.iOS_4_0")]
		//public void VerifyAbsentFromAssembly(string st)
		//{
		//    bool contains = _assemblyDataItemSet.Items.Any(item => item.SimplifiedName.Equals(st));
		//    Assert.IsFalse(contains);
		//}
	}
}
