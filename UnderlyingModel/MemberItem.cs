using System;
using System.Collections.Generic;
using System.Linq;
using MemDoc;
using System.IO;
using ELanguage = UnderlyingModel.LanguageUtil.ELanguage;

namespace UnderlyingModel
{
	public class MemberItem
	{
		public string ItemName { get; private set; }
		public AssemblyType ItemType { get; private set; }
		private readonly List<SignatureEntry> m_SignatureList = new List<SignatureEntry> ();
		public List<SignatureEntry> Signatures { get { return m_SignatureList.Where (e => (!e.InAsm || !e.Asm.Private)).ToList (); } }
		public List<MemberItem> ChildMembers { get; internal set; } 
		public MemDocModel DocModelTranslated { get; private set; }
		public MemDocModel DocModel { get; private set; }
		
		public static bool MultipleSignaturesPossibleForType (AssemblyType asmType)
		{
			return
				asmType == AssemblyType.Method ||
				asmType == AssemblyType.Operator ||
				asmType == AssemblyType.Constructor ||
				asmType == AssemblyType.Property ||
				asmType == AssemblyType.Unknown;
		}
		
		public bool MultipleSignaturesPossible { get { return MultipleSignaturesPossibleForType (ItemType); } }
		
		public MemberItem (string name, AssemblyType asmType)
		{
			ItemName = name;
			ItemType = asmType;
			ChildMembers = new List<MemberItem>();
		}
		
		public string GetFileName (ELanguage language = ELanguage.English)
		{
			return DirectoryWithLangUtil.GetPathFromMemberNameAndDir (ItemName, DirectoryUtil.MemberDocsDirFullPath, language);
		}
		
		public MemDocModel LoadDoc (string memFileContent, ELanguage language = ELanguage.English, bool updateMemberSignaturesAndFlags = true, bool assignDocToMemberItem = true)
		{
			bool translated = language != ELanguage.English;
			
			// Create new MemDocModel
			MemDocModel memDocModel = new MemDocModel (memFileContent) { Language = language, AssemblyKind = ItemType };
			
			InitializeDocSignaturesIfNone (memDocModel);
			
			if (assignDocToMemberItem)
			{
				if (translated)
					DocModelTranslated = memDocModel;
				else
					DocModel = memDocModel;
			}
			
			if (updateMemberSignaturesAndFlags)
			{
				// Remove old (possibly outdated) doc info from member signature list
				for (int i=m_SignatureList.Count-1; i>=0; i--)
				{
					if (translated)
						m_SignatureList[i].InDocTranslated = false;
					else
						m_SignatureList[i].InDoc = false;
					
					if (!m_SignatureList[i].InAsm && !m_SignatureList[i].InDoc && !m_SignatureList[i].InDocTranslated)
						m_SignatureList.RemoveAt (i);
				}


				// Populate member signature list with signatures from (updated) doc
				foreach (string signature in memDocModel.SignatureListFromAllBlocks)
				{
					if (!m_SignatureList.Any (e => (e.Name == signature)))
						m_SignatureList.Add (new SignatureEntry (signature));
					SignatureEntry signatureEntry = m_SignatureList.First (e => e.Name == signature);
					var subSection = memDocModel.SubSectionOfSignature(signature);
					if (subSection!=null)
						subSection.SignatureEntryList.Add(signatureEntry);
					if (translated)
						signatureEntry.InDocTranslated = true;
					else
						signatureEntry.InDoc = true;
					//if (signatureEntry.InAsm && signatureEntry.Asm.Private)
					//	System.Console.WriteLine ("Private documented signature "+signature+" in member "+ItemName);
				}
				
				UpdateFlags ();
			}
			
			// Add assembly information to MemberDocModel
			memDocModel.ProcessAsm (this);
			
			return memDocModel;
		}
		
		public void LoadDoc (ELanguage language = ELanguage.English)
		{
			string docContent = string.Empty;
			string fileName = string.Empty;
			try
			{
				fileName = GetFileName(language);
				docContent = File.Exists(fileName) ? File.ReadAllText (fileName) : string.Empty;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Attempting to load doc for {0}, exception {1}", fileName, ex);
			}
			LoadDoc (docContent, language);
		}
		
		public void SaveDoc (bool translated = false)
		{
			MemDocModel doc = translated ? DocModelTranslated : DocModel;
			var writer = new StreamWriter (GetFileName (doc.Language));
			writer.Write (doc.ToString ());
			writer.Close ();	
		}
		
		public void DeleteDoc (bool translated = false)
		{
			MemDocModel doc = translated ? DocModelTranslated : DocModel;
			string fileName = GetFileName (doc.Language);
			if (File.Exists (fileName))
				File.Delete (fileName);
		}
		
