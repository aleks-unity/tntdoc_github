using System;	
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityTxtParser;

namespace UnityDocDissector.Tests
{
	[TestFixture]
	public class FileNameCollisions
	{
		[Test]
		public void NameCollisionsTest()
		{
		    string[] txtFiles;
            if (!DirectoryUtil.TryGetRuntimeExportTxtFiles(out txtFiles))
            {
                if (txtFiles==null)
                    Assert.Fail("failure getting txt files (incorrect directory)");
                else if (txtFiles.Length == 0)
                    Assert.Fail("no txt files were found in directory");
                return;
            }
            try
            {
                string memberDocTestDir = DirectoryUtil.MemberDocsDir;
                Directory.CreateDirectory(memberDocTestDir);
                foreach (var f in Directory.EnumerateFiles(memberDocTestDir))
                {
                    File.Delete(f);
                }
            }
            catch (Exception)
            {
                Assert.Fail("could not create directory for testing member docs");
                return;
            }
			foreach (var txt in txtFiles)
			{
                using (var txtReader = File.OpenText(txt))
                {
                    try
                    {
                        Parser.Parse(txtReader, new DissectorTxtConsumer(throwOnDuplicateFiles: true, throwOnUnmatchedRegex: false));
                    }
                    catch (DuplicateFileNameException e)
                    {
                        Assert.Fail("duplicate file detected {0}", e.FaultyFileName);
                    }
                }
			}
		}

	}
}
