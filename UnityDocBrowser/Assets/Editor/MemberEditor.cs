using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnderlyingModel;
using MemDoc;

[System.Serializable]
public class MemberEditor
{
	private DocBrowser m_Browser;
	private MemberItem m_Item;
	private MemberItem m_OrphanDocItem;
	
	private Vector2 m_DocScroll;
	private Vector2 m_TranslateScroll;
	private Vector2 m_TranslateOldScroll;
	private Vector2 m_RawTextScroll;
	
	private MemberSession m_EnglishMember;
	private MemberSession m_TranslatedMember;
	private MemberSession m_TranslatedMemberOld;
	private MemberSession EditMember { get { return m_Browser.translating ? m_TranslatedMember : m_EnglishMember; } }
	private bool m_ShowMerge;
	private int m_SelectedSection = -1;
	private int m_SelectedText = -1;
	private bool EditGrouping { get { return m_Browser.EditGrouping; } set { m_Browser.EditGrouping = value; } }
	private bool ShowDiff { get { return m_Browser.ShowDiff; } set { m_Browser.ShowDiff = value; } }
	
	private static string sTodoText = "TODO.";
	
	public MemberEditor (DocBrowser browser, MemberItem item)
	{
		this.m_Browser = browser;
		Init (item);
	}
	
	public MemberEditor (DocBrowser browser, MemberItem docItem, MemberItem asmItem)
	{
		this.m_Browser = browser;
		Init (docItem, asmItem);
	}
	
	private void Init (MemberItem item)
	{
		m_Item = item;
		m_EnglishMember = new MemberSession (m_Browser.docProject, m_Item, LanguageUtil.ELanguage.English);
		if (m_Browser.translating)
		{
			// First get translated member text
			item.DocModelTranslated.SanitizeForEditing ();
			string translatedModelTextOld = item.DocModelTranslated.ToString ();
			
			// Load translated member and change its doc model to match the English version
			m_TranslatedMember = MergeTranslated (m_Item.DocModel, m_Item.DocModelTranslated);
			
			// Load translated member as dummy using old translated docs
			m_TranslatedMemberOld = new MemberSession (m_Browser.docProject, m_Item, translatedModelTextOld, m_Browser.language);
			
			// Figure out if merged is identical to old translated. If it is, don't show old translated column.
			m_ShowMerge = (m_TranslatedMemberOld.Model.ToString ().Trim () != string.Empty && m_TranslatedMember.Model.ToString () != m_TranslatedMemberOld.Model.ToString ());
			m_TranslatedMember.DirtyAutoChanges = m_ShowMerge;
		}
		else
		{
			m_TranslatedMember = null;
			m_TranslatedMemberOld = null;
		}
	}
	
	private void Init (MemberItem docItem, MemberItem asmItem)
	{
		m_Item = asmItem;
		m_OrphanDocItem = docItem;
		m_EnglishMember = new MemberSession (m_Browser.docProject, m_Item, LanguageUtil.ELanguage.English);
		
		// Find pure member name of asm and doc
		string docMemberName = docItem.Signatures.First (e => e.InDoc).Name;
		int paramsStartIndexChar =  docMemberName.IndexOfAny (new char[] {'(', '<'});
		if (paramsStartIndexChar >= 0)
			docMemberName = docMemberName.Substring (0, paramsStartIndexChar);
		string asmMemberName = asmItem.Signatures.First (e => e.InAsm).Asm.Name;
		
		// Create merged doc text
		string merged = docItem.DocModel.ToString ();
		merged = merged.Replace (docMemberName, asmMemberName);
		
		// Load merged
		m_Item.LoadDoc (merged, LanguageUtil.ELanguage.English);
		m_EnglishMember.DirtyAutoChanges = true;
		
		// Set diff
		m_EnglishMember.TextCurrent = m_Item.DocModel.ToString ();
		m_EnglishMember.Diff.Compare (m_EnglishMember.TextOrig, m_EnglishMember.TextCurrent);
		
		m_TranslatedMember = null;
		m_TranslatedMemberOld = null;
	}
	
	public bool LeaveWithPermission () { return EditMember == null || EditMember.LeaveWithPermission (); }
	
	public void OnEnable (MemberItem selectedMember)
	{		
		// Code below is executed both when the window is opened and at mono reloads.
		if (selectedMember != null)
		{
			m_Item = selectedMember;
			m_EnglishMember.OnEnable (selectedMember);
			if (m_Browser.translating)
			{
				m_TranslatedMember.OnEnable (selectedMember);
				m_TranslatedMemberOld.OnEnable (selectedMember);
			}
		}
		else
		{
			m_EnglishMember = null;
			m_TranslatedMember = null;
			m_TranslatedMemberOld = null;
		}
	}
	
