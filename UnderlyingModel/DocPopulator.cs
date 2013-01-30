using System;
using System.IO;
using MemDoc;
using NUnit.Framework;

namespace UnderlyingModel
{
	public partial class NewDataItemProject
	{
		private void PopulateFromMem ()
		{
			string[] memFiles;

			if (!DirectoryUtil.TryGetAllMemFiles (out memFiles))
			{
				Console.WriteLine ("error getting source files");
				return;
			}

			foreach (var memFile in memFiles)
			{
				// Get member name
				var memberName = Path.GetFileNameWithoutExtension (memFile);
				if (memberName == null)
				{
					Console.WriteLine ("error: could not get file name from {0} ", memFile);
					continue;
				}

				//HACK SPECIAL CASE
				if (SkipSpecialCaseMember(memberName))
					continue;

				// Create or find MemberItem
				if (!m_MapNameToItem.ContainsKey (memberName))
					m_MapNameToItem[memberName] = new MemberItem (memberName, AssemblyType.Unknown);
			}
		}
	}
}
