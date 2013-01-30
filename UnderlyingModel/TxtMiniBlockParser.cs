using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnderlyingModel;

namespace MemDoc
{
	internal class TxtMiniBlockParser : MiniBlockParser
	{
		public TxtMiniBlockParser(MemberSubSection block)
			: base(block)
		{
		}

		internal override void ProcessOneMeaningfulBlock(ref List<string> remainingLines)
		{
			var accumulator = new StringBuilder();

			bool convertExample = false;
			bool noCheck = false;
			m_CurrentState = DefaultMemFileState;

			while (remainingLines.Any())
			{
				var line = remainingLines.First();
				var shortLine = line.TrimStart();

				remainingLines.RemoveAt(0);

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
					Assert.AreNotEqual(MemFileState.ExampleMarkup, m_CurrentState);
					TerminateChunk(accumulator, MemFileState.Param);
				}
				if (shortLine.StartsWith(TxtToken.Return))
				{
					Assert.AreNotEqual(MemFileState.ExampleMarkup, m_CurrentState);
					TerminateChunk(accumulator, MemFileState.Return);
				}

				if (shortLine.StartsWith("*undoc"))
				{
					m_TheBlock.IsUndoc = true;
					continue;
				}

				if (shortLine.StartsWith(TxtToken.CsNone))
				{
					m_TheBlock.IsCsNone = true;
					line = line.Replace("CSNONE ", "");
				}
				accumulator.AppendUnixLine(line);
			}

			//make sure we're not in the middle of an example when we reached EOF or next signature marker
			Assert.AreNotEqual(MemFileState.Example, m_CurrentState);

			TerminateChunk(accumulator, DefaultMemFileState);
		}
	}
}
