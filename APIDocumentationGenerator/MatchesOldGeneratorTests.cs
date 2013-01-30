using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Mono.Cecil;
using NUnit.Framework;
using UnderlyingModel;

namespace APIDocumentationGenerator
{
	[TestFixture]
	class MatchesOldGeneratorTests
	{
		[TestCase("UnityEngine", "Animator", "GetLayerWeight")]
		[TestCase("UnityEngine", "Animator", "IsControlled")]
		[TestCase("UnityEngine", "Animator", "applyRootMotion")]
		[TestCase("UnityEngine", "Animator", "IsInTransition")]
		[TestCase("UnityEngine", "Animator", "humanScale")]
		[TestCase("UnityEngine", "Animator", "ForceStateNormalizedTime")]
		[TestCase("UnityEngine", "Animator", "gravityWeight")]
		[TestCase("UnityEngine", "Animator", "deltaRotation")]
		[TestCase("UnityEngine", "Animator", "deltaPosition")]
		[TestCase("UnityEngine", "Animator", "SetIKPosition")]
		[TestCase("UnityEngine", "Animator", "SetIKPosition")]
		[TestCase("UnityEngine", "NavMeshHit", "distance")]
		public void RunTest(string namespaze, string typename, string membername)
		{
			var assembly = ModuleDefinition.ReadModule(DirectoryUtil.EngineDllLocation);
			var typeDef = assembly.GetType(namespaze + "." + typename);
			
			MemberReference memberReference = typeDef.Methods.SingleOrDefault(p => p.Name == membername) ??
			                          (MemberReference) typeDef.Properties.SingleOrDefault(p => p.Name == membername);

			var htmlfile = (memberReference is PropertyDefinition) ? typename + "-" + membername + ".html": typename + "." + membername + ".html";
			var oldfile = Path.Combine(DirectoryUtil.ScriptRefOutput, htmlfile);
			var oldContent = File.ReadAllText(oldfile);
		
			
			var newContent = new Program().GenerateFor(memberReference);
			StringAssertEqual(newContent, oldContent);
		}

		private static void StringAssertEqual(string produced, string expected)
		{
			produced = TrimWhiteSpaceBeforeNewLine(produced.WithUnixLineEndings());
			expected = TrimWhiteSpaceBeforeNewLine(expected.WithUnixLineEndings());
			if (produced != expected)
			{
				int first = FindFirstDifference(produced, expected);

				Console.WriteLine("Expected:");
				Console.WriteLine(SubStringFor(expected,first));
				Console.WriteLine();
				Console.WriteLine("Produced:");
				Console.WriteLine(SubStringFor(produced,first));
			}
			Assert.AreEqual(expected.WithUnixLineEndings(), produced);
		}

		private static string TrimWhiteSpaceBeforeNewLine(string str)
		{
			str = str.Replace(" \n", "\n");
			return str.Replace("\t</p>", "</p>");
		}

		private static string SubStringFor(string str, int first)
		{
			var startIndex = Math.Max(0, first - 300);
			return str.Substring(startIndex, 600);
		}

		private static int FindFirstDifference(string newContent, string oldContent)
		{
			for (int i = 0; i != newContent.Length; i++)
			{
				if (newContent[i] != oldContent[i])
					return i;
			}
			throw new ArgumentException();
		}
		
	}
}
