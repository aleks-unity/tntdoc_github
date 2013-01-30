using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnderlyingModel;
using MemDoc;


public partial class DocBrowser : EditorWindow
{
	public const int kListWidth = 500;
	public bool m_ShowList = true;
	private bool m_ShowMatchList;
	public bool ShowMatchList
	{
		get { return m_ShowMatchList; }
		set
		{
			if (m_ShowMatchList == value)
				return;
			m_MatchList.SelectNone ();
			m_ShowMatchList = value;
			if (m_ShowMatchList)
			{
				m_Translating = false;
				m_ShowList = true;
			}
			OnSelectMember (m_List, m_List.selectedMemberName);
		}
	}
	
	// General
	private NewDataItemProject m_DocProject;
	public NewDataItemProject docProject { get { return m_DocProject; } }
	
	private bool m_Translating = false;
	public bool translating { get { return m_Translating; } }
	
	public bool ShowDiff { get; set; }
	public bool ShowRawNames { get; set; }
	
	private bool m_EditGrouping = false;
	public bool EditGrouping { get { return m_EditGrouping && !m_Translating; } set { m_EditGrouping = value; }  }
	
	private LanguageUtil.ELanguage m_Language = LanguageUtil.ELanguage.Japanese;
	public LanguageUtil.ELanguage language { get { return m_Language; } }
	
	private MemberList m_List;
	private MemberList m_MatchList;
	private MemberEditor m_Editor;
	
	// Other
	static Styles s_Styles;
	public static Styles styles { get { return s_Styles; } }
	private int m_FirstLoad = 3;
	
	// Add menu named "Doc Browser" to the Window menu
	[MenuItem ("Window/Doc Browser")]
	static void Init ()
	{
		// Get existing open window or if none, make a new one:
		DocBrowser window = (DocBrowser)EditorWindow.GetWindow (typeof (DocBrowser));
		window.minSize = new Vector2 (700, 400);
	}
	
	void OnEnable ()
	{		
		// Code below is executed both when the window is opened and at mono reloads.
		LoadMembers (m_FirstLoad > 0);
		if (string.IsNullOrEmpty (m_List.selectedMemberName))
			m_Editor = null;
		if (m_Editor != null)
			m_Editor.OnEnable (m_List.GetSelectedMemberSlow ());
		if (m_List != null)
			m_List.SetCallbacks (MaySelectNewMember, OnSelectMember);
		if (m_MatchList != null)
			m_MatchList.SetCallbacks (MaySelectNewMatch, OnSelectMatch);
	}

	void OnDestroy ()
	{
		if (m_Editor != null)
			m_Editor.OnDestroy ();
	}
	
	private bool MaySelectNewMember ()
	{
		return (m_Editor == null || m_Editor.LeaveWithPermission ());
	}
	
	private void OnSelectMember (MemberList list, string memberName)
	{
		if (ShowMatchList)
		{
			UpdateMatch ();
			return;
		}
		
		MemberItem item = list.GetSelectedMemberSlow ();
		if (item == null)
			return;
		m_Editor = new MemberEditor (this, item);
	}
	
	private bool MaySelectNewMatch ()
	{
		return (m_Editor == null || m_Editor.LeaveWithPermission ());
	}
	
	private void OnSelectMatch (MemberList list, string memberName)
	{
		UpdateMatch ();
	}
	
	private void UpdateMatch ()
	{
		MemberItem docItem = m_List.GetSelectedMemberSlow ();
		MemberItem asmItem = m_MatchList.GetSelectedMemberSlow ();
		if (docItem == null)
		{
			m_Editor = null;
			return;
		}
		if (asmItem == null)
		{
			m_Editor = new MemberEditor (this, docItem);
			return;
		}
		m_Editor = new MemberEditor (this, docItem, asmItem);
	}
	
	void OnGUI ()
	{
		if (s_Styles == null)
			s_Styles = new Styles ();
		
		ToolbarGUI ();
		
		EditorGUILayout.BeginHorizontal ();
		{
			if (m_ShowList && m_List != null )
			{
				m_List.OnGUI ();
				GUILayout.Space (styles.dividerSpace);
			}
			
			if (ShowMatchList)
			{
				m_MatchList.OnGUI ();
				GUILayout.Space (styles.dividerSpace);
			}
			
			EditorGUILayout.BeginVertical ();
			{
				if (m_Editor != null)
					m_Editor.OnGUI ();
				else
					MemberEditor.NoEditorGUI (this);
			}
			EditorGUILayout.EndVertical ();
		}
		EditorGUILayout.EndHorizontal ();
		
		if (m_FirstLoad > 0 && Event.current.type == EventType.Repaint)
		{
			m_FirstLoad--;
			if (m_FirstLoad == 0)
				LoadMembers ();
			Repaint ();
		}
	}
	
