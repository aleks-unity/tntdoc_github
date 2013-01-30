using System.Collections.Generic;
using System.Linq;
using UnderlyingModel;

namespace MemDoc
{
	public partial class MemDocModel
	{
		protected abstract class MiniParser
		{
			protected MemDocModel _memDocModel;

			protected MiniParser(MemDocModel memDocModel)
			{
				_memDocModel = memDocModel;
			}
			internal abstract void ParseString(string st);
			
			
		}

		protected sealed class MemMiniParser : MiniParser
		{
			public MemMiniParser(MemDocModel memDocModel)
				: base(memDocModel)
			{
			}

			internal override void ParseString(string st)
			{
				var remainingLines = new List<string>(st.SplitUnixLines());
				while (remainingLines.Any())
				{
					var block = new MemberSubSection();
					var miniBlockParser = new MemMiniBlockParser(block);
					miniBlockParser.ProcessOneMeaningfulBlock(ref remainingLines);
					//block.EnforcePunctuation ();
					_memDocModel.SubSections.Add(block);
				}
			}
		}

		protected sealed class TxtMiniParser : MiniParser
		{
			public TxtMiniParser(MemDocModel memDocModel)
				: base(memDocModel)
			{
			}

			internal override void ParseString(string st)
			{
				var remainingLines = new List<string>(st.SplitLines());
				while (remainingLines.Any())
				{
					var block = new MemberSubSection();
					var miniBlockParser = new TxtMiniBlockParser(block);
					miniBlockParser.ProcessOneMeaningfulBlock(ref remainingLines);
					//block.EnforcePunctuation ();
					_memDocModel.SubSections.Add(block);
				}
			}
		}
	}
}
