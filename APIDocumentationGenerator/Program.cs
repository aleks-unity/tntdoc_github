using NUnit.Framework;

namespace APIDocumentationGenerator
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var processor = new DirectoryProcessorHg();
			processor.Process();
		}
	}
}