	public void OnDestroy ()
	{
		if (EditMember != null)
			EditMember.LeaveForced ();
	}
	
	public void OnGUI ()
	{
		DocGUI ();
		if (ShowDiff)
		{
			GUILayout.Space (DocBrowser.styles.dividerSpace);
			RawTextDiffGUI ();
		}
	}
	
	private void ToolbarGUI ()
	{
		EditorGUILayout.BeginHorizontal (EditorStyles.toolbar);
		
		DocEditButtons ();
		
		GUILayout.FlexibleSpace ();
		
		if (m_Browser.ShowMatchList)
		{
			CancelAcceptButtons (m_Browser, this);
		}
		else
		{
			RevertSaveButtons ();
		}
		
		EditorGUILayout.EndHorizontal ();
	}
	
	private void DocEditButtons ()
	{
		bool somethingSelected = m_SelectedText >= 0;
		EditorGUI.BeginDisabledGroup (EditMember == null || !EditMember.Loaded || !somethingSelected);
		if (GUILayout.Button ("Insert Example", EditorStyles.toolbarButton))
		{
			MemberSubSection section = EditMember.Model.SubSections[m_SelectedSection];
			section.TextBlocks.Insert (m_SelectedText + 1, new ExampleBlock (""));
			EditMember.Model.SanitizeForEditing ();
			EditMember.OnModelEdited ();
		}
		EditorGUI.EndDisabledGroup ();
		
		if (!m_Browser.translating)
		{
			EditorGUILayout.Space ();
			EditGrouping = GUILayout.Toggle (EditGrouping, "Edit Grouping", EditorStyles.toolbarButton);
		}
	}
	
	private void RevertSaveButtons ()
	{
		EditorGUI.BeginDisabledGroup (EditMember == null || !EditMember.Loaded || !EditMember.Dirty);
		if (GUILayout.Button ("Revert", EditorStyles.toolbarButton))
		{
			EditorGUIUtility.keyboardControl = 0;
			EditMember.Load ();
		}
		EditorGUI.EndDisabledGroup ();
		
		EditorGUI.BeginDisabledGroup (EditMember == null || !EditMember.Loaded || (!EditMember.Dirty && !EditMember.DirtyAutoChanges));
		if (GUILayout.Button ("Save", EditorStyles.toolbarButton))
			Save ();
		EditorGUI.EndDisabledGroup ();
	}
	
	private static void CancelAcceptButtons (DocBrowser browser, MemberEditor editor)
	{
		if (GUILayout.Button ("Cancel", EditorStyles.toolbarButton))
			browser.ShowMatchList = false;
		
		EditorGUI.BeginDisabledGroup (editor == null || editor.m_OrphanDocItem == editor.m_Item || editor.m_OrphanDocItem == null);
		if (GUILayout.Button ("Save Match", EditorStyles.toolbarButton))
			editor.Save ();
		EditorGUI.EndDisabledGroup ();
	}
	
	public void Save ()
	{
		EditorGUIUtility.keyboardControl = 0;
		EditMember.Save ();
		if (m_ShowMerge)
			Init (m_Item);
		if (m_OrphanDocItem != null)
			m_OrphanDocItem.DeleteDoc (false);
	}
	
	public static void NoEditorGUI (DocBrowser browser)
	{
		// Member header
		GUILayout.Label ("No member selected", DocBrowser.styles.docHeader);
		
		EditorGUILayout.BeginVertical (DocBrowser.styles.frameWithMargin, GUILayout.ExpandWidth (true));
		
		EditorGUILayout.BeginHorizontal (EditorStyles.toolbar);
		GUILayout.FlexibleSpace ();
		if (browser.ShowMatchList)
			CancelAcceptButtons (browser, null);
		EditorGUILayout.EndHorizontal ();
		
		EditorGUILayout.EndVertical ();
	}
	
