using System;
using System.Collections.Generic;
using System.Linq;


namespace UnderlyingModel
{
	public partial class NewDataItemProject 
	{
		internal readonly Dictionary<string, MemberItem> m_MapNameToItem;
		
		public int ItemCount { get { return m_MapNameToItem.Count; } }

		public static bool SkipSpecialCaseMember(string name)
		{
			return name == "Application._absoluteUrl";
		}

		public NewDataItemProject ()
		{
			m_MapNameToItem = new Dictionary<string, MemberItem> ();
		}

		private void PrintElapsedTime(string message, ref DateTime startTime)
		{
			DateTime endTime = DateTime.Now;
			double duration = (endTime - startTime).TotalSeconds;
			Console.WriteLine("{0} took {1} seconds", message, duration);
			startTime = endTime;
		}

		public void ReloadAllProjectData (LanguageUtil.ELanguage language = LanguageUtil.ELanguage.English)
		{
			m_MapNameToItem.Clear ();
			
			DateTime startTime = DateTime.Now;
			DateTime originalStartTime = startTime;

			// Populate list from assembly
			LoadAsmModules ();
			PrintElapsedTime("Loading ASM modules", ref startTime);
			
			PopulateFromAsm ();
			PrintElapsedTime("Populating from ASM", ref startTime);
			// Populate list from member files - does not actually load the MemDocModels
			PopulateFromMem();
			PrintElapsedTime("PopulateFromMem", ref startTime);
			SearchAsmForPrivateApiMatchingOrphanDocs ();
			PrintElapsedTime("SearchingOrphanDocs", ref startTime);
			// For each member, load MemDocModel from disc.
			// We do this for all members, even the ones we know don't have member files.
			// This way the state and flags for each member are initialized correctly.
			foreach (MemberItem member in m_MapNameToItem.Values)
			{
				try
				{
					member.LoadDoc();
					if (language != LanguageUtil.ELanguage.English)
						member.LoadDoc(language);
				}
				catch (Exception)
				{
                    Console.WriteLine("error loading doc for {0}", member.ItemName);
				}
			}

			PrintElapsedTime("Loading member docs", ref startTime);
			var totalDuration = (startTime - originalStartTime).TotalSeconds;
			Console.WriteLine("Total time for ReloadAllProjectData = {0} sec", totalDuration);
		}

		public bool ContainsMember (string memberName)
		{
			return m_MapNameToItem.ContainsKey (memberName);
		}
		
		public MemberItem GetMember (string memberName)
		{
			MemberItem item;
			if (m_MapNameToItem.TryGetValue (memberName, out item))
				return item;
			return null;
		}

		private int ZeroIsOne (int number)
		{
			return number == 0 ? 1 : number;
		}
		
		// TODO: Fix it! This currently returns nunber of all signatures, not number of signatures that are in assembly!
		public int NumAsmSignaturesForMember (string st)
		{
			var item = m_MapNameToItem[st];
			return item == null ? 0 : ZeroIsOne (item.Signatures.Count);
		}

		public int NumDocSignatures (string st)
		{
			var item = m_MapNameToItem[st];
			return item == null || item.DocModel == null? 0 : ZeroIsOne (item.DocModel.SignatureCount);
		}
		
		public List<MemberItem> GetAllMembers ()
		{
			return m_MapNameToItem.Values.ToList ();
		}

		public List<MemberItem> GetFilteredMembersForProgramming (Presence api, Presence docs)
		{
			IEnumerable<MemberItem> items = new List<MemberItem> (m_MapNameToItem.Values);
			items = items.Where (elem => elem.AllPrivate == false);
			
			if (api == Presence.AllAbsent)
				items = items.Where (elem => elem.AnyThatHaveDocHaveAsm == false);
			else if (api == Presence.SomeOrAllAbsent)
				items = items.Where (elem => elem.AllThatHaveDocHaveAsm == false);
			else if (api == Presence.SomeOrAllPresent)
				items = items.Where (elem => elem.AnyThatHaveDocHaveAsm == true);
			else if (api == Presence.AllPresent)
				items = items.Where (elem => elem.AllThatHaveDocHaveAsm == true);
			
			if (docs == Presence.AllAbsent)
				items = items.Where (elem => elem.AnyThatHaveAsmHaveDoc == false);
			else if (docs == Presence.SomeOrAllAbsent)
				items = items.Where (elem => elem.AllThatHaveAsmHaveDoc == false);
			else if (docs == Presence.SomeOrAllPresent)
				items = items.Where (elem => elem.AnyThatHaveAsmHaveDoc == true);
			else if (docs == Presence.AllPresent)
				items = items.Where (elem => elem.AllThatHaveAsmHaveDoc == true);

			return items.ToList ();
		}
		
		public List<MemberItem> GetFilteredMembersForTranslation (Presence docs, Presence translation)
		{
			IEnumerable<MemberItem> items = new List<MemberItem> (m_MapNameToItem.Values);
			items = items.Where (elem => elem.AllPrivate == false);
			
			if (translation == Presence.AllAbsent)
				items = items.Where (elem => elem.AnyThatHaveEngHaveTra == false);
			else if (translation == Presence.SomeOrAllAbsent)
				items = items.Where (elem => elem.AllThatHaveEngHaveTra == false);
			else if (translation == Presence.SomeOrAllPresent)
				items = items.Where (elem => elem.AnyThatHaveEngHaveTra == true);
			else if (translation == Presence.AllPresent)
				items = items.Where (elem => elem.AllThatHaveEngHaveTra == true);
			
			if (docs == Presence.AllAbsent)
				items = items.Where (elem => elem.AnyThatHaveTraHaveEng == false);
			else if (docs == Presence.SomeOrAllAbsent)
				items = items.Where (elem => elem.AllThatHaveTraHaveEng == false);
			else if (docs == Presence.SomeOrAllPresent)
				items = items.Where (elem => elem.AnyThatHaveTraHaveEng == true);
			else if (docs == Presence.AllPresent)
				items = items.Where (elem => elem.AllThatHaveTraHaveEng == true);

			return items.ToList ();
		}
	}

}
