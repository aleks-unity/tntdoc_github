using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnderlyingModel;

namespace MemDoc
{
	public partial class MemDocModel
	{
		public List<MemberSubSection> SubSections { get; set; }
		private int m_BlockToWriteTo;

		// The type coming from assembly, consider also serializing this with the .mem file
		public AssemblyType AssemblyKind { get; set; }
		
		public LanguageUtil.ELanguage Language { get; internal set; }

		public string ErrorMessage { get; private set; }

		public MemDocModel ()
		{
			SubSections = new List<MemberSubSection> ();
			ErrorMessage = null;
			m_BlockToWriteTo = 0;
		}

		public MemDocModel (string text) : this ()
		{
			ParseFromString (text);
		}
		
		public void ProcessAsm (MemberItem member)
		{
			try 
			{
				foreach (MemberSubSection section in SubSections)
					section.ProcessAsm (member);
			}
			catch (Exception e)
			{
				ErrorMessage = e.Message + e.StackTrace;
			}
		}

		public void ParseFromString (string text, bool inputIsMem = true)
		{
			try
			{
				MiniParser miniParser;
				if (inputIsMem)
					miniParser = new MemMiniParser(this);
				else
					miniParser = new TxtMiniParser(this);
				miniParser.ParseString(text);
			}
			catch (Exception e)
			{
				ErrorMessage = e.Message + e.StackTrace;
			}
		}

		public void WriteToAnotherBlockNextTime ()
		{
			var block = new MemberSubSection ();
			SubSections.Add (block);
			m_BlockToWriteTo++;
		}

		internal List<string> SignatureListFromAllBlocks
		{
			get
			{
				var allSigs = new List<string> ();
				if (SubSections.Count == 0)
					return allSigs;
				foreach (var section in SubSections)
					allSigs.AddRange (section.SignatureList);
				return allSigs;
			}
		}

		internal MemberSubSection SubSectionOfSignature(string sig)
		{
			return SubSections.FirstOrDefault(section => section.SignatureList.Contains(sig));
		}

		public bool SignatureListContains (string st)
		{
			return SignatureListFromAllBlocks.Contains (st);
		}

		public int SignatureCount
		{
			get { return SignatureListFromAllBlocks.Count; }
		}
		
		public void AddSignatureToCurrentBlock (string sig)
		{
			if (m_BlockToWriteTo > SubSections.Count-1)
				SubSections.Add (new MemberSubSection ());
			SubSections[m_BlockToWriteTo].SignatureList.Add (sig);
		}

		public override string ToString ()
		{
			// Create signature block even if there are no signatures in cases where there
			// can be more than one signature. Otherwise the parameters / return type
			// docs and descriptions for the different sections get all mixed up.
			bool includeSignatureBlock = MemberItem.MultipleSignaturesPossibleForType (AssemblyKind);
			if (AssemblyKind == AssemblyType.Unknown && SignatureCount == 0)
				includeSignatureBlock = false;
			
			var sb = new StringBuilder ();
			foreach (var block in SubSections)
			{
				sb.Append (block.ToString (includeSignatureBlock));
				if (block != SubSections.Last ())
					sb.AppendUnixLine ();
			}

			return sb.ToString ();
		}
		
		public void SanitizeForEditing ()
		{
			foreach (MemberSubSection section in SubSections)
				section.SanitizeForEditing ();
		}
		
		public void EnforcePunctuation ()
		{
			foreach (MemberSubSection section in SubSections)
				section.EnforcePunctuation ();
		}
		
		public bool IsEmpty ()
		{
			return SubSections.All (e => e.IsEmpty ());
		}

		public string GetUniqueSigContent (string uniqueSig, int numTabs = 0)
		{
			foreach (var block in SubSections)
			{
				var content = block.GetUniqueSigContent (uniqueSig, numTabs);
				if (!content.IsEmpty ())
					return content;
			}
			Console.WriteLine("no docs found for signature {0}", uniqueSig);
			return "";
		}

		public void ProcessCurrentMeaningfulBlock (string accumulatedContent)
		{
			var lines = accumulatedContent.SplitUnixLines ().ToList ();
			var block = SubSections[m_BlockToWriteTo];
			var txtMiniBlockParser = new TxtMiniBlockParser (block);
			txtMiniBlockParser.ProcessOneMeaningfulBlock (ref lines);
		}

		public bool IsUndoc ()
		{
			return SubSections.Count > 0 && SubSections.All (s => s.IsUndoc);
		}

		public bool IsCsNone ()
		{
			return SubSections.Count > 0 && SubSections.All (s => s.IsCsNone);
		}

		public void FakeTranslate(string fakeSuffix)
		{
			foreach (var section in SubSections)
				section.FakeTranslate(fakeSuffix);
		}

		#region Functions useful for APIDocumentationGenerator

		public string Summary
		{
			get { return SubSections.Count > 0 ? SubSections[0].Summary : ""; }
		}

		public List<TextBlock> TextBlocks
		{
			get { return SubSections.Count > 0 ? SubSections[0].TextBlocks : new List<TextBlock>(); }
		}

		#endregion
	}
}