	private void DocGUI ()
	{
		// Member header
		string header = EditMember.Member.ItemName;
		if (m_Browser.ShowMatchList)
			if (m_OrphanDocItem != null)
				header = "Match Docs for "+m_OrphanDocItem.ItemName+" to API "+m_Item.ItemName+" ?";
			else
				header = "Match Docs for "+m_Item.ItemName+" to API [No API Item Selected] ?";
		GUILayout.Label (header, DocBrowser.styles.docHeader);
		
		EditorGUILayout.BeginVertical (DocBrowser.styles.frameWithMargin);
		
		// Doc toolbar
		ToolbarGUI ();
		
		if (EditMember != null && EditMember.Loaded)
		{
			float width = m_Browser.position.width;
			if (m_Browser.m_ShowList)
				width -= DocBrowser.kListWidth + DocBrowser.styles.dividerSpace + 1;
			if (m_Browser.ShowMatchList)
				width -= DocBrowser.kListWidth + DocBrowser.styles.dividerSpace + 1;
			if (m_Browser.translating)
				if (m_ShowMerge)
					width = (width - 2 * (DocBrowser.styles.dividerSpace + 1)) / 3;
				else
					width = (width - (DocBrowser.styles.dividerSpace + 1)) / 2;
			
			EditorGUILayout.BeginHorizontal ();
			
			// English Doc scrollview
			DocColumnGUI (m_EnglishMember, m_Browser.translating, ref m_DocScroll, width, ""+m_EnglishMember.Language);
			
			if (m_Browser.translating)
			{
				GUILayout.Space (DocBrowser.styles.dividerSpace + 1);
				
				// Translated Doc scrollview
				DocColumnGUI (m_TranslatedMember, false, ref m_TranslateScroll, width, m_TranslatedMember.Language + (m_ShowMerge ? " (Merged)" : ""));
				
				if (m_ShowMerge)
				{
					GUILayout.Space (DocBrowser.styles.dividerSpace + 1);
					
					// Translated Doc scrollview
					DocColumnGUI (m_TranslatedMemberOld, true, ref m_TranslateOldScroll, width, ""+m_TranslatedMemberOld.Language+" (Old)");
				}
			}
			
			EditorGUILayout.EndHorizontal ();
		}
		
		EditorGUILayout.EndVertical ();
		
		DocBrowser.DragEnd ();
	}
	
	private void DocColumnGUI (MemberSession member, bool readOnly, ref Vector2 scroll, float width, string title)
	{
		GUILayout.BeginVertical ();
		
		if (m_Browser.translating)
			GUILayout.Label (title, DocBrowser.styles.docHeader);
		
		scroll = GUILayout.BeginScrollView (scroll, DocBrowser.styles.docArea, GUILayout.Width (width));
		DocEditGUI (member, readOnly);
		GUILayout.EndScrollView ();
		
		GUILayout.EndVertical ();
	}
	
	private string TextArea (string text, GUIStyle style, bool readOnly, LanguageUtil.ELanguage language)
	{
		// @TODO: Make SelectableLabel work correctly and then use that,
		/*if (readOnly)
		{
			EditorGUILayout.SelectableLabel (text, style);
			return text;
		}*/
		
		Rect rect = GUILayoutUtility.GetRect (new GUIContent (text), style);
		
		// Hack to prevent select all when clicking in a text field
		Event evt = Event.current;
		Color oldCursorColor = GUI.skin.settings.cursorColor;
		if (evt.type == EventType.MouseDown && rect.Contains (evt.mousePosition))
			GUI.skin.settings.cursorColor = Color.clear;
		
		//GUI.enabled = !readOnly || Event.current.type == EventType.Repaint;
		EditorGUI.BeginChangeCheck ();
		string newText = EditorGUI.TextArea (rect, text, style);
		if (EditorGUI.EndChangeCheck () && !readOnly)
		{
			text = newText;
			if (style != DocBrowser.styles.example && language == LanguageUtil.ELanguage.English)
				text = MemberSubSection.EnforcePunctuation (text);
		}
		
		GUI.skin.settings.cursorColor = oldCursorColor;
		
		return text;
	}
	
