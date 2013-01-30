using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnderlyingModel;
using Mono.Cecil;

public enum MemberFilter
{
	Everything,
	AllApi,
	AllDocumented,
	ApiWithoutDocs,
	DocsWithoutApi,
	MismatchedApiAndDocs,
	AllTranslated,
	SourceWithoutTranslation,
	TranslationWithoutSource
}

static class Extensions
{
	public static string GetNiceName(string t)
	{
		switch (t)
		{
			case "Int32": return "int";
			case "Single": return "float";
			case "Boolean": return "bool";
			case "String": return "string";
			default: return t;
		}
	}
}

public partial class DocBrowser
{
	public class Styles
	{
		public float dividerSpace = 4;
		
		public GUIStyle frameWithMargin = new GUIStyle ("OL BOX");
		public GUIStyle frameWithMarginWhite;
		public GUIStyle frameWithMarginClear;
		public GUIStyle frameStatusBar = new GUIStyle ("OL BOX");
		
		public GUIStyle listItem = new GUIStyle ("HI Label");
		public GUIStyle listItemWhite;
		public GUIStyle columnHeader = new GUIStyle ("OL TITLE");
		
		public GUIStyle miniButton = new GUIStyle (EditorStyles.miniButton);
		public GUIStyle miniIcon;
		
		public GUIStyle searchField = new GUIStyle ("ToolbarSeachTextFieldPopup");
		public GUIStyle searchFieldCancelButton = new GUIStyle ("ToolbarSeachCancelButton");
		
		public GUIStyle languageHeader = new GUIStyle (EditorStyles.toolbarButton);
		public GUIStyle docArea;
		public GUIStyle docHeader = new GUIStyle (EditorStyles.largeLabel);
		public GUIStyle docSectionHeader = new GUIStyle (EditorStyles.miniBoldLabel);
		public GUIStyle docSectionMessage;
		public GUIStyle docSectionMessageWarning;
		public GUIStyle signature = new GUIStyle (EditorStyles.label);
		public GUIStyle paramLabel = new GUIStyle (EditorStyles.label);
		public GUIStyle param = new GUIStyle (EditorStyles.textField);
		public GUIStyle description;
		public GUIStyle example = new GUIStyle (EditorStyles.textField);
		public GUIStyle dif = new GUIStyle (EditorStyles.label);
		
		public GUIStyle dragHandle = new GUIStyle ("RL DragHandle");
		public GUIStyle dragInsertLine = new GUIStyle ();
		
		public Texture2D docCanvasTex;
		public Texture2D docTextAreaTex;
		public Texture2D docExampleTextAreaTex;
		public Color nonEditColor = new Color (0, 0, 0, 0.8f);
		public Color pureTextColor = Color.black;
		
		public Texture2D iconRemove;
		public Texture2D iconAdd;
		
		private Texture2D LoadGUITexture (string name)
		{
			return (Texture2D)Resources.Load (name, typeof (Texture2D));
		}
		
		private Color InvertColor (Color col)
		{
			return new Color (1-col.r, 1-col.g, 1-col.b, col.a);
		}
		
