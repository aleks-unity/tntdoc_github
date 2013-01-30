using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnderlyingModel;

namespace MemDoc
{
	internal class MemMiniBlockParser : MiniBlockParser
	{
		protected internal MemMiniBlockParser(MemberSubSection block)
			: base(block)
		{
		}

		internal override void ProcessOneMeaningfulBlock(ref List<string> remainingLines)
		{
			var accumulator = new StringBuilder();

			bool convertExample = false;
			bool noCheck = false;
			int nSigBlocksFound = 0;

			m_CurrentState = DefaultMemFileState;
			bool inTxtTags = false;

			while (remainingLines.Any() && nSigBlocksFound < 2)
			{
				var line = remainingLines.First();
				var shortLine = line.TrimStart();

				if (shortLine.StartsWith(MemToken.SignatureOpen))
				{
					nSigBlocksFound++;
					//if we detect a second SignatureOpen token, this is the beginning of a new block, 
					//so we don't consume this line
					if (nSigBlocksFound == 2)
						continue;

					remainingLines.RemoveAt(0);
					TerminateChunk(accumulator, MemFileState.Signatures);
					continue;
				}
				remainingLines.RemoveAt(0);

				if (shortLine.StartsWith(MemToken.SignatureClose))
				{
					Assert.AreEqual(MemFileState.Signatures, m_CurrentState);
					TerminateChunk(accumulator, DefaultMemFileState);
					continue;
				}
				if (shortLine.StartsWith(MemToken.TxtTagOpen))
				{
					inTxtTags = true;
					continue;
				}
				if (shortLine.StartsWith(MemToken.TxtTagClose))
				{
					inTxtTags = false;
					continue;
				}
				if (shortLine.StartsWith(TxtToken.ConvertExample) || shortLine.StartsWith(TxtToken.NoCheck))
				{
					convertExample = shortLine.StartsWith(TxtToken.ConvertExample);
					noCheck = shortLine.StartsWith(TxtToken.NoCheck);
					Assert.IsFalse(convertExample && noCheck);
					if (m_CurrentState != MemFileState.Example)
						TerminateChunk(accumulator, MemFileState.ExampleMarkup, convertExample, noCheck);
					continue;
				}

				if (shortLine.StartsWith(TxtToken.BeginEx))
				{
					if (shortLine.Contains(TxtToken.NoCheck))
						noCheck = true;
					//Assert.AreNotEqual(MemFileState.Example, m_CurrentState);
					if (m_CurrentState == MemFileState.ExampleMarkup)
						m_CurrentState = MemFileState.Example;
					TerminateChunk(accumulator, MemFileState.Example);
					continue;
				}
				if (shortLine.StartsWith(TxtToken.EndEx))
				{
					Assert.AreEqual(MemFileState.Example, m_CurrentState);
					TerminateChunk(accumulator, DefaultMemFileState, convertExample, noCheck);
					convertExample = noCheck = false;
					continue;
				}
				if (shortLine.StartsWith(TxtToken.Param))
				{
					Assert.AreNotEqual(MemFileState.Example, m_CurrentState);
					Assert.AreNotEqual(MemFileState.ExampleMarkup, m_CurrentState);
					TerminateChunk(accumulator, MemFileState.Param);
				}
				if (shortLine.StartsWith(TxtToken.Return))
				{
					Assert.AreNotEqual(MemFileState.ExampleMarkup, m_CurrentState);
					TerminateChunk(accumulator, MemFileState.Return);
				}

				if (shortLine.StartsWith(TxtToken.CsNone))
				{
					m_TheBlock.IsCsNone = true;
					Assert.IsTrue(inTxtTags);
					continue;
				}
				if (shortLine.StartsWith(MemToken.Undoc))
				{
					m_TheBlock.IsUndoc = true;
					Assert.IsTrue(inTxtTags);
					continue;
				}
				accumulator.AppendUnixLine(line);
			}

			//make sure we're not in the middle of an example when we reached EOF or next signature marker
			Assert.AreNotEqual(MemFileState.Example, m_CurrentState, "unclosed example detected");
			Assert.AreNotEqual(MemFileState.Signatures, m_CurrentState, "unclosed signatures block detected");
			TerminateChunk(accumulator, DefaultMemFileState);
		}
	}
}
