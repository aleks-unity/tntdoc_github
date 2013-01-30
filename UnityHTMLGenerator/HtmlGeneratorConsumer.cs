using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnderlyingModel;
using UnityDocAnatomizer;
using UnityTxtParser;
using System.Collections.Generic;

namespace UnityHtmlGenerator
{
	public class HtmlGeneratorConsumer : AnatomizerTxtConsumer
	{
		private Action<string> _noneFilenameHandler;

        public string GenerateHTML(AssemblyType id, string txtContent)
        {
            string htmlString = "";

            switch (id)
            {
                case AssemblyType.Class:
                    htmlString = GenerateHTMLClass(txtContent);
                    break;
                case AssemblyType.Property:
                    htmlString = GenerateHTMLProperty(txtContent);
                    break;
                case AssemblyType.Method:
                    htmlString = GenerateHTMLFunction(txtContent);
                    break;
                default:
                    break;
            }

            return htmlString;
        }

        public string GenerateHTMLClass(string txtContent)
        {
            string htmlString = "";


            return htmlString;
        }

        /// <summary>
        /// Quick hack Template gen function. need fix later...
        /// </summary>
        /// <param name="txtContent"></param>
        /// <param name="className"></param>
        /// <param name="memberName"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public string GenerateHTMLProperty2(string txtContent, string className, string memberName, string typeName)
        {
            string desc = GetDescriptionFromText(txtContent);

        	var myData = new TemplateData_Base
        	             	{
								m_class = className,
								m_member = memberName, 
								m_type = typeName,
								m_description = desc
							};

        	var examples = CreateExamplesFromText(txtContent);

            myData.m_examples = examples;
            //if (examples.Count > 0)
            //{
            //    //myData.m_examples.AddRange(examples);
            //}

            var page = new PropertyTemplate(myData);
            return page.TransformText();
        }

        private string GetDescriptionFromText(string memberDocText)
        {
            int convertExampleFirstIndex = memberDocText.IndexOf("CONVERTEXAMPLE");
            int beginExFirstIndex = memberDocText.IndexOf("BEGIN EX");

            String descString = null;

			//no convertexample or begin ex
			if (convertExampleFirstIndex < 0 && beginExFirstIndex < 0)
				descString = memberDocText;
			//found both
			else if (convertExampleFirstIndex >= 0 && beginExFirstIndex >= 0)
				descString = memberDocText.Substring(0, convertExampleFirstIndex);
			//only example, no conversion
			else if (convertExampleFirstIndex < 0)
				descString = memberDocText.Substring(0, beginExFirstIndex);
			//conversion, but no example?
			else if (beginExFirstIndex < 0)
				Debug.Assert(false);
			else
				descString = memberDocText;

        	descString = descString.Replace("///", "");
            descString = descString.Replace("CONVERTEXAMPLE", "");
            descString = descString.Replace("BEGIN EX", "");

            return descString;
        }

        private static List<Example> CreateExamplesFromText(string memberDocText)
        {
            // TODO: language of example must be taken from text...

            // TODO: parse multiple Examples

            int exampleBeginIndex = memberDocText.IndexOf("BEGIN EX");
            int exampleEndIndex = memberDocText.IndexOf("END EX");

            var examples = new List<Example>();

            while (exampleBeginIndex > 0)
            {
                if (exampleBeginIndex >= 0 && exampleEndIndex >= 0)
                {
                    String exampleText = memberDocText.Substring(exampleBeginIndex, exampleEndIndex - exampleBeginIndex);

                    exampleText = exampleText.Replace("BEGIN EX", "");
                    exampleText = exampleText.Replace("END EX", "");

                	var ex = new Example
                	         	{
									m_languageID = ScriptLanguageID.JavaScript,
									m_expression = exampleText
								};
                	// TODO
                	examples.Add(ex);
                }
                else
                {
                    break;
                }

                exampleBeginIndex = memberDocText.IndexOf("BEGIN EX", exampleEndIndex);
                exampleEndIndex = memberDocText.IndexOf("END EX", exampleEndIndex+1);
            }

                /*
            else
            {
                //
                // Currently the template just read example [0] so
                //
                Example ex = new Example();
                ex.m_languageID = ScriptLanguageID.SCRIPT_LANGUAGE_JAVASCRIPT; // TODO
                ex.m_expression = "";
                examples.Add(ex);
            }
                 */

            //myData.m_examples = new System.Collections.Generic.List<Example>();
            //Example item1 = new Example();
            //item1.m_languageID = ScriptLanguageID.SCRIPT_LANGUAGE_JAVASCRIPT;
            //item1.m_expression = "c=a+b";
            //myData.m_examples.Add(item1);

            //Example item2 = new Example();
            //item2.m_languageID = ScriptLanguageID.SCRIPT_LANGUAGE_JAVASCRIPT;
            //item2.m_expression = "d=a*b";
            //myData.m_examples.Add(item2);
            
            // TODO:
            return examples;
        }


