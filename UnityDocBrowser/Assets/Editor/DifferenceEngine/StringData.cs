using System;
using System.IO;
using System.Collections;

namespace DifferenceEngine
{
	public class DiffList_StringData : IDiffList
	{
		private ArrayList _lines;

		public DiffList_StringData(string data)
		{
			_lines = new ArrayList();
			string[] lines = data.Split ('\n');
			foreach (string line in lines)
			{
				_lines.Add(new TextLine(line));
			}
		}
		#region IDiffList Members

		public int Count()
		{
			return _lines.Count;
		}

		public IComparable GetByIndex(int index)
		{
			return (TextLine)_lines[index];
		}

		#endregion
	
	}
}