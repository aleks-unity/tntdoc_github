using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityDocAnatomizer;
using UnityTxtParser;

namespace HTMLGenerator
{
 
	public class HTMLTxtConsumer : AnatomizerTxtConsumer
	{
	    private readonly StringBuilder _accumulatedContent = new StringBuilder();
        private bool bConvertExample = false;

        //implementation of ITxtConsumer
        public HTMLTxtConsumer()
        {
        }


	    public override void OnDocumentation(string content)
        {
            var result = Regex.Split(content, "\r\n|\r|\n");
            foreach (var s in result)
            {
                _accumulatedContent.AppendLine(string.Format("///{0}", s));
            }
        }

		public override void OnCppRaw(string content)
		{
		    _accumulatedContent.Clear();
		}
		
		
		public override void OnCsRaw(string content)
		{
		    if (_accumulatedContent.Length == 0)
		    {
		        return;
		    }			
		}

        public override void OnConvertExample()
        {
            _accumulatedContent.AppendLine("CONVERTEXAMPLE");
            bConvertExample = true;
        }

        public override void OnExample(string content)
        {
            _accumulatedContent.AppendLine("<div>");
            if (bConvertExample)
            {
                CreateDropDown();
                HideShowEx("JavaScript");
				string csExample = printCSharpCodeFor();
            }
            _accumulatedContent.AppendLine(content);
            _accumulatedContent.AppendLine("</div>");
            bConvertExample = false;
        }

        private void HideShowEx(string compLang)
        {
            _accumulatedContent.AppendFormat("<div class='code' code_lang_name='{0}'>\n", compLang);
        }

        private void CreateDropDown()
        {
            _accumulatedContent.Append("<div class='example'>");
            _accumulatedContent.Append("  <div style='clear:both;'>");
            _accumulatedContent.Append("    <div class='cSelect cSelectWidth roundBottom'>");
            _accumulatedContent.Append("      <div class='cSelect-wrapper'>");
            _accumulatedContent.Append("        <div class='cSelect-ArrowDown'></div>");
            _accumulatedContent.Append("	 <span class='cSelect-Selected'>JavaScript</span>");
            _accumulatedContent.Append("	 <ul class='cSelectWidth' style='display:none;'>");
            _accumulatedContent.Append("		<li>JavaScript</li>");
            _accumulatedContent.Append("		<li>C#</li>");
            _accumulatedContent.Append("		<li>Boo</li>");
            _accumulatedContent.Append("	 </ul>");
            _accumulatedContent.Append("      </div>");
            _accumulatedContent.Append("    </div>");
            _accumulatedContent.Append("    <div style='clear:both;'></div>"); // Used to keep the selection dropdown above the *pre*formated box
            _accumulatedContent.Append("  </div>"); // closes <div style="clear:both;">
        }


        public override void OnConditional(string content)
        {
        }

        public override void StartEnum(string content)
        {
            var accumulatedContent = _accumulatedContent.Consume().Trim();
            var filename = GetProperClassName(content);

            //WriteContentToFile(accumulatedContent, filename);

            StartType(content);
        }

        public override void OnEnumMember(string codeContent)
        {
            var filename = GetProperEnumName(codeContent);
            var accumulatedContent = _accumulatedContent.Consume().Trim();

            //WriteContentToFile(accumulatedContent, filename);
        }

       
        public override void StartStruct(string content)
        {
            var accumulatedContent = _accumulatedContent.Consume().Trim();

            var filename = GetProperStructName(content);

            //WriteContentToFile(accumulatedContent, filename);

            StartType(content);
        }

        public override void EndClass()
        {
            PopTypeName();
        }

        public override void OnCustom(string content)
        {
            if (!content.Contains("private"))
            {
                //ExportDocs(content);
            }
        }

        public override void OnAuto(string content)
        {
            //ExportDocs(content);
        }

        public override void OnCustomProp(string content)
        {
            //ExportDocs(content);

        }

        public override void OnAutoProp(string codeContent)
        {
            var accumulatedContent = _accumulatedContent.Consume().Trim();

            var filename = GetProperAutoPropName(codeContent);

            //WriteContentToFile(accumulatedContent, filename);               
        }


        public override void OnCsNone(string content)
        {
            //ExportDocs(content);
        }

        
	    protected override void StartClass(string codeContent, bool isSealed)
		{
			var accumulatedContent = _accumulatedContent.Consume().Trim();

	        var filename = GetProperClassName(codeContent);
            //WriteContentToFile(accumulatedContent, filename);
			StartType(codeContent);
		}

	}
}