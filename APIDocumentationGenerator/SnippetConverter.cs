using System;
using System.IO;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Ast.Visitors;
using Boo.Lang.Compiler.IO;
using UnityExampleConverter;

namespace APIDocumentationGenerator
{
	public class SnippetConverter
	{
		[ThreadStatic] static UnityScriptConverter s_PerThreadConverter;
		public static UnityScriptConverter PerThreadConverter
		{
			get { return s_PerThreadConverter ?? (s_PerThreadConverter = CreateUnityScriptConverter()); }
		}

		private static UnityScriptConverter CreateUnityScriptConverter()
		{
			var converter = new UnityScriptConverter();
			converter.References.Add(typeof(UnityEngine.MonoBehaviour).Assembly);
			converter.References.Add(typeof(UnityEditor.MenuItem).Assembly);
			return converter;
		}

		public ConversionResult Convert(string javaScriptText)
		{
			PerThreadConverter.Input.Add(new StringInput("Example", javaScriptText));
			try
			{
				var convertedCode = PerThreadConverter.Run();
				return new ConversionResult(CSharpCodeFor(convertedCode), BooCodeFor(convertedCode));
			}
			finally
			{
				PerThreadConverter.Input.Clear();
			}
		}

		public struct ConversionResult
		{
			public readonly string CSharpCode;
			public readonly string BooCode;

			public ConversionResult(string cSharpCode, string booCode)
			{
				CSharpCode = cSharpCode;
				BooCode = booCode;
			}
		}

		private static string CSharpCodeFor(CompileUnit[] convertedCode)
		{
			var csharp = new StringWriter();
			convertedCode[0].Accept(new CSharpPrinter(csharp));
			return csharp.ToString();
		}

		private static string BooCodeFor(CompileUnit[] convertedCode)
		{
			var boo = new StringWriter();
			convertedCode[0].Accept(new BooPrinterVisitor(boo));
			return boo.ToString();
		}
	}
}