	void DocEditGUI (MemberSession member, bool readOnly)
	{
		if (!m_Browser.translating)
		{
			if (EditGrouping)
				GUILayout.Label ("Change order of signatures or whole sections by dragging the handles next to signatures or sections headers.\nThe descriptions and examples are hidden in this mode.", "helpbox");
			
			IEnumerable<SignatureEntry> missingDocSignatures = member.Member.Signatures.Where (s => s.InAsmOrDoc).Where (s => !s.InDoc);
			if (missingDocSignatures.Count () > 0)
			{
				if (!EditGrouping)
				{
					string missingSigsString = string.Join ("\n", missingDocSignatures.Select (e => e.Formatted).ToArray ());
					EditorGUILayout.HelpBox ("Some signatures are missing from the docs (click 'Edit Grouping' to edit):\n"+missingSigsString, MessageType.Warning);
				}
				else
				{
					// Show signatures with missing docs and allow moving down into doc sub sections
					GUILayout.BeginHorizontal ();
					GUILayout.Space (24);
					GUILayout.Label ("Signatures Missing from Docs", DocBrowser.styles.docSectionHeader);
					GUILayout.EndHorizontal ();
					GUI.backgroundColor = GetColor (true, false);
					GUILayout.BeginVertical (DocBrowser.styles.docArea, GUILayout.ExpandHeight (false));
					GUI.backgroundColor = Color.white;
					List<string> missingSigs = missingDocSignatures.Select (e => e.Name).ToList ();
					for (int i=0; i<missingSigs.Count; i++)
						SignatureGUI (member, -1, missingSigs, i, readOnly);
					GUILayout.Space (3);
					GUILayout.EndVertical ();
				}
				
				EditorGUILayout.Space ();
			}
		}
		
		m_SelectedSection = -1;
		m_SelectedText = -1;
		// Display each section
		for (int i=0; i<member.Model.SubSections.Count; i++)
			SubSectionEditGUI (member, i, readOnly);
		
		// Section header for adding new section
		if (EditGrouping && member.Member.MultipleSignaturesPossible)
		{
			SectionHeaderGUI (member, -1, readOnly);
			EditorGUILayout.Space ();
		}
		
		EditorGUILayout.Space ();
		
		GUILayout.FlexibleSpace ();
	}
	
	bool MiniButton (Rect rect, Texture2D icon)
	{
		return GUI.Button (rect, icon, DocBrowser.styles.miniButton);
	}
	
	void SectionHeaderGUI (MemberSession member, int sectionIndex, bool readOnly)
	{
		MemberSubSection section = sectionIndex < 0 ? null : member.Model.SubSections[sectionIndex];
		
		string headerText;
		if (sectionIndex >= 0)
			headerText = "Section "+(sectionIndex+1)+" of "+member.Model.SubSections.Count;
		else
			headerText = "New Section";
		
		EditorGUI.BeginChangeCheck ();
		
		if (EditGrouping && !readOnly)
		{
			// Handle dragging of section
			Rect dragTargetRect = GUILayoutUtility.GetRect (10, 0);
			dragTargetRect.y -= 4;
			dragTargetRect.height = 16;
			DocBrowser.DragTarget (dragTargetRect, typeof (MemberSubSection), member.Model.SubSections, sectionIndex >= 0 ? sectionIndex : member.Model.SubSections.Count);
			
			GUILayout.BeginHorizontal (GUILayout.Height (25));
			
			GUILayout.Space (4);
			Rect rect = GUILayoutUtility.GetRect (14, 14, GUILayout.ExpandWidth (false));
			rect.y += 11;
			
			if (sectionIndex >= 0)
			{
				if (member.Model.SubSections.Count > 1)
				{
					DocBrowser.DragHandle (rect, typeof (MemberSubSection), member.Model.SubSections, sectionIndex, DocBrowser.styles.dragHandle);
					
					if (section.SignatureList.Count == 0)
					{
						GUILayout.Space (4);
						rect = GUILayoutUtility.GetRect (14, 14, GUILayout.ExpandWidth (false));
						rect.y += 11;
						if (MiniButton (rect, DocBrowser.styles.iconRemove))
							member.Model.SubSections.RemoveAt (sectionIndex);
					}
				}
			}
			else
			{
				if (MiniButton (rect, DocBrowser.styles.iconAdd))
					member.Model.SubSections.Add (new MemberSubSection (sTodoText));
			}
			
			GUILayout.Label (headerText, DocBrowser.styles.docSectionHeader);
			GUILayout.EndHorizontal ();
		}
		else
			GUILayout.Label (headerText, DocBrowser.styles.docSectionHeader);
		
		if (sectionIndex >= 0)
		{
			// Handle dragging of signature
			Rect dragTargetRect = GUILayoutUtility.GetRect (10, 0);
			dragTargetRect.y -= 3;
			dragTargetRect.height = 14;
			DocBrowser.DragTarget (dragTargetRect, typeof (SignatureEntry), section.SignatureList, 0);
		}
		
		if (EditorGUI.EndChangeCheck ())
				member.OnModelEdited (true);
		
		if (EditGrouping && section != null && section.SignatureList.Count == 0 && member.Member.MultipleSignaturesPossible && member.Model.SubSections.Count > 1)
		{
			if ((string.IsNullOrEmpty (section.Summary) || section.Summary == sTodoText) && section.Parameters.Count == 0 && section.ReturnDoc == null)
				GUILayout.Label ("Section has no documentation. It can safely be deleted.", DocBrowser.styles.docSectionMessage);
			else
				GUILayout.Label ("Section has documentation. (Exit 'Edit Grouping' mode to see it.) Deleting this section will delete its docs too.", DocBrowser.styles.docSectionMessageWarning);
		}
		if (section != null && section.SignatureList.Count > 0)
		{
			IEnumerable<SignatureEntry> asmEntries = section.SignatureList.Select (e => member.Member.GetSignature (e, true));
			asmEntries = asmEntries.Where (e => e.InAsm);
			IEnumerable<string> returnTypes = asmEntries.Select (e => e.Asm.ReturnType);
			returnTypes = returnTypes.Distinct ();
			if (returnTypes.Count () > 1)
				GUILayout.Label ("Section has signatures with multiple different return types: "+string.Join (", ", returnTypes.ToArray ())+".", DocBrowser.styles.docSectionMessageWarning);
		}
	}
	
