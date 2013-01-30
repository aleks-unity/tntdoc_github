using NUnit.Framework;

namespace UnderlyingModel.Tests
{
	[TestFixture]
	public class OperatorNamingTests
	{	
		[Test]
		public void OperatorEquals()
		{
			const string inputSignature = "bool operator == (BoneWeight lhs, BoneWeight rhs)";
			const string expectedMemberName = "op_Equal";
			const string expectedSignatureName = "Equal(BoneWeight,BoneWeight)";
			VerifySignature(inputSignature, expectedMemberName, expectedSignatureName);
		}

		[Test]
		public void OperatorNotEquals()
		{
			const string inputSignature = "public static bool operator != (BoneWeight lhs, BoneWeight rhs)";
			const string expectedMemberName = "op_NotEqual";
			const string expectedSignatureName = "NotEqual(BoneWeight,BoneWeight)";
			VerifySignature(inputSignature, expectedMemberName, expectedSignatureName);
		}
		
		[Test]
		public void OperatorMultiply()
		{
			const string inputSignature = "static public Vector4 operator * (Matrix4x4 lhs, Vector4 v)";
			const string expectedMemberName = "op_Multiply";
			const string expectedSignatureName = "Multiply(Matrix4x4,Vector4)";
			VerifySignature(inputSignature, expectedMemberName, expectedSignatureName);
		}

		[Test]
		public void OperatorDivide()
		{
			const string inputSignature = "Vector2 operator / (Vector2 a, float d)";
			const string expectedMemberName = "op_Divide";
			const string expectedSignatureName = "Divide(Vector2,float)";
			VerifySignature(inputSignature, expectedMemberName, expectedSignatureName);
		}

		[Test]
		public void OperatorPlus()
		{
			const string inputSignature = "Vector2 operator + (Vector2 a, Vector2 b)";
			const string expectedMemberName = "op_Plus";
			const string expectedSignatureName = "Plus(Vector2,Vector2)";
			VerifySignature(inputSignature, expectedMemberName, expectedSignatureName);
		}

		[Test]
		public void OperatorMinus()
		{
			const string inputSignature = "Vector2 operator - (Vector2 a, Vector2 b)";
			const string expectedMemberName = "op_Minus";
			const string expectedSignatureName = "Minus(Vector2,Vector2)";
			VerifySignature(inputSignature, expectedMemberName, expectedSignatureName);
		}

		[Test]
		public void ImplicitOperatorMem()
		{
			const string inputSignature = "public static implicit operator Color32(Color c);";
			const string expectedMemberName = "implop_Color32(Color)";
			const string expectedSignatureName = "implop_Color32(Color)";
			VerifySignature(inputSignature, expectedMemberName, expectedSignatureName);
		}

		private void VerifySignature(string inputSignature, string expectedMemberName, string expectedSignatureName)
		{
			var actualMemberName = MemberNameGenerator.GetMemberNameFromPureSignature(inputSignature);
			var actualSignatureName = MemberNameGenerator.GetSignatureFromPureSignature(inputSignature);
			Assert.AreEqual(expectedMemberName, actualMemberName);
			Assert.AreEqual(expectedSignatureName, actualSignatureName);
		}
	}
}
