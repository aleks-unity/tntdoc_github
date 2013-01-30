using System;
using System.IO;
using UnderlyingModel;

//this project is obsolete, look at APIDocumentationGenerator instead
namespace UnityHtmlGenerator
{
	class MainClass
	{
		public static void Main (string[] args)
		{
            //testing code
			

            var htmlGenerator = new HtmlGeneratorConsumer(); ;
            string contents = "Hello World";

			//var txtReader = new StringReader(contents);
			//var txtWriter = new StringWriter();
			//Parser.Parse(txtReader, new HtmlGeneratorConsumer());

			string htmlString = htmlGenerator.GenerateHTML(AssemblyType.Property, contents);
            File.WriteAllText("outputPage.html", htmlString);

            //Console.WriteLine( htmlString );
			Console.WriteLine("Done");

			return;
		}
	}
}
