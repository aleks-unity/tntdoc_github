using System.Collections;
using System.Collections.Generic;
using DifferenceEngine;

public class StringDiff
{
	public struct DiffLine
	{
		public string text;
		public int status;
		public DiffLine (int status, string text)
		{
			this.text = text;
			this.status = status;
		}
	}

	DiffEngine engine = new DiffEngine ();
	IDiffList source;
	IDiffList destination;
	public List<DiffLine> diffLines = new List<DiffLine> ();
	public void Compare (string source, string destination)
	{
		this.source = new DiffList_StringData (source + "\n");
		this.destination = new DiffList_StringData (destination + "\n");
		engine.ProcessDiff (this.source, this.destination);

		ArrayList list = engine.DiffReport ();
		diffLines.Clear ();
		foreach (DiffResultSpan span in list)
		{
			switch (span.Status)
			{
				case DiffResultSpanStatus.NoChange:
					diffLines.Add (new DiffLine (0, GetSpanText (span, false)));
					break;
				case DiffResultSpanStatus.AddDestination:
					diffLines.Add (new DiffLine (+1, GetSpanText (span, true)));
					break;
				case DiffResultSpanStatus.DeleteSource:
					diffLines.Add (new DiffLine (-1, GetSpanText (span, false)));
					break;
				case DiffResultSpanStatus.Replace:
					diffLines.Add (new DiffLine (-1, GetSpanText (span, false)));
					diffLines.Add (new DiffLine (+1, GetSpanText (span, true)));
					break;
			}
		}
	}

	private string GetSpanText (DiffResultSpan span, bool fromDest)
	{
		IDiffList list;
		int startIndex;
		if (fromDest)
		{
			list = destination;
			startIndex = span.DestIndex;
		}
		else
		{
			list = source;
			startIndex = span.SourceIndex;
		}

		string str = "";
		for (int i = 0; i < span.Length; i++)
		{
			str += ((TextLine)list.GetByIndex (startIndex + i)).Line + (i < span.Length - 1 ? "\n" : "");
		}
		return str;
	}
}