	Color GetColor (bool hasSource, bool hasDoc)
	{
		if (!hasSource)
			return new Color (1, 0.7f, 0.7f, 1);
		if (!hasDoc)
			return new Color (1, 1, 0.7f, 1);
		return Color.white;
	}
	
	void SignatureGUI (MemberSession member, int sectionIndex, List<string> signatureList, int signatureIndex, bool readOnly)
	{
		string sig = signatureList[signatureIndex];
		SignatureEntry sigEntry = member.Member.GetSignature (sig, true);
		
		bool edit = !readOnly && !m_Browser.translating && member.Member.MultipleSignaturesPossible;
		
		EditorGUI.BeginChangeCheck ();
		
		GUILayout.BeginHorizontal ();
		
		if (edit)
		{
			GUILayout.Space (4);
			Rect rect = GUILayoutUtility.GetRect (14, 14, GUILayout.ExpandWidth (false));
			rect.y += 4;
			
			DocBrowser.DragHandle (rect, typeof (SignatureEntry), signatureList, signatureIndex, DocBrowser.styles.dragHandle);
			
			if (!sigEntry.InAsm || sigEntry.Asm.Private)
			{
				GUILayout.Space (4);
				rect = GUILayoutUtility.GetRect (14, 14, GUILayout.ExpandWidth (false));
				rect.y += 4;
				if (MiniButton (rect, DocBrowser.styles.iconRemove))
					signatureList.RemoveAt (signatureIndex);
			}
		}
		
		string sigString = m_Browser.ShowRawNames ? sigEntry.Name : sigEntry.FormattedHTML;
		if (sigEntry.InAsm && sigEntry.Asm.Private)
			sigString = "<b><color=red>private</color></b> " + sigString;
		
		GUILayout.Label (sigString, DocBrowser.styles.signature);
		
		GUILayout.EndHorizontal ();
		
		// Handle dragging
		Rect dragTargetRect = GUILayoutUtility.GetRect (10, 0);
		dragTargetRect.y -= 3;
		dragTargetRect.height = 14;
		DocBrowser.DragTarget (dragTargetRect, typeof (SignatureEntry), signatureList, signatureIndex+1);
		
		if (EditorGUI.EndChangeCheck ())
			member.OnModelEdited (true);
	}
	
