using System.Collections.Generic;
using NUnit.Framework;

namespace UnderlyingModel.Tests
{
	[TestFixture]
	public class FileNamingTests
	{
		[TestCase("Vector3()", "ctor", "ctor()")] //simple constructor
		[TestCase("Vector3(float a, float b, float c)", "ctor", "ctor(float,float,float)")] //complex constructor
		[TestCase("~Vector3()", "dtor", "dtor()")]
		[TestCase("public WWW(string url, byte[] postData, Dictionary<string, string> headers )", "ctor", "ctor(string,byte[],Dictionary<string,string>)")]
		[TestCase("public WWW(string url, byte[] postData, Hashtable headers )", "ctor", "ctor(string,byte[],Hashtable)")]

		[TestCase("static int touchCountSecondary", "_touchCountSecondary", "touchCountSecondary")]
		[TestCase("public AndroidJavaObject(string className, params object[] args)", "ctor", "ctor(string,object[])")]
		[TestCase("public void Foo(string className, params object[] args)", "Foo", "Foo(string,object[])")]
		[TestCase("public void Fun(float[] args)", "Fun", "Fun(float[])")]
		[TestCase("public void Fun(float[])", "Fun", "Fun(float[])")]
		[TestCase("public void Fun(float)", "Fun", "Fun(float)")]
		[TestCase("void SetHeights (int xBase, int yBase, float[,] heights)", "SetHeights", "SetHeights(int,int,float[,])")]
		[TestCase("void SetAlphaMaps (int x, int y, float[,,] map)", "SetAlphaMaps", "SetAlphaMaps(int,int,float[,,])")]
		[TestCase("Touch GetSecondaryTouch (int index)", "GetSecondaryTouch", "GetSecondaryTouch(int)")] //basic function no modifiers
		[TestCase("static Touch GetSecondaryTouch (int index)", "GetSecondaryTouch", "GetSecondaryTouch(int)")] //static modifier
		[TestCase("static Touch GetSecondaryTouch (ref int index, out float f)", "GetSecondaryTouch", "GetSecondaryTouch(int,float)")] //out and ref params
		[TestCase("Touch GetSecondaryTouch ()", "GetSecondaryTouch", "GetSecondaryTouch()")]
		[TestCase("Touch GetSecondaryTouch ( )", "GetSecondaryTouch", "GetSecondaryTouch()")] //note the extra space
		
		[TestCase("Touch GetSecondaryTouch (Int32 index)", "GetSecondaryTouch", "GetSecondaryTouch(int)")]
		[TestCase("Touch GetSecondaryTouch (System.Float)", "GetSecondaryTouch", "GetSecondaryTouch(float)")]
		[TestCase("Touch GetSecondaryTouch (System.Double)", "GetSecondaryTouch", "GetSecondaryTouch(double)")]
		[TestCase("Touch GetSecondaryTouch (System.Single)", "GetSecondaryTouch", "GetSecondaryTouch(float)")]
		[TestCase("Object GetSecondaryTouch (System.Object index)", "GetSecondaryTouch", "GetSecondaryTouch(object)")]
		[TestCase("Object GetSecondaryTouch (UnityEngine.Object index)", "GetSecondaryTouch", "GetSecondaryTouch(Object)")]
		[TestCase("Touch GetSecondaryTouch (String[] arr)", "GetSecondaryTouch", "GetSecondaryTouch(string[])")]
		[TestCase("Touch GetSecondaryTouch (Object[] arr)", "GetSecondaryTouch", "GetSecondaryTouch(Object[])")]
		[TestCase("Touch GetSecondaryTouch (System.Byte)", "GetSecondaryTouch", "GetSecondaryTouch(byte)")]
		[TestCase("FieldType Get<FieldType>(string fieldName)", "Get", "Get<FieldType>(string)")]
		[TestCase("public delegate void AndroidJavaRunnable();", "AndroidJavaRunnable", "AndroidJavaRunnable()")]
		public void VerifyMemberAndSignatureName(string inputSignature, string expectedMemberName, string expectedSignatureName)
		{
			var actualMemberName = MemberNameGenerator.GetMemberNameFromPureSignature(inputSignature);
			var actualSignatureName = MemberNameGenerator.GetSignatureFromPureSignature(inputSignature);
			Assert.AreEqual(expectedMemberName, actualMemberName);
			Assert.AreEqual(expectedSignatureName, actualSignatureName);
		}

