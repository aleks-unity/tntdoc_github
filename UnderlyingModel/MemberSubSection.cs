using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Collections.Generic;
using NUnit.Framework;
using UnderlyingModel;

namespace MemDoc
{
	//a collection of signatures which have a common description
	public class MemberSubSection
	{
		//the list of signatures that all the docs for this block apply to
		public List<string> SignatureList { set; get; }
		public List<SignatureEntry> SignatureEntryList { set; get; } 
	
		//union of all parameters for this meaningful block, derived from the signature list
		public List<ParameterWithDoc> Parameters { set; get; }
		public bool ContainsParameter (string name) { return Parameters.Any (e => e.Name == name); }
		public ParameterWithDoc GetParameter (string name) { return Parameters.FirstOrDefault (e => e.Name == name); }

		//the return type for this meaningful block (there must be only 1 return type in such a block
		public ReturnWithDoc ReturnDoc { set; get; }

		public string Summary { set; get; }
		
		//assuming each CSNONE function will have its own description and does not come in a list of overloaded functions, we can have 1 flag for the whole MeaningfulBlock
		public bool IsCsNone { get; set; }
		public bool IsUndoc { get; set; }
		public List<TextBlock> TextBlocks { set; get; }
		
		public MemberSubSection (string memberDoc) : this (memberDoc, false)
		{
			
		}

		public MemberSubSection (string memberDoc, bool isMemInput)
		{
			InitBasics ();
			var remainingLines = new List<string> (memberDoc.SplitUnixLines ());
			MiniBlockParser miniBlockParser;
			if (isMemInput)
				miniBlockParser = new MemMiniBlockParser (this);
			else
				miniBlockParser = new TxtMiniBlockParser (this);
			miniBlockParser.ProcessOneMeaningfulBlock (ref remainingLines);
			Assert.IsFalse (remainingLines.Any (), "memInput="+ memberDoc);
			//EnforcePunctuation ();
		}

		private void InitBasics ()
		{
			Parameters = new List<ParameterWithDoc> ();
			ReturnDoc = null;
			Summary = "";
			TextBlocks = new List<TextBlock> ();
			SignatureList = new List<string> ();
			SignatureEntryList = new List<SignatureEntry>();
			IsCsNone = false;
		}

		public MemberSubSection ()
		{
			InitBasics ();
		}

		public ParameterWithDoc GetOrCreateParameter (string name)
		{
			var p = Parameters.FirstOrDefault (m => m.Name == name);
			if (p == null)
			{
				p = new ParameterWithDoc (name);
				Parameters.Add (p);
			}
			return p;
		} 
		
		public ReturnWithDoc GetOrCreateReturnDoc ()
		{
			return ReturnDoc ?? (ReturnDoc = new ReturnWithDoc ());
		}

		//returns the string of .mem file output (with SignatureList and no slashes)
		public override string ToString ()
		{
			return ToString (false);
		}
		
		public string ToString (bool includeSignatureBlock)
		{
			if (IsEmpty () && !SignatureList.Any ())
				return string.Empty;
			
			var sb = new StringBuilder ();
			if (includeSignatureBlock)
				OutputSignatureList (sb);
			const bool prependSlashes = false;
			if (IsCsNone || IsUndoc)
			{
				sb.AppendUnixLine (MemToken.TxtTagOpen);
				if (IsCsNone)
					sb.AppendUnixLine (TxtToken.CsNone);
				if (IsUndoc)
					sb.AppendUnixLine (MemToken.Undoc);
				sb.AppendUnixLine (MemToken.TxtTagClose);
			}
			OutputForSummary (prependSlashes, sb);
			OutputForTextBlocks (prependSlashes, sb);
			OutputForParameters (prependSlashes, sb);
			OutputForReturnDoc (prependSlashes, sb);

			return sb.ToString ().Trim ();
		}

		//return the string of .txt file output
		private string ContentForReassembler (int numTabs = 0)
		{
			const bool kPrependSlashes = true;
			var sb = new StringBuilder ();
			OutputForSummary (kPrependSlashes, sb);
			OutputForTextBlocks (kPrependSlashes, sb);
			OutputForParameters (kPrependSlashes, sb);
			OutputForReturnDoc (kPrependSlashes, sb);

			var lines = sb.ToString().SplitUnixLines();

			var linesSb = new StringBuilder();
			foreach (var line in lines)
			{
				if (line.IsEmpty())
					continue;
				linesSb.AppendTabs(numTabs);
				linesSb.AppendUnixLine(line);
			}
			return linesSb.ToString ();
		}

		private void OutputSignatureList (StringBuilder sb)
		{
			sb.AppendUnixLine (MemToken.SignatureOpen);
			foreach (var sig in SignatureList)
				sb.AppendUnixLine (sig);
			sb.AppendUnixLine (MemToken.SignatureClose);
		}

		private void OutputForSummary (bool prependSlashes, StringBuilder sb)
		{
			if (IsUndoc)
			{
				if (prependSlashes)
				{
					sb.Append (TxtToken.DocComment);
					sb.Append (TxtToken.UndocNoSlashes);
				}
			}
			if (!Summary.IsEmpty ())
			{
				if (prependSlashes)
					sb.Append (TxtToken.DocComment);
				sb.AppendUnixLine (Summary);
			}
		}

