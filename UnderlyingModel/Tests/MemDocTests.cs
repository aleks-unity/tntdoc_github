using MemDoc;
using NUnit.Framework;

namespace UnderlyingModel.Tests
{
	[TestFixture]
	public class MemDocTests
	{
		
		[Test]
		public void ExampleToString()
		{
			var example = new ExampleBlock(@"blah");
			const string kExpectedOut = @"BEGIN EX
blah
END EX";
			Assert.AreEqual(example.ToString(), kExpectedOut);
		}

		[Test]
		public void ExampleModifiedTextToString()
		{
			var example = new ExampleBlock(@"blah");
			example.Text = @"blah2";
			const string kExpectedOut = @"BEGIN EX
blah2
END EX";
			Assert.AreEqual(example.ToString(), kExpectedOut);
		}

		[Test]
		public void Example2LinesToString()
		{
			var example = new ExampleBlock(@"blah
blah");
			const string kExpectedOut = @"BEGIN EX
blah
blah
END EX";
			Assert.AreEqual(example.ToString(), kExpectedOut);
		}

		[Test]
		public void MemberWith2MeaningfulBlocks()
		{
			const string kInputString = @"<signature>
sig1
sig2
</signature>
Summary for first sigs
<signature>
sig3
sig4
</signature>
Summary for second sigs
";
			var derivedModel = new MemDocModel();
			derivedModel.ParseFromString(kInputString, true);
			Assert.AreEqual(2, derivedModel.SubSections.Count);
			Assert.AreEqual("Summary for first sigs", derivedModel.SubSections[0].Summary);
			Assert.AreEqual("Summary for second sigs", derivedModel.SubSections[1].Summary);
		}
	}
}