	void ToolbarGUI ()
	{
		EditorGUILayout.BeginHorizontal (EditorStyles.toolbar);
		GUILayout.Space (1);
		
		if (!m_ShowMatchList)
		{
			// Hide List
			m_ShowList = !GUILayout.Toggle (!m_ShowList, "Hide List", EditorStyles.toolbarButton);
			
			GUILayout.Space (100);
			
			// Translation
			
			EditorGUI.BeginChangeCheck ();
			
			bool translatingNew = GUILayout.Toggle (m_Translating, "Translation Mode", EditorStyles.toolbarButton);
			
			LanguageUtil.ELanguage languageNew = m_Language;
			if (m_Translating)
				languageNew = (LanguageUtil.ELanguage)EditorGUILayout.EnumPopup (m_Language, EditorStyles.toolbarPopup);
			
			if (EditorGUI.EndChangeCheck ())
			{
				EditorGUIUtility.keyboardControl = 0;
				if (m_Editor == null || m_Editor.LeaveWithPermission ())
				{
					bool doLoad = m_Language != languageNew;
					m_Language = languageNew;
					m_Translating = translatingNew;
					if (translatingNew)
						m_EditGrouping = false;
					if (doLoad)
						LoadMembers ();
					OnSelectMember (m_List, m_List.selectedMemberName);
				}
			}
		}
		
		GUILayout.FlexibleSpace ();
		
		// Raw Names button
		ShowRawNames = GUILayout.Toggle (ShowRawNames, "Show Raw Names", EditorStyles.toolbarButton);
		
		// Diff button
		ShowDiff = GUILayout.Toggle (ShowDiff, "Show Diff", EditorStyles.toolbarButton);
		
		GUILayout.Space (100);
		
		// Reload
		if (GUILayout.Button ("Reload", EditorStyles.toolbarButton))
			LoadMembers ();
		
		EditorGUILayout.EndHorizontal ();
	}
	
	void LoadMembers (bool loadNone = false)
	{
		m_DocProject = new NewDataItemProject ();
		
		if (!loadNone)
		{
			m_DocProject.ReloadAllProjectData (language);
		}
		else
		{
			ShowNotification (new GUIContent ("Loading data..."));
		}
		
		if (m_List == null)
			m_List = new MemberList (this);
		m_List.SetCallbacks (MaySelectNewMember, OnSelectMember);
		
		if (m_MatchList == null)
			m_MatchList = new MemberList (this);
		m_MatchList.filter = MemberFilter.AllApi;
		m_MatchList.SetCallbacks (MaySelectNewMatch, OnSelectMatch);
		
		LoadFilteredList (m_DocProject, m_List, m_List.filter, m_List.search);
		LoadFilteredList (m_DocProject, m_MatchList, m_MatchList.filter, m_MatchList.search);
	}
		
	public static void LoadFilteredList (NewDataItemProject project, MemberList list, MemberFilter filter, string search)
	{
		List<MemberItem> filteredMembers;
		switch (filter)
		{
			case MemberFilter.Everything:
				filteredMembers = project.GetFilteredMembersForProgramming (Presence.DontCare, Presence.DontCare);
				break;
			case MemberFilter.AllApi:
				filteredMembers = project.GetFilteredMembersForProgramming (Presence.SomeOrAllPresent, Presence.DontCare);
				break;
			case MemberFilter.AllDocumented:
				filteredMembers = project.GetFilteredMembersForProgramming (Presence.DontCare, Presence.SomeOrAllPresent);
				break;
			case MemberFilter.ApiWithoutDocs:
				filteredMembers = project.GetFilteredMembersForProgramming (Presence.SomeOrAllPresent, Presence.SomeOrAllAbsent);
				break;
			case MemberFilter.DocsWithoutApi:
				filteredMembers = project.GetFilteredMembersForProgramming (Presence.SomeOrAllAbsent, Presence.SomeOrAllPresent);
				break;
			case MemberFilter.MismatchedApiAndDocs:
				filteredMembers = project.GetFilteredMembersForProgramming (Presence.SomeOrAllAbsent, Presence.SomeOrAllAbsent);
				break;
			case MemberFilter.AllTranslated:
				filteredMembers = project.GetFilteredMembersForTranslation (Presence.DontCare, Presence.SomeOrAllPresent);
				break;
			case MemberFilter.SourceWithoutTranslation:
				filteredMembers = project.GetFilteredMembersForTranslation (Presence.SomeOrAllPresent, Presence.SomeOrAllAbsent);
				break;
			case MemberFilter.TranslationWithoutSource:
				filteredMembers = project.GetFilteredMembersForTranslation (Presence.SomeOrAllAbsent, Presence.SomeOrAllPresent);
				break;
			default:
				filteredMembers = new List<MemberItem> ();
				break;
		}
		
		if (search != "")
		{
			string searchAdjusted = search.ToLower ().Replace ("_", "");
			filteredMembers = filteredMembers.Where (elem => elem.ItemName.ToLower ().Replace ("_", "").IndexOf (searchAdjusted) >= 0).ToList ();
		}

		filteredMembers = filteredMembers.OrderBy (m => m.ItemName).ToList ();
		list.members = filteredMembers;
	}
}
