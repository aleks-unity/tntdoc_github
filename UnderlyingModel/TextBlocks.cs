using System.Text;
using UnderlyingModel;

namespace MemDoc
{
	public abstract class TextBlock
	{
		public string Text { set; get; }

	    protected TextBlock(string txt)
		{
			Text = txt;
		}

		public override string ToString()
		{
			return Text;
		}
	}
	
	public class DescriptionBlock : TextBlock
	{
		public DescriptionBlock(string txt) : base(txt) { }
	}
	
	public class ExampleBlock : TextBlock
	{
		public bool IsConvertExample { set; get; }
		public bool IsNoCheck { set; get; }
		 
		public ExampleBlock(string txt) : base (txt) {
			var lines = txt.SplitUnixLines();
			var exampleNoMarkup = new StringBuilder();

			//parse the lines to extract CONVERTEXAMPLE and NOCHECK flags
			foreach (var line in lines)
			{
				if (line.StartsWith(TxtToken.ConvertExample))
					IsConvertExample = true;
				if (line.StartsWith(TxtToken.NoCheck))
					IsNoCheck = true;
				exampleNoMarkup.AppendUnixLine(line);
			}
			Text = exampleNoMarkup.ToString();
		}
		
		public override string ToString()
		{
			var sb = new StringBuilder();
			if (IsConvertExample)
				sb.AppendUnixLine(TxtToken.ConvertExample);
			var nocheckString = IsNoCheck ? " "+TxtToken.NoCheck : "";
			//note that the last line of text will terminate in an endline 
			var textNoEndline = Text.TrimEndAndNewlines();
			sb.AppendFormat("{0}{1}\n{2}\n{3}", TxtToken.BeginEx, nocheckString, textNoEndline, TxtToken.EndEx);
			return sb.ToString();
		}
	}

}