        public string GenerateHTMLProperty(string txtContent)
        {
            string htmlString = txtContent;

        	var myData = new TemplateData_Base
        	             	{
        	             		m_class = "Color",
        	             		m_member = "r",
        	             		m_type = "float",
        	             		m_description = "Red component of the color.",
        	             		m_examples = new List<Example>()
        	             	};

        	var item1 = new Example {m_languageID = ScriptLanguageID.JavaScript, m_expression = "c=a+b"};
        	myData.m_examples.Add(item1);

        	var item2 = new Example {m_languageID = ScriptLanguageID.JavaScript, m_expression = "d=a*b"};
        	myData.m_examples.Add(item2);

            var page = new PropertyTemplate(myData);
            htmlString = page.TransformText();
            //System.IO.File.WriteAllText("outputPage.html", htmlString);

            return htmlString;
        }

        public string GenerateHTMLFunction(string txtContent)
        {
            string htmlString = "";


            return htmlString;
        }

		//implementation of ITxtConsumer
		public HtmlGeneratorConsumer()
		{
			//_duplicateHandler = s => Console.WriteLine(string.Format("Duplicate filename detected {0}", s));
			_noneFilenameHandler = s => Console.WriteLine(string.Format("Filename starts with 'none': {0}", s));
		}

		public void SetDuplicateHandler(Action<string> handler)
		{
			//_duplicateHandler = handler;
		}

		public void SetUnmatchedRegexHandler(Action<string> handler)
		{
			MNG.SetUnmatchedRegexHandler(handler);
		}

		public void SetNoneHandler(Action<string> handler)
		{
			_noneFilenameHandler = handler;
		}

		public override void OnCppRaw(string content)
		{
			_accumulatedContent.Clear();
		}
		
		
		public override void OnCsRaw(string content)
		{
			if (_accumulatedContent.Length == 0)
				return;

			bool bExportDocs = DoImportOrExport(content);
			
			if (bExportDocs)
				ExportDocs(content);                           
			
		}


		public override void OnConditional(string content)
		{
		}

		public override void StartEnum(string content)
		{
			var accumulatedContent = _accumulatedContent.Consume().Trim();
			var filename = GetMemberFileName(content, TypeKind.Class);

			WriteContentToFile(accumulatedContent, filename);

			StartType(content);
		}

		public override void OnEnumMember(string codeContent)
		{
			var filename = GetMemberFileName(codeContent, TypeKind.Enum);
			var accumulatedContent = _accumulatedContent.Consume().Trim();

			WriteContentToFile(accumulatedContent, filename);
		}

	   
		public override void StartStruct(string codeContent)
		{
			var accumulatedContent = _accumulatedContent.Consume().Trim();

			var filename = GetMemberFileName(codeContent, TypeKind.Struct);

			WriteContentToFile(accumulatedContent, filename);

			StartType(codeContent);
		}

        public override void StartStructNoLayout(string codeContent)
        {
            StartStruct(codeContent);
        }

		public override void EndClass()
		{
			PopTypeName();
		}

		public override void OnCustom(string content)
		{
			if (!content.Contains("private"))
			{
				ExportDocs(content);
			}
		}

		public override void OnAuto(string content)
		{
			ExportDocs(content);
		}

		public override void OnCustomProp(string content)
		{
			ExportDocs(content);
		}

		public override void OnAutoProp(string codeContent)
		{
			var accumulatedContent = _accumulatedContent.Consume().Trim();

			var filename = GetMemberFileName(codeContent, TypeKind.AutoProp);

			WriteContentToFile(accumulatedContent, filename);               
		}

        public override void OnAutoPtrProp(string codeContent)
        {
            throw new NotImplementedException();
        }


		public override void OnCsNone(string content)
		{
			ExportDocs(content);
		}


		private void ExportDocs(string codeContent)
		{
			var accumulatedContent = _accumulatedContent.Consume().Trim();

			if (accumulatedContent.Length > 0 && !accumulatedContent.Contains("*undoc"))
			{
				var filename = GetMemberFileName(codeContent, TypeKind.PureSignature);
				
				WriteContentToFile(accumulatedContent, filename);               
			}
		}

		private void WriteContentToFile(string accumulatedContent, string filename)
		{
			if (String.IsNullOrEmpty(accumulatedContent))
				return;
			
			try
			{
				if (Path.GetFileName(filename).StartsWith("none"))
				{
					_noneFilenameHandler(filename);
				}
				
				var writer = new StreamWriter(filename);
				writer.Write(accumulatedContent);
				writer.Close();
			}
			catch (DirectoryNotFoundException)
			{
				Console.Error.WriteLine("directory not found for file {0}", Path.GetFullPath(filename));
			}
		}


		protected override void StartClass(string codeContent, bool isSealed)
		{
			var accumulatedContent = _accumulatedContent.Consume().Trim();

			var filename = GetMemberFileName(codeContent, TypeKind.Class);
			WriteContentToFile(accumulatedContent, filename);
			StartType(codeContent);
		}

	}
}