	void SubSectionEditGUI (MemberSession member, int sectionIndex, bool readOnly)
	{
		MemberSubSection section = member.Model.SubSections[sectionIndex];
		
		if ((member.Member.MultipleSignaturesPossible && member.Model.SubSections.Count > 1) || EditGrouping)
			SectionHeaderGUI (member, sectionIndex, readOnly);
		else
			GUILayout.Space (18);
		
		if (!m_Browser.translating)
		{
			// Toggle for NoDoc
			Rect toggleRect = GUILayoutUtility.GetRect (10, 0);
			toggleRect.height = 16;
			toggleRect.y -= 16;
			toggleRect.xMin = toggleRect.xMax - 175;
			EditorGUI.BeginChangeCheck ();
			section.IsUndoc = GUI.Toggle (toggleRect, section.IsUndoc, "Exclude section from docs");
			if (EditorGUI.EndChangeCheck ())
				member.OnModelEdited ();
		}
		
		// Show signatures
		for (int i=0; i<section.SignatureList.Count; i++)
			SignatureGUI (member, sectionIndex, section.SignatureList, i, readOnly);
		
		if (section.IsUndoc)
		{
			if (!m_Browser.translating)
				EditorGUILayout.HelpBox ("This section will be excluded from the documentation but it will still show up in intelli-sense etc. Many users may not realize it's not officially supported. Consider either making the API internal or include it in the documentation.", MessageType.Warning);
			else
				EditorGUILayout.HelpBox ("This section is not documented. No translation is needed.", MessageType.Info);
		}
		else
		{
			if (!EditGrouping)
			{
				EditorGUI.BeginChangeCheck ();
				ParametersAndReturnGUI (member, sectionIndex, readOnly);
				DescriptionAndExamplesGUI (member, sectionIndex, readOnly);
				if (EditorGUI.EndChangeCheck ())
					member.OnModelEdited ();
			}
		}
		
		EditorGUILayout.Space ();
		
		GUI.enabled = true;
	}
	
	string ParamOrReturnGUI (bool hasAsm, bool hasDoc, string label, string text, LanguageUtil.ELanguage language, bool readOnly, out bool remove, IList list = null, int index = -1)
	{
		remove = false;
		
		EditorGUILayout.BeginHorizontal ();
		
		bool edit = !m_Browser.translating && !readOnly;
		
		bool hasSource, hasDest;
		if (m_Browser.translating)
		{
			hasSource = true;
			hasDest = !text.StartsWith (sTodoText);
		}
		else
		{
			hasSource = hasAsm;
			hasDest = hasDoc;
		}
		
		Rect labelRect = GUILayoutUtility.GetRect (160 + (edit ? 18 : 0), 16, GUILayout.ExpandWidth (false));
		labelRect.xMin += 4;
		labelRect.y += 2;
		
		if (edit)
		{
			// Drag handle
			Rect dragRect = new Rect (labelRect.x, labelRect.y+2, 14, 14);
			if (list != null)
				DocBrowser.DragHandle (dragRect, typeof (ParameterWithDoc), list, index, DocBrowser.styles.dragHandle);
			labelRect.xMin += 14+4;
			
			// remove button
			if (!hasSource)
			{
				Rect buttonRect = new Rect (labelRect.x, labelRect.y+2, 14, 14);
				if (MiniButton (buttonRect, DocBrowser.styles.iconRemove))
					remove = true;
				labelRect.xMin += 14;
			}
		}
		
		// Label
		GUI.Label (labelRect, label, DocBrowser.styles.paramLabel);
		
		// Text field
		if (!readOnly)
			GUI.backgroundColor = GetColor (hasSource, hasDest);
		text = TextArea (text, DocBrowser.styles.param, readOnly, language);
		GUI.backgroundColor = Color.white;
		
		EditorGUILayout.EndHorizontal ();
		
		// Handle dragging
		if (list != null)
		{
			Rect dragTargetRect = GUILayoutUtility.GetRect (10, 0);
			dragTargetRect.y -= 6;
			dragTargetRect.height = 14;
			DocBrowser.DragTarget (dragTargetRect, typeof (ParameterWithDoc), list, index+1);
		}
		
		return text;
	}
	
	void ParametersAndReturnGUI (MemberSession member, int sectionIndex, bool readOnly)
	{
		MemberSubSection section = member.Model.SubSections[sectionIndex];
		
		// Parameters
		if (section.Parameters.Count > 0)
		{
			GUILayout.Label ("Parameters", DocBrowser.styles.docSectionHeader);
			
			// Handle dragging parameter into first slot
			Rect dragTargetRect = GUILayoutUtility.GetRect (10, 0);
			dragTargetRect.y -= 6;
			dragTargetRect.height = 14;
			DocBrowser.DragTarget (dragTargetRect, typeof (ParameterWithDoc), section.Parameters, 0);
			
			ParameterWithDoc paramToRemove = null;
			for (int i=0; i<section.Parameters.Count; i++)
			{
				ParameterWithDoc p = section.Parameters[i];
				string label = "<b>" + p.Name + " : </b>" + (p.HasAsm ? Extensions.GetNiceName (p.TypeString) : "?");
				bool remove;
				p.Doc = ParamOrReturnGUI (p.HasAsm, p.HasDoc, label, p.Doc, member.Language, readOnly, out remove, section.Parameters, i);
				if (remove)
					paramToRemove = p;
			}
			if (paramToRemove != null)
				section.Parameters.Remove (paramToRemove);
		}
		
		// Returns
		if (section.ReturnDoc != null)
		{
			GUILayout.Label ("Returns", DocBrowser.styles.docSectionHeader);
			ReturnWithDoc p = section.ReturnDoc;
			string label = (p.HasAsm ? Extensions.GetNiceName(p.ReturnType) : "?");
			bool remove;
			p.Doc = ParamOrReturnGUI (p.HasAsm, p.HasDoc, label, p.Doc, member.Language, readOnly, out remove);
			if (remove)
				section.ReturnDoc = null;
		}
		
		EditorGUILayout.Space ();
	}
		
