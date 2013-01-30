using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnderlyingModel;

namespace MemDoc
{
		public enum MemFileState
		{
			Description,
			Signatures,
			Example,
			ExampleMarkup,
			Param,
			Return
		}

		internal abstract class MiniBlockParser
		{
			protected MemberSubSection m_TheBlock;
			internal MemFileState m_CurrentState;
			protected bool m_IsSummaryFound = false;
			//the state we get into when we get out of another and don't know any better
			protected const MemFileState DefaultMemFileState = MemFileState.Description;


			protected MiniBlockParser(MemberSubSection theBlock)
			{
				m_TheBlock = theBlock;
			}

			protected void TerminateChunk(StringBuilder accumulator, MemFileState futureState)
			{
				TerminateChunk(accumulator, futureState, false, false);
			}

			protected void TerminateChunk(StringBuilder accumulator, MemFileState futureState, bool convertExample, bool noCheck)
			{
				string consumeAndTrim = accumulator.ConsumeAndTrimEndAndNewlines();
				if (consumeAndTrim.Length > 0)
				{
					switch (m_CurrentState)
					{
						case MemFileState.Signatures:
							{
								var sigs = consumeAndTrim.SplitUnixLines();
								foreach (var sig in sigs)
									m_TheBlock.SignatureList.Add(sig);
								break;
							}
						case MemFileState.Example:
							{
								var oneExample = new ExampleBlock(consumeAndTrim)
								{
									IsNoCheck = noCheck,
									IsConvertExample = convertExample
								};
								m_TheBlock.TextBlocks.Add(oneExample);
								break;
							}

						case MemFileState.Description:
							{
								if (!m_IsSummaryFound)
								{
									var lines = consumeAndTrim.SplitUnixLines();
									m_TheBlock.Summary = lines[0];
									consumeAndTrim = consumeAndTrim.Replace(m_TheBlock.Summary, "");
									consumeAndTrim = consumeAndTrim.TrimEndAndNewlines();
									m_IsSummaryFound = true;
									if (consumeAndTrim.Length == 0)
										break;
								}
								var oneText = new DescriptionBlock(consumeAndTrim);
								m_TheBlock.TextBlocks.Add(oneText);
								break;
							}
						case MemFileState.Param:
							{
								consumeAndTrim = Regex.Replace (consumeAndTrim, @"@param\s+", "");
								int index = consumeAndTrim.IndexOfAny(new char[] { ' ', '\t' });
								string name = consumeAndTrim.Substring(0, index).Trim();
								string doc = consumeAndTrim.Substring(index + 1).Trim();
								ParameterWithDoc paramDoc = m_TheBlock.GetOrCreateParameter(name);
								paramDoc.Doc = doc;
								break;
							}
						case MemFileState.Return:
							{
								consumeAndTrim = Regex.Replace (consumeAndTrim, @"@returns?\s+", "");
								m_TheBlock.GetOrCreateReturnDoc().Doc = consumeAndTrim;
								break;
							}
					}
				}
				m_CurrentState = futureState;
			}

			internal abstract void ProcessOneMeaningfulBlock(ref List<string> remainingLines);
		}

		
}