		private void OutputForReturnDoc (bool prependSlashes, StringBuilder sb)
		{
			if (ReturnDoc != null && ReturnDoc.HasDoc)
			{
				if (prependSlashes)
					sb.Append (TxtToken.DocComment);

				sb.AppendUnixLine (ReturnDoc.ToString ());
			}
		}

		private void OutputForParameters (bool prependSlashes, StringBuilder sb)
		{
			foreach (var param in Parameters)
			{
				if (param.HasDoc)
				{
					if (prependSlashes)
						sb.Append (TxtToken.DocComment);

					sb.AppendUnixLine (param.ToString ());
				}
			}
		}

		private void OutputForTextBlocks (bool prependSlashes, StringBuilder sb)
		{
			foreach (var textOrExample in TextBlocks)
			{
				string str = textOrExample.ToString ().TrimEndAndNewlines ();
				if (!str.IsEmpty ())
				{
					if (textOrExample is DescriptionBlock && prependSlashes)
					{
						var multiLine = str.SplitUnixLines ();
						foreach (var line in multiLine)
						{
							sb.Append (TxtToken.DocComment);
							sb.AppendUnixLine (line);
						}
					}
					else
					{
						//note, we do not prepend slashes to examples regardless
						sb.AppendUnixLine (str);
					}
				}
			}
		}

		public void ProcessAsm (MemberItem member)
		{
			foreach (var sig in SignatureList)
			{
				// Don't include private signatures. We don't want the parameter list to include parameters used in private signatures only.
				SignatureEntry signature = member.GetSignature (sig);
				if (signature == null)
					continue;
				if (!signature.InAsm)
					continue;
				
				// Handle parameters
				System.Diagnostics.Debug.Assert (signature.Asm != null, "SignatureEntry.m_Asm for "+sig+" is null in ProcessAsm.");
				if (signature.Asm == null)
					throw new Exception ("SignatureEntry.m_Asm for "+sig+" is null in ProcessAsm.");
				System.Diagnostics.Debug.Assert (signature.Asm.ParamList != null, "SignatureEntry.m_Asm.m_ParamList for "+sig+" is null in ProcessAsm.");
				if (signature.Asm.ParamList == null)
					throw new Exception ("SignatureEntry.m_Asm.m_ParamList for "+sig+" is null in ProcessAsm.");
				foreach (ParamElement param in signature.Asm.ParamList)
				{
					ParameterWithDoc paramWithDoc = GetParameter (param.Name);
					if (paramWithDoc == null)
					{
						paramWithDoc = new ParameterWithDoc (param.Name);
						Parameters.Add (paramWithDoc);
					}
					paramWithDoc.AddType (param.Type);
				}
				
				// Handle return type
				if (ReturnDoc != null)
				{
					if (ReturnDoc.HasAsm)
					{
						if (ReturnDoc.ReturnType != signature.Asm.ReturnType)
							throw new Exception ("Different return types in the same MeaningfulBlock.");
					}
					else
						ReturnDoc.ReturnType = signature.Asm.ReturnType;
				}
			}
			
		}
		
		public bool IsEmpty ()
		{
			Assert.IsNotNull (Parameters);
			Assert.IsNotNull (TextBlocks);
			return
				Summary == "" &&
				(ReturnDoc == null || ReturnDoc.Doc == "") &&
				Parameters.All(e => e.Doc == "") &&
				TextBlocks.All(e => e.ToString () == "") &&
				!IsUndoc;
		}
		
		internal void SanitizeForEditing ()
		{
			// If there's no text or example, add one
			if (TextBlocks.Count == 0)
				TextBlocks.Add (new DescriptionBlock (""));
			
			// If the last text or example is not a text, add one
			if (! (TextBlocks[TextBlocks.Count-1] is DescriptionBlock))
				TextBlocks.Add (new DescriptionBlock (""));
			
			// Make sure there's a text before each example
			for (int i=TextBlocks.Count-1; i>=0; i--)
			{
				if (TextBlocks[i] is ExampleBlock && (i==0 || ! (TextBlocks[i-1] is DescriptionBlock)))
					TextBlocks.Insert (i, new DescriptionBlock (""));
			}
			
			// When there's two descriptions after each other, merge them together
			for (int i=TextBlocks.Count-2; i>=0; i--)
			{
				if (TextBlocks[i] is DescriptionBlock && TextBlocks[i+1] is DescriptionBlock)
				{
					TextBlocks[i].Text += "\n\n" + TextBlocks[i+1].Text;
					TextBlocks.RemoveAt (i+1);
				}
			}
		}
		
		public void EnforcePunctuation ()
		{
			foreach (var param in Parameters)
				param.Doc = EnforcePunctuation (param.Doc);
			if (ReturnDoc != null)
				ReturnDoc.Doc = EnforcePunctuation (ReturnDoc.Doc);
			Summary = EnforcePunctuation (Summary);
			foreach (TextBlock block in TextBlocks)
				if (block is DescriptionBlock)
					block.Text = EnforcePunctuation (block.Text);
		}
		