	void DescriptionAndExamplesGUI (MemberSession member, int sectionIndex, bool readOnly)
	{
		MemberSubSection section = member.Model.SubSections[sectionIndex];
		
		// Summary
		string controlName = "Section_"+sectionIndex+"_Summary";
		GUI.SetNextControlName (controlName);
		if (GUI.GetNameOfFocusedControl () == controlName)
		{
			m_SelectedSection = sectionIndex;
			m_SelectedText = 0;
		}
		GUILayout.Label ("Description (first line is summary)", DocBrowser.styles.docSectionHeader);
		if (!readOnly)
			GUI.backgroundColor = GetColor (true, m_Browser.translating ? section.Summary != sTodoText : section.Summary != string.Empty);
		section.Summary = TextArea (section.Summary, DocBrowser.styles.description, readOnly, member.Language);
		GUI.backgroundColor = Color.white;
	
		// Description and examples
		int i = 0;
		int removeExample = -1;
		foreach (TextBlock t in section.TextBlocks)
		{
			bool isExample = (t is ExampleBlock);
			
			controlName = "Section_"+sectionIndex+"_Text_"+i;
			GUI.SetNextControlName (controlName);
			if (GUI.GetNameOfFocusedControl () == controlName && !isExample)
			{
				m_SelectedSection = sectionIndex;
				m_SelectedText = i;
			}
			
			if (isExample)
			{
				GUILayout.Space (4);
				
				EditorGUILayout.BeginHorizontal ();
				var example = t as ExampleBlock;
				
				GUILayout.FlexibleSpace ();
				
				EditorGUI.BeginDisabledGroup (readOnly || m_Browser.translating);
				example.IsConvertExample = GUILayout.Toggle (example.IsConvertExample, "Convert");
				example.IsNoCheck = GUILayout.Toggle (example.IsNoCheck, "NoCheck");
				if (GUILayout.Button ("Delete", EditorStyles.miniButton))
					removeExample = i;
				EditorGUI.EndDisabledGroup ();
				
				EditorGUILayout.EndHorizontal ();
				
				// Some hackery to show the label nicely, since gui margins don't work inside a horizontal group.
				Rect r = GUILayoutUtility.GetLastRect ();
				r.x += 4; r.y += 3;
				GUI.Label (r, "Example", DocBrowser.styles.docSectionHeader);
				
				t.Text = TextArea (t.Text, DocBrowser.styles.example, readOnly || m_Browser.translating, member.Language);
			}
			else
			{
				if (i > 0)
					GUILayout.Label ("Description (continued)", DocBrowser.styles.docSectionHeader);
				t.Text = TextArea (t.Text, DocBrowser.styles.description, readOnly, member.Language);
			}
			
			i++;
		}
		
		if (removeExample >= 0)
		{
			section.TextBlocks.RemoveAt (removeExample);
			EditMember.Model.SanitizeForEditing ();
		}
		
	}
	
	void RawTextDiffGUI ()
	{
		EditorGUILayout.BeginVertical (DocBrowser.styles.frameWithMarginWhite, GUILayout.Height (150), GUILayout.ExpandHeight (false));
		
		if (EditMember != null && EditMember.Loaded)
		{
			Color red, green;
			if (EditorGUIUtility.isProSkin)
			{
				green = new Color (0.1f, 0.3f, 0.1f);
				red = new Color (0.35f, 0.1f, 0.1f);
			}
			else
			{
				green = new Color (0.8f, 1.0f, 0.8f);
				red = new Color (1.0f, 0.8f, 0.8f);
			}
			
			m_RawTextScroll = EditorGUILayout.BeginScrollView (m_RawTextScroll);
			
			foreach (StringDiff.DiffLine d in EditMember.Diff.diffLines)
			{
				if (d.status == 0)
					GUI.backgroundColor = Color.clear;
				else if (d.status == 1)
					GUI.backgroundColor = green;
				else if (d.status == -1)
					GUI.backgroundColor = red;
				GUILayout.Label (d.text, DocBrowser.styles.dif);
			}
			GUI.backgroundColor = Color.white;

			EditorGUILayout.EndScrollView ();
		}
		
		EditorGUILayout.EndHorizontal ();
	}
	