		public Styles ()
		{
			if (EditorGUIUtility.isProSkin)
			{
				nonEditColor = new Color (1, 1, 1, 0.5f);
				pureTextColor = new Color (1, 1, 1, 0.7f);
			}
			
			if (EditorGUIUtility.isProSkin)
			{
				docCanvasTex = LoadGUITexture ("DocCanvasDimDark");
				docTextAreaTex = LoadGUITexture ("DocTextAreaDark");
				docExampleTextAreaTex = LoadGUITexture ("DocExampleTextAreaDark");
			}
			else
			{
				docCanvasTex = LoadGUITexture ("DocCanvasDim");
				docTextAreaTex = LoadGUITexture ("DocTextArea");
				docExampleTextAreaTex = LoadGUITexture ("DocExampleTextArea");
			}
			
			iconRemove     = LoadGUITexture ("IconRemove");
			iconAdd        = LoadGUITexture ("IconAdd");
			
			dragInsertLine.normal.background = LoadGUITexture ("DragInsertLine");
			
			frameStatusBar.margin = new RectOffset (0, 0, 0, 0);
			frameStatusBar.overflow = new RectOffset (1, 1, 1, 1);
			frameStatusBar.stretchHeight = false;
			frameStatusBar.padding = new RectOffset (4, 4, 1, 1);
			frameStatusBar.normal.textColor = pureTextColor;
			
			frameWithMargin.margin = new RectOffset (1, 0, 0, 1);
			frameWithMargin.overflow = new RectOffset (1, 1, 1, 1);
			
			frameWithMarginWhite = new GUIStyle (frameWithMargin);
			frameWithMarginWhite.normal.background = docCanvasTex;
			frameWithMarginWhite.border = new RectOffset (1, 1, 1, 1);
			
			frameWithMarginClear = new GUIStyle (frameWithMargin);
			frameWithMarginClear.normal.background = null;
			
			docArea = new GUIStyle (frameWithMarginWhite);
			docArea.margin = new RectOffset (0, 0, 0, 0);
			
			listItem.padding.left = 4;
			listItem.overflow = new RectOffset (0, 0, 0, 0);
			if (EditorGUIUtility.isProSkin)
			{
				listItem.normal.textColor = pureTextColor;
				listItem.focused.textColor = pureTextColor;
			}
			
			listItemWhite = new GUIStyle (listItem);
			GUIStyleState state = listItemWhite.normal;
			state.background = EditorGUIUtility.whiteTexture;
			listItemWhite.normal = state;
			listItemWhite.active = state;
			listItemWhite.focused = state;
			state = listItemWhite.onNormal;
			state.background = EditorGUIUtility.whiteTexture;
			listItemWhite.onNormal = state;
			listItemWhite.onActive = state;
			listItemWhite.onFocused = state;

			columnHeader.overflow = new RectOffset (1, 1, 1, 1);
			columnHeader.padding.left = 4;
			
			languageHeader.overflow = EditorStyles.toolbar.overflow;
			languageHeader.normal = EditorStyles.toolbar.normal;
			
			miniButton.padding = new RectOffset (2, 2, 2, 2);
			miniIcon = new GUIStyle (miniButton);
			miniIcon.normal.background = null;
			
			// Doc edit styles
			
			docHeader.fontStyle = FontStyle.Bold;
			docHeader.wordWrap = true;
			docHeader.normal.textColor = nonEditColor;
			
			signature.wordWrap = true;
			signature.normal.textColor = nonEditColor;
			signature.richText = true;
			
			docSectionMessage = new GUIStyle (signature);
			docSectionMessage.padding.left = 20;
			
			docSectionMessageWarning = new GUIStyle (docSectionMessage);
			docSectionMessageWarning.normal.textColor = Color.red;
			
			docSectionHeader.margin.bottom = 0;
			docSectionHeader.margin.top = 10;
			docSectionHeader.normal.textColor = pureTextColor;
			
			paramLabel.normal.textColor = nonEditColor;
			paramLabel.richText = true;
			
			param.normal.background = docTextAreaTex;
			param.normal.textColor = pureTextColor;
			param.focused.background = null;
			param.overflow = new RectOffset (0, 0, 0, 1);
			param.padding = new RectOffset (3, 3, 3, 3);
			param.border = new RectOffset (1, 1, 1, 1);
			param.margin = new RectOffset (4, 4, 0, 0);
			param.wordWrap = true;
			
			description = param;
			
			example = new GUIStyle (param);
			example.normal.background = docExampleTextAreaTex;
			example.margin.bottom = 10;
			
			// Diff styles
			dif.margin = new RectOffset (0, 0, 0, 0);
			dif.padding = new RectOffset (6, 6, 0, 0);
			dif.normal.background = EditorGUIUtility.whiteTexture;
			dif.normal.textColor = pureTextColor;
		}
	}
	
	private static IList sDraggingList;
	private static int sDraggingIndex;
	private static Type sDraggingType;
	private static bool sReleaseHotControl;
	
	public static void DragTarget (Rect rect, Type t, IList list, int index)
	{
		if (sDraggingType != t)
			return;
		Event evt = Event.current;
		switch (evt.type)
		{
			case EventType.Repaint:
			{
				if (rect.Contains (evt.mousePosition) && !(list == sDraggingList && (index == sDraggingIndex || index == sDraggingIndex+1)))
					DocBrowser.styles.dragInsertLine.Draw (new Rect (rect.x, rect.y+rect.height/2-4, rect.width, 8), false, false, false, false);
				break;
			}
			case EventType.MouseUp:
			{
				if (rect.Contains (evt.mousePosition) && !(list == sDraggingList && (index == sDraggingIndex || index == sDraggingIndex+1)))
				{
					list.Insert (index, sDraggingList[sDraggingIndex]);
					if (list == sDraggingList && index < sDraggingIndex)
						sDraggingIndex++;
					sDraggingList.RemoveAt (sDraggingIndex);
					
					GUI.changed = true;
					Event.current.Use ();
					sReleaseHotControl = true;
				}
				break;
			}
		}
	}
	
	public static void DragHandle (Rect rect, Type t, IList list, int index, GUIStyle style)
	{
		int id = GUIUtility.GetControlID (FocusType.Passive);
		Event evt = Event.current;
		switch (evt.type)
		{
			case EventType.Repaint:
			{
				rect.y += (rect.height - 5) / 2;
				rect.height = 5;
				rect.xMin += 2;
				rect.xMax -= 2;
				if (GUIUtility.hotControl == id)
					style.Draw (new Rect (rect.x, evt.mousePosition.y-rect.height/2, rect.width, rect.height), false, false, false, false);
				else
					style.Draw (rect, false, false, false, false);
				break;
			}
			case EventType.MouseDown:
			{
				if (rect.Contains (evt.mousePosition))
				{
					GUIUtility.hotControl = id;
					sDraggingList = list;
					sDraggingIndex = index;
					sDraggingType = t;
				}
				break;
			}
			case EventType.MouseDrag:
			{
				if (GUIUtility.hotControl == id)
					evt.Use ();
				break;
			}
			case EventType.MouseUp:
			{
				sReleaseHotControl = true;
				break;
			}
		}
	}
	
	public static void DragEnd () {
		if (sReleaseHotControl)
		{
			Event.current.Use ();
			GUIUtility.hotControl = 0;
			sDraggingType = null;
			sReleaseHotControl = false;
		}
	}
}
