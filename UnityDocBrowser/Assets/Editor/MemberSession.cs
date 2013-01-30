using UnityEngine;
using UnityEditor;
using UnderlyingModel;
using MemDoc;

[System.Serializable]
internal class MemberSession
{
	public LanguageUtil.ELanguage Language;
	public bool Translated { get { return Language != LanguageUtil.ELanguage.English; } }
	public NewDataItemProject DocProject;
	public MemberItem Member = null;
	public MemDocModel DummyModel { get; private set; }
	private bool m_IsDummy = false;
	public MemDocModel Model
	{
		get
		{
			if (DummyModel != null)
				return DummyModel;
			if (Translated)
				return Member.DocModelTranslated;
			return Member.DocModel;
		}
	}
	public string TextOrig = "";
	public string TextCurrent = "";
	public bool Dirty = false;
	public bool DirtyAutoChanges = false;
	public StringDiff Diff;

	public MemberSession (NewDataItemProject docProject, MemberItem member, LanguageUtil.ELanguage language) {
		DocProject = docProject;
		Language = language;
		Member = member;
		
		Load ();
	}
	
	public MemberSession (NewDataItemProject docProject, MemberItem member, string oldMemberText, LanguageUtil.ELanguage language) {
		DocProject = docProject;
		Language = language;
		Member = member;
		
		m_IsDummy = true;
		DummyModel = member.LoadDoc (oldMemberText, Language, false, false);
		if (language == LanguageUtil.ELanguage.English)
			Model.EnforcePunctuation ();
		Model.SanitizeForEditing ();
		
		TextOrig = Model.ToString ();
		TextCurrent = TextOrig;
	}
	
	public void OnEnable (MemberItem member)
	{
		if (m_IsDummy)
		{
			Member = member;
			DummyModel = member.LoadDoc (TextOrig, Language, false, false);
			if (Language == LanguageUtil.ELanguage.English)
				DummyModel.EnforcePunctuation ();
			DummyModel.SanitizeForEditing ();
		}
		else
		{
			Member = member;
			member.LoadDoc (Language);
			member.LoadDoc (TextCurrent, Language, false);
			if (Language == LanguageUtil.ELanguage.English)
				Model.EnforcePunctuation ();
			Model.SanitizeForEditing ();
			if (Diff == null)
				Diff = new StringDiff ();
			Diff.Compare (TextOrig, TextCurrent);
		}
	}
	
	public bool Loaded { get { return Member != null && Model != null && Diff != null; } }

	public void Load ()
	{
		if (m_IsDummy)
			return;
		
		if (Member == null)
		{
			Debug.LogError ("Cannot load member since it is null.");
			return;
		}
		
		Member.LoadDoc (Language);
		if (Language == LanguageUtil.ELanguage.English)
			Model.EnforcePunctuation ();
		Model.SanitizeForEditing ();
		
		TextOrig = Model.ToString ();
		TextCurrent = TextOrig;
		if (Diff == null)
			Diff = new StringDiff ();
		Diff.Compare (TextOrig, TextCurrent);
		
		Dirty = false;
		DirtyAutoChanges = false;
	}

	public void Save ()
	{
		if (m_IsDummy)
			return;
		
		Member.SaveDoc (Translated);
		Load ();
	}

	public bool LeaveWithPermission ()
	{
		if (m_IsDummy || !Loaded || !Dirty)
		{
			if (DirtyAutoChanges)
				Load ();
			return true;
		}
		int choice = EditorUtility.DisplayDialogComplex (
			"Save Documentation for Member",
			"Do you want to save the "+Language+" documentation for "+Member.ItemName +"?",
			"Save", // 0
			"Don't Save", // 1
			"Cancel" // 2
		);
		if (choice == 2)
			return false;
		if (choice == 0)
			Save ();
		else
			Load ();
		return true;
	}
	
	public void LeaveForced ()
	{
		if (m_IsDummy || !Dirty)
		{
			if (DirtyAutoChanges)
				Load ();
			return;
		}
		if (EditorUtility.DisplayDialog (
			"Save Documentation for Member",
			"Do you want to save the "+Language+" documentation for "+Member.ItemName+"?",
			"Save",
			"Don't Save"
		))
			Save ();
		else
			Load ();
	}
	
	public void OnModelEdited (bool reload = false)
	{
		TextCurrent = Model.ToString ();
		Dirty = true;
		if (reload)
			Member.LoadDoc (TextCurrent, Language);
		Diff.Compare (TextOrig, TextCurrent);
	}
}
