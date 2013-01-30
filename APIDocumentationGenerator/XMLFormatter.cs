using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using APIDocumentationGenerator;
using UnderlyingModel;
using MemDoc;

class XMLFormatter
{
	private readonly SnippetConverter _snippetConverter;

	public static XElement FormattedXML(MemberItem member)
	{
		return new XMLFormatter(new SnippetConverter()).FormattedXmlFor(member);
	}

	public XMLFormatter(SnippetConverter snippetConverter)
	{
		_snippetConverter = snippetConverter;
	}

	public XElement FormattedXmlFor(MemberItem member)
	{
		var sectionElements = member.DocModel.SubSections.Select(FormattedXML).ToList();
		if (member.ItemType == AssemblyType.Class || member.ItemType == AssemblyType.Struct)
		{
			var childrenList = member.ChildMembers.Select(child => new XElement("member_id", child.ItemName)).ToList();
			return new XElement("Model", sectionElements, new XElement("Children", childrenList));
		}
		return new XElement("Model", sectionElements);
	}

	private XElement FormattedXML(MemberSubSection section)
	{
		var finalList = new List<XElement>();
		var sigList = section.SignatureEntryList.Where(sig => sig.Asm!=null).Select(sig => FormattedXML(sig.Asm));
		finalList.AddRange(sigList);
		finalList.Add(new XElement("Summary", section.Summary));
		if (section.Parameters != null && section.Parameters.Count > 0)
		{
			var paramsList = section.Parameters.Select(paramWithDoc => FormattedXML(paramWithDoc));
			finalList.AddRange(paramsList);
		}

		if (section.ReturnDoc != null)
			finalList.Add(FormattedXML(section.ReturnDoc));

		var blocks = new List<XElement>();
		foreach (var block in section.TextBlocks)
		{
			XElement blockXML = new XElement("dummy");
			DescriptionBlock descriptionBlock = block as DescriptionBlock;
			if (descriptionBlock != null)
				blockXML = FormattedXML(descriptionBlock);

			ExampleBlock exampleBlock = block as ExampleBlock;
			if (exampleBlock != null)
				blockXML = FormattedXML(exampleBlock);
			blocks.Add(blockXML);
		}
		finalList.AddRange(blocks);
		return new XElement("Section", finalList);
	}

	private static XElement FormattedXML(AsmEntry asmEntry)
	{
		string typeName = StringConvertUtils.AssemblyTypeNameForSignature(asmEntry.EntryType);
		var attList = new List<XAttribute> {new XAttribute("name", asmEntry.Name), new XAttribute("type", typeName)};
		if (!asmEntry.Modifiers.IsEmpty())
			attList.Add(new XAttribute("modifiers", asmEntry.Modifiers));
		var declElement = new XElement("Declaration", attList);
		
		var elementList = new List<XElement> {declElement};
		if (asmEntry.EntryType == AssemblyType.Method || asmEntry.EntryType == AssemblyType.Constructor)
		{
			elementList.AddRange(asmEntry.ParamList.Select(param => FormattedXML(param)));
		}
		var returnType = new XElement("ReturnType", asmEntry.ReturnType);
		elementList.Add(returnType);
		return new XElement("Signature", elementList);
	}

	private static XElement FormattedXML(ParameterWithDoc paramWithDoc)
	{
		return new XElement("ParamWithDoc", new XElement("name", paramWithDoc.Name), new XElement("doc", new XCData(paramWithDoc.Doc)));
	}

	private static XElement FormattedXML(ReturnWithDoc returnWithDoc)
	{
		return new XElement("ReturnWithDoc", new XAttribute("type", returnWithDoc.ReturnType), new XElement("doc", new XCData(returnWithDoc.Doc)));
	}

	private static XElement FormattedXML(ParamElement paramElement)
	{
		var attList = new List<XAttribute> {new XAttribute("name", paramElement.Name), new XAttribute("type", paramElement.Type)};
		if (!paramElement.Modifiers.IsEmpty())
			attList.Add(new XAttribute("modifier", paramElement.Modifiers));

		return new XElement("ParamElement",  attList);
	}

	private static XElement FormattedXML(DescriptionBlock block)
	{
		return new XElement("Description", new XCData(block.Text));
	}

	private XElement FormattedXML(ExampleBlock block)
	{
		if (block.IsConvertExample)
		{
			var exampleList = new List<XElement>();
			string javaScriptText = block.Text;
			string cSharpScriptText;
			string booScriptText;
			DoConversion(javaScriptText, out cSharpScriptText, out booScriptText);
			exampleList.Add(new XElement("JavaScript", new XCData(javaScriptText)));
			exampleList.Add(new XElement("CSharp", new XCData(cSharpScriptText)));
			exampleList.Add(new XElement("Boo", new XCData(booScriptText)));

			return new XElement("Example",
				new XAttribute("nocheck", block.IsNoCheck),
				new XAttribute("convertexample", block.IsConvertExample),
				exampleList);
		}
		
		return new XElement("Example", 
			new XAttribute("nocheck", block.IsNoCheck), 
			new XAttribute("convertexample", block.IsConvertExample),
			new XCData(block.Text));
	}

	private void DoConversion(string javaScriptText, out string cSharpScriptText, out string booScriptText)
	{
		try
		{
			Console.WriteLine("Converting...");
			var result = _snippetConverter.Convert(javaScriptText);
			cSharpScriptText = result.CSharpCode;
			booScriptText = result.BooCode;
		}
		catch (Exception)
		{
			cSharpScriptText = "stub for C# example";
			booScriptText = "stub for Boo example";
		}
	}
}
