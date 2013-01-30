using NUnit.Framework;

namespace UnderlyingModel.Tests
{
	[TestFixture]
	public class SyntaxMatchingTests
	{

		[Test]
		public void BasicMethod()
		{
			const string memberSignature = "Touch GetSecondaryTouch (int index)";
			RegexMatcher matcher;

			bool found = MemberNameGenerator.TryMatchSignature(memberSignature, out matcher);

			Assert.AreEqual(found, true);
			Assert.AreEqual(matcher.Name, "METHOD");
		}

		[Test]
		public void BasicConstructor()
		{
			const string memberSignature = "AndroidJavaRunnable()";
			RegexMatcher matcher;

			bool found = MemberNameGenerator.TryMatchSignature(memberSignature, out matcher);

			Assert.AreEqual(found, true);
			Assert.AreEqual(matcher.Name, "CONSTRUCTOR");
		}

		

		[Test]
		public void EnumEntryAssignment()
		{
			const string memberSignature = "White = 0,";
			RegexMatcher matcher;

			bool found = MemberNameGenerator.TryMatchSignature(memberSignature, out matcher);

			Assert.AreEqual(found, true);
			Assert.AreEqual(matcher.Name, "ENUM_ENTRY");
		}

		[Test]
		public void SimplifyTypesTest2()
		{
			const string kMyType = "MyNameSpace.MyType<OtherNameSpace.Othertype>";

			string simplified = kMyType.SimplifyTypes();
			Assert.AreEqual("MyType<Othertype>", simplified);
		}

		[Test]
		public void SimplifyTypesTest3()
		{
			const string kMyType = "MyNameSpace.MyType<OtherNameSpace.Othertype, OtherNameSpace.Type2>";

			string simplified = kMyType.SimplifyTypes();
			Assert.AreEqual("MyType<Othertype,Type2>", simplified);
		}

		[Test]
		public void SimplifyTypesTest4()
		{
			const string kMyType =
				"System.Collections.Generic.List`1<System.Collections.Generic.KeyValuePair`2<UnityEditor.SnapEdge,UnityEditor.SnapEdge>>";
			string simplified = kMyType.SimplifyTypes();
			Assert.AreEqual("List<KeyValuePair<SnapEdge,SnapEdge>>", simplified);
		}
	}
}