		[Test]
		public void AutoProp_MemberOnly()
		{
			const string codeContent = "Matrix4x4 worldToLocalMatrix GetWorldToLocalMatrix";
			const string expectedMemberName = "_worldToLocalMatrix";
			var actualMemberName = MemberNameGenerator.GetSignatureID(codeContent, TypeKind.AutoProp);
			Assert.AreEqual(expectedMemberName, actualMemberName);
		}
		
		[Test]
		public void AutoPropPtr_MemberOnly()
		{
			const string codeContent = "Transform parent GetParent SetParent";
			const string expectedMemberName = "_parent";
			var actualMemberName = MemberNameGenerator.GetSignatureID(codeContent, TypeKind.AutoProp);
			Assert.AreEqual(expectedMemberName, actualMemberName);
		}

		[Test]
		public void NameWithTab_SignatureOnly()
		{
			const string codeContent = "void					RemoveStateMachine(StateMachine	stateMachine)";
			const string expectedSignatureName = "RemoveStateMachine(StateMachine)";
			var actualSignatureName = MemberNameGenerator.GetSignatureFromPureSignature(codeContent);
			Assert.AreEqual(expectedSignatureName, actualSignatureName);
		}

		

		/// <summary>
		/// this is to make sure we strip the initializer
		/// </summary>
		[Test]
		public void FunctionWithObjectArrayAndInitizer()
		{
			const string inputSignature = "public bool Foo(string className, params object[] args) : this()";
			const string expectedMemberName = "Foo";
			const string expectedSignatureName = "Foo(string,object[])";
			var actualMemberName = MemberNameGenerator.GetMemberNameFromPureSignature(inputSignature);
			var actualSignatureName = MemberNameGenerator.GetSignatureFromPureSignature(inputSignature);
			Assert.AreEqual(expectedMemberName, actualMemberName);
			Assert.AreEqual(expectedSignatureName, actualSignatureName);
		}

		[Test]
		public void BasicFunctionWithType_SignatureOnly()
		{
			const string type = "TouchyFeely";
			const string inputSignature = "Touch GetSecondaryTouch (int index)";
			const string expectedSignatureName = "TouchyFeely.GetSecondaryTouch(int)";
			Stack<string> typeStack = TypeStackFromOneType(type);
			var actualSignatureName = MemberNameGenerator.PureNameWithTypeStack(inputSignature, TypeKind.PureSignature, typeStack); //GetMemberNameFromTypeAndSignature(type, inputSignature);
			Assert.AreEqual(expectedSignatureName, actualSignatureName);   
		}

		[Test]
		public void BasicFunctionWithEmptyType_SignatureOnly()
		{
			const string type = "";
			const string inputSignature = "Touch GetSecondaryTouch (int index)";
			const string expectedSignatureName = "GetSecondaryTouch(int)";
			Stack<string> typeStack = TypeStackFromOneType(type);
			var actualSignatureName = MemberNameGenerator.PureNameWithTypeStack(inputSignature, TypeKind.PureSignature, typeStack);
			Assert.AreEqual(expectedSignatureName, actualSignatureName);
		}

		[Test]
		[Ignore ("the problem may be that this is not a function we need to parse")]
		public void ComplexFunction_SignatureOnly()
		{
			const string type = "";
			const string inputSignature = "TrackedReferenceBaseToScriptingObject_GetAnimationEvent(self)->stateSender_animationState";
			const string expectedSignatureName = "GetSecondaryTouch_int";
			Stack<string> typeStack = TypeStackFromOneType(type);
			var actualSignatureName = MemberNameGenerator.PureNameWithTypeStack(inputSignature, TypeKind.PureSignature, typeStack);
			Assert.AreEqual(expectedSignatureName, actualSignatureName);
		}

		private static Stack<string> TypeStackFromOneType(string sType)
		{
			var typeStack = new Stack<string>();
			if (!sType.IsEmpty())
				typeStack.Push(sType);
			return typeStack;
		}
	}
}