		private void InitializeDocSignaturesIfNone (MemDocModel memDocModel)
		{
			// If member can only have one signature and it doesn't have it, add it based on asm signature
			if (memDocModel.SignatureCount == 0 && memDocModel.SubSections.Count <= 1)
			{
				List<SignatureEntry> sigEntries = Signatures.Where (e => e.InAsm).OrderBy (e => e.Asm.ReturnType).ToList ();
				if (sigEntries.Any ())
				{
					string prevReturnType = sigEntries[0].Asm.ReturnType ?? "";
					int section = 0;
					foreach (SignatureEntry sigEntry in sigEntries)
					{
						string returnType = sigEntry.Asm.ReturnType ?? "";
						if (returnType != prevReturnType)
						{
							prevReturnType = returnType;
							section++;
						}
						if (memDocModel.SubSections.Count <= section)
							memDocModel.SubSections.Add (new MemberSubSection ());
						memDocModel.SubSections[section].SignatureList.Add (sigEntry.Name);
						memDocModel.SubSections[section].SignatureEntryList.Add(sigEntry);
					}
				}
			}
		}
		
		public bool AnyHaveAsm { get; private set; }
		public bool AnyHaveDoc { get; private set; }
		public bool AnyHaveTra { get; private set; }
		public bool AllThatHaveAsmHaveDoc { get; private set; }
		public bool AnyThatHaveAsmHaveDoc { get; private set; }
		public bool AllThatHaveDocHaveAsm { get; private set; }
		public bool AnyThatHaveDocHaveAsm { get; private set; }
		public bool AllThatHaveEngHaveTra { get; private set; }
		public bool AnyThatHaveEngHaveTra { get; private set; }
		public bool AllThatHaveTraHaveEng { get; private set; }
		public bool AnyThatHaveTraHaveEng { get; private set; }
		public bool AllPrivate { get; private set; }
		
		private void UpdateFlags ()
		{
			if (!MultipleSignaturesPossible && Signatures.Count <= 1)
			{
				AnyHaveAsm = ItemType != AssemblyType.Unknown;
				AnyHaveDoc = DocModel != null && !DocModel.IsEmpty ();
				AnyHaveTra = DocModelTranslated != null && !DocModelTranslated.IsEmpty ();
				
				AllThatHaveAsmHaveDoc = !AnyHaveAsm || AnyHaveDoc;
				AnyThatHaveAsmHaveDoc = !AnyHaveAsm || AnyHaveDoc;
				AllThatHaveDocHaveAsm = !AnyHaveDoc || AnyHaveAsm || DocModel.IsCsNone ();
				AnyThatHaveDocHaveAsm = !AnyHaveDoc || AnyHaveAsm || DocModel.IsCsNone ();
				
				AllThatHaveEngHaveTra = !AnyHaveDoc || AnyHaveTra;
				AnyThatHaveEngHaveTra = !AnyHaveDoc || AnyHaveTra;
				AllThatHaveTraHaveEng = !AnyHaveTra || AnyHaveDoc;
				AnyThatHaveTraHaveEng = !AnyHaveTra || AnyHaveDoc;
			}
			else
			{
				AnyHaveAsm = Signatures.Any (e => e.InAsm);
				AnyHaveDoc = Signatures.Any (e => e.InDoc) && DocModel != null && !DocModel.IsEmpty ();
				AnyHaveTra = Signatures.Any (e => e.InDocTranslated) && DocModelTranslated != null && !DocModelTranslated.IsEmpty ();
				
				IEnumerable<SignatureEntry> asmOrDoc = Signatures.Where (s => (s.InAsm || s.InDoc));
				
				AllThatHaveAsmHaveDoc = !AnyHaveAsm || asmOrDoc.All (e => e.InDoc);
				AnyThatHaveAsmHaveDoc = !AnyHaveAsm || asmOrDoc.Any (e => e.InDoc);
				AllThatHaveDocHaveAsm = !AnyHaveDoc || asmOrDoc.All (e => e.InAsm) || DocModel.IsCsNone ();;
				AnyThatHaveDocHaveAsm = !AnyHaveDoc || asmOrDoc.Any (e => e.InAsm) || DocModel.IsCsNone ();;
				
				IEnumerable<SignatureEntry> engOrTra = Signatures.Where (s => (s.InDoc || s.InDocTranslated));
				
				AllThatHaveEngHaveTra = !AnyHaveDoc || engOrTra.All (e => e.InDocTranslated);
				AnyThatHaveEngHaveTra = !AnyHaveDoc || engOrTra.Any (e => e.InDocTranslated);
				AllThatHaveTraHaveEng = !AnyHaveTra || engOrTra.All (e => e.InDoc);
				AnyThatHaveTraHaveEng = !AnyHaveTra || engOrTra.Any (e => e.InDoc);
			}
			
			AllPrivate = m_SignatureList.Any(s => s.InAsm && s.Asm.Private) && !Signatures.Any(s => (s.InAsm || s.InDoc));
		}

		public bool ContainsSignature (string sig, bool includePrivate = false)
		{
			if (includePrivate)
				return m_SignatureList.Any (e => e.Name == sig);
			return Signatures.Any (e => e.Name == sig);
		}

		public SignatureEntry GetSignature (string sig, bool includePrivate = false)
		{
			if (includePrivate)
				return m_SignatureList.FirstOrDefault (e => e.Name == sig);
			return Signatures.FirstOrDefault (e => e.Name == sig);
		}
		
		internal void AddSignature (SignatureEntry signature)
		{
			m_SignatureList.Add (signature);
		}
	}
}