	private string GetInitialTranslationString (string englishString)
	{
		return (string.IsNullOrEmpty (englishString) ? string.Empty : sTodoText);
	}
	
	private MemberSession MergeTranslated (MemDocModel english, MemDocModel translatedOld)
	{
		MemberSession translatedNewMember = new MemberSession (m_Browser.docProject, m_Item, translatedOld.Language);
		MemDocModel translatedNew = translatedNewMember.Model;
		
		// If number of sections don't match at all, make new list of sections
		if (english.SubSections.Count != translatedNew.SubSections.Count)
		{
			translatedNew.SubSections.Clear ();
			for (int i=0; i<english.SubSections.Count; i++)
				translatedNew.SubSections.Add (new MemberSubSection (""));
		}
		
		for (int s=0; s<english.SubSections.Count; s++)
		{
			MemberSubSection engSec = english.SubSections[s];
			MemberSubSection newSec = translatedNew.SubSections[s];
			
			// Replace signatures
			newSec.SignatureList = engSec.SignatureList;
			// Replace flags
			newSec.IsCsNone = engSec.IsCsNone;
			newSec.IsUndoc = engSec.IsUndoc;
			
			// Merge parameters
			List<ParameterWithDoc> oldParams = newSec.Parameters;
			newSec.Parameters = new List<ParameterWithDoc> ();
			foreach (ParameterWithDoc engParam in engSec.Parameters)
			{
				ParameterWithDoc newParam = new ParameterWithDoc (engParam.Name);
				newParam.Types = engParam.Types;
				ParameterWithDoc oldParam = oldParams.FirstOrDefault (e => e.Name == engParam.Name);
				if (oldParam != null && !string.IsNullOrEmpty (oldParam.Doc))
					newParam.Doc = oldParam.Doc;
				else
					newParam.Doc = GetInitialTranslationString (engParam.Doc);
				newSec.Parameters.Add (newParam);
			}
			
			// Merge return type
			if (engSec.ReturnDoc == null)
				newSec.ReturnDoc = null;
			else
			{
				string returnDocString = newSec.ReturnDoc != null ? newSec.ReturnDoc.Doc : GetInitialTranslationString (engSec.ReturnDoc.Doc);
				newSec.ReturnDoc = new ReturnWithDoc ();
				newSec.ReturnDoc.ReturnType = engSec.ReturnDoc.ReturnType;
				newSec.ReturnDoc.Doc = returnDocString;
			}
			
			// Merge text blocks
			bool blockTypesMatch = true;
			if (newSec.TextBlocks.Count != engSec.TextBlocks.Count)
				blockTypesMatch = false;
			else
			{
				for (int i=0; i<engSec.TextBlocks.Count; i++)
				{
					if (engSec.TextBlocks[i].GetType () != newSec.TextBlocks[i].GetType ())
						blockTypesMatch = false;
				}
			}
			
			// If block types match, only replace examples
			if (blockTypesMatch)
			{
				for (int i=0; i<engSec.TextBlocks.Count; i++)
				{
					TextBlock engBlock = engSec.TextBlocks[i];
					if (engBlock is ExampleBlock)
						newSec.TextBlocks[i] = engBlock;
				}
			}
			// If block types don't match, replace examples and make all description
			// blocks have TODO in them (except if the English one is empty too).
			else
			{
				newSec.TextBlocks.Clear ();
				for (int i=0; i<engSec.TextBlocks.Count; i++)
				{
					TextBlock engBlock = engSec.TextBlocks[i];
					if (engBlock is ExampleBlock)
						newSec.TextBlocks.Add (engBlock);
					else if (engBlock is DescriptionBlock)
						newSec.TextBlocks.Add (new DescriptionBlock (GetInitialTranslationString (engBlock.Text)));
				}
			}
		}
		
		translatedNewMember.TextOrig = translatedOld.ToString ();
		translatedNewMember.TextCurrent = translatedNew.ToString ();
		translatedNewMember.Diff.Compare (translatedNewMember.TextOrig, translatedNewMember.TextCurrent);
		return translatedNewMember;
	}
}
