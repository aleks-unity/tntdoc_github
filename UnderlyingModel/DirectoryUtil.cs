using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace UnderlyingModel
{
    static public class DirectoryUtil
    {
        public const string TxtExtensionMask = "*.txt";
    	public const string MemExtensionMask = "*.mem";
    	public const string MemExtension = "mem";

    	public static string RootDirName
    	{
    		get
    		{
    			var curDir = Path.GetDirectoryName(Directory.GetCurrentDirectory());
				if (curDir == null)
				{
					Console.Write("CURDIR = {0}", Directory.GetCurrentDirectory());
					return "CRAP";
				}

    			var toolsIndex = curDir.IndexOf("Tools", StringComparison.CurrentCultureIgnoreCase);

				if (toolsIndex < 0)
				{
					Console.WriteLine("could not find 'tools' inside directory name (curdir = {0})", curDir);
					return "crap";
				}
    			var rootDir = curDir.Substring(0, toolsIndex);
    			return rootDir;
    		}
    	}

    	public static string RuntimeExportDir  = Path.Combine(RootDirName, "Runtime/Export");
        public static string EditorMonoDir = Path.Combine(RootDirName, "Editor/Mono");
        public static string RuntimeExportGeneratedDir = Path.Combine(RootDirName, "Runtime/ExportGenerated/Editor");
        public static string MemberDocsDir = Path.Combine(RootDirName, "Documentation/MemberDocs");
        public static string MemberDocsDirFullPath = Path.GetFullPath(MemberDocsDir);
        public static string ReassembledDir = Path.Combine(RootDirName, "Documentation/Reassembled");
		public static string StrippedDir = Path.Combine(RootDirName, "Documentation/Stripped");

	    public static string ScriptRefOutput = Path.Combine(RootDirName,
	                                                        "build/UserDocumentation/ActualDocumentation/Documentation/ScriptReference");
		public static string CombinedAssembliesDir = Path.Combine(RootDirName, "build/CombinedAssemblies");
		public static string EngineDllLocation = Path.Combine(CombinedAssembliesDir, "UnityEngine.dll");
		public static string EditorDllLocation = Path.Combine(CombinedAssembliesDir, "UnityEditor.dll");
		
		public static bool IsEditorMono(string fname)
		{
			fname = fname.Replace("\\", "/");
			return fname.Contains("Editor/Mono");
		}

        public static bool TryGetRuntimeExportTxtFiles(out string[] txtFiles)
        {
            txtFiles=null;
            try
            {
	            txtFiles = GetTxtFilesRecursive(RuntimeExportFullPath()).ToArray();
            }
            catch (DirectoryNotFoundException d)
            {
				Console.WriteLine(d);
                return false;
            }
            return txtFiles.Length > 0;
        }

        public static bool TryGetAllTxtFiles(out string[] txtFiles)
        {
            txtFiles = null;
            string[] engineTxtFiles;
            string[] editorTxtFiles;
            try
            {
	            engineTxtFiles = GetTxtFilesRecursive(RuntimeExportFullPath()).ToArray();
	            editorTxtFiles = GetTxtFilesRecursive(EditorMonoFullPath()).ToArray();
            }
            catch (DirectoryNotFoundException d)
            {
                Console.WriteLine(d);
                return false;
            }

            var lst = engineTxtFiles.ToList();
            lst.AddRange(editorTxtFiles.ToList());
            txtFiles = lst.ToArray();
            return txtFiles.Length > 0;
        }

		public static bool TryGetAllMemFiles(out string[] memFiles)
		{
			memFiles = null;
	
			try
			{
				memFiles =  Directory.GetFiles(MemberDocsDirFullPath, MemExtensionMask).ToArray();
			}
			catch (DirectoryNotFoundException d)
			{
				Console.WriteLine(d);
				return false;
			}

			return memFiles.Length > 0;
		}

	    private static IEnumerable<string> GetTxtFilesRecursive(string path)
	    {
			var retFiles = new List<string>();
		    var subDirectories = Directory.GetDirectories(path);
		    foreach (var subDir in subDirectories)
		    {
			    if (subDir.Contains("SyntaxDefs"))
				    continue; //exclude UnityAPI.txt
			    var filesInThisDir = Directory.GetFiles(subDir, TxtExtensionMask);
			    retFiles.AddRange(filesInThisDir);
		    }
		    var filesAtTopLevel = Directory.GetFiles(path, TxtExtensionMask);
		    retFiles.AddRange(filesAtTopLevel);
		    return retFiles;
	    }

	    public static string RuntimeExportFullPath()
        {			
			var fullPath = Path.GetFullPath(RuntimeExportDir);
        	return fullPath;
        }

        public static string EditorMonoFullPath()
        {
            return Path.GetFullPath(EditorMonoDir);
        } 

    	public static void CreateDirectoryIfNeeded(string outputFolder)
    	{
			try
			{
				if (!Directory.Exists(outputFolder))
					Directory.CreateDirectory(outputFolder);
			}
			catch (DirectoryNotFoundException)
			{
				Console.Error.WriteLine("could not create directory {0}", outputFolder);
			}
    	}

        public static void DeleteAllFiles(string dir)
        {
            if (!Directory.Exists(dir))
                return;
            foreach (var f in Directory.GetFiles(dir))
                File.Delete(f);
        }

    	public static string MemberNameWithExtension(string memberName, string extMask)
    	{
    		return String.Format("{0}.{1}", memberName, extMask);
    	}

    	public static void CopyDirectory(string sourcePath, string destPath)
    	{
			CreateDirectoryIfNeeded(destPath);

    		foreach (var file in Directory.GetFiles(sourcePath))
    		{
    			var dest = Path.Combine(destPath, Path.GetFileName(file));
    			File.Copy(file, dest);
    		}

    		foreach (var folder in Directory.GetDirectories(sourcePath))
    		{
    			var dest = Path.Combine(destPath, Path.GetFileName(folder));
    			CopyDirectory(folder, dest);
    		}
    	}

		public static void CopyDirectoryFromScratch(string fullPathOrig, string fullPathDestin)
		{
			DeleteAllFiles(fullPathDestin);
			if (Directory.Exists(fullPathDestin))
				Directory.Delete(fullPathDestin, true);
			CreateDirectoryIfNeeded(fullPathDestin);
			CopyDirectory(fullPathOrig, fullPathDestin);
		}
    }
}