		public static string EnforcePunctuation (string str)
		{
			str = EnforcePunctuationStart (str);
			str = EnforcePunctuationEnd (str);
			return str;
		}
		
		private static string EnforcePunctuationStart (string str)
		{
			if (str == null)
				return str;
			str = str.TrimStart ();
			if (str == string.Empty)
				return str;
			char firstChar = str.First ();
			int replaceAt = 0;
			
			if (firstChar == '\'')
			{
				if (str.Length > 2 && str.StartsWith (@"''") && str.EndsWith (@"''"))
				{
					firstChar = str[2];
					replaceAt = 2;
				}
			}
			
			if (char.IsLower (firstChar) && (str.Length < replaceAt+2 || !char.IsUpper (str[replaceAt+1])))
				str = str.Substring (0, replaceAt) + char.ToUpper (firstChar) + str.Substring (replaceAt+1);
			return str;
		}
		
		private static string EnforcePunctuationEnd (string str)
		{
			if (str == null)
				return str;
			str = str.TrimEnd ();
			if (str == string.Empty)
				return str;
			char lastChar = str.Last ();
			int insertAt = str.Length;
			
			// If last character is paranthesis, look in second last char for punctuation.
			if (lastChar == ')')
			{
				if (str.EndsWith ("(RO)"))
				{
					lastChar = str[str.Length-6];
					insertAt = str.Length-5;
				}
				else if (str.EndsWith ("(Default)"))
				{
					lastChar = str[str.Length-11];
					insertAt = str.Length-10;
				}
				else
					lastChar = str[str.Length-2]; // char right before closing paranthesis
			}
			else if (lastChar == '\'')
			{
				if (str.EndsWith (@"''"))
				{
					// If string ends with '', then look if punctuation before it.
					lastChar = str[str.Length-3];
					int lastLineStartIndex = str.LastIndexOf ('\n') + 1;
					// If line started with '' too, put the punctuation inside formatting start and end, otherewise after it.
					if (str[lastLineStartIndex] == '\'')
						insertAt = str.Length-2;
				}
			}
			else if (lastChar == '\\')
			{
				if (str.EndsWith (@"\\"))
				{
					// If string ends with \\, then look if punctuation before it.
					lastChar = str[str.Length-3];
					insertAt = str.Length-2;
				}
			}
			
			if (lastChar == '.' || // Allow basic punctuation that ends a sentence.
				lastChar == ':' || // Allow basic punctuation that ends a sentence.
				lastChar == '?' || // Allow basic punctuation that ends a sentence.
				lastChar == '!' || // Allow basic punctuation that ends a sentence.
				lastChar == '>' || // Ignore if string ends with greater than sign, which is likely part of a html tag.
				lastChar == ';' || // Allow semi-colon in end. There's sometimes inline code that's not code examples per-se.
				lastChar == '_') // Ignore if string ends in underscore which is likely part of formatting.
				return str;
			
			str = str.Substring (0, insertAt) + "." + str.Substring (insertAt);
			return str;
		}

		public string GetUniqueSigContent (string uniqueSig, int numTabs = 0)
		{
			var sigListSize = SignatureList.Count;
			var index = SignatureList.FindIndex (m => m==uniqueSig);

			//uniqueSig is the LAST element of the list 
			if (index == sigListSize - 1)
			{
				string reassemblerContent = ContentForReassembler(numTabs);
				if (!reassemblerContent.IsEmpty())
					return reassemblerContent;
			}
			//uniqueSig not found
			else if (index < 0 || index >= sigListSize)
				return "";

			//uniqueSig found but is listonly or has no content
			var sb = new StringBuilder();
			sb.AppendTabs(numTabs);
			sb.AppendFormat("///{0}\n", TxtToken.ListOnlyNoSlashes);
			return sb.ToString();
		}

		static string FakeTranslateText (string sourceText, string fakeSuffix)
		{
			string[] wordByWord = sourceText.Split (new[]{' ', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries);
			var wordByWordTranslated = new string[wordByWord.Length];

			for (int j = 0; j < wordByWord.Length; j++)
			{
				var theWord = wordByWord[j];

				wordByWordTranslated[j] = String.Concat (theWord, fakeSuffix);
			}
			return String.Join (" ", wordByWordTranslated);
		}

		internal void FakeTranslate (string fakeSuffix)
		{
			Summary = FakeTranslateText (Summary, fakeSuffix);
			
			foreach (var block in TextBlocks)
			{
				if (block is DescriptionBlock)
					block.Text = FakeTranslateText (block.Text, fakeSuffix);
			}
			foreach (var param in Parameters)
			{
				if (param.HasDoc)
					param.Doc = FakeTranslateText (param.Doc, fakeSuffix);
			}
			if (ReturnDoc!=null && ReturnDoc.HasDoc)
				ReturnDoc.Doc = FakeTranslateText (ReturnDoc.Doc, fakeSuffix);
		}
	}
}
