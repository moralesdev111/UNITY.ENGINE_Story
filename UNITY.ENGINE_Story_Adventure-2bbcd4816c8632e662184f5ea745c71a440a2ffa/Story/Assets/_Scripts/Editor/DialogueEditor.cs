using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using Microsoft.SqlServer.Server;

public class DialogueEditor : EditorWindow
	{
		Dialogue selectedDialogue = null;
		[NonSerialized]
		GUIStyle nodeStyle;
		[NonSerialized]
		DialogueNode draggingNode = null;
		[NonSerialized]
		Vector2 draggingOffset;
		[NonSerialized]
		DialogueNode creatingNode = null;
		[NonSerialized]
		DialogueNode deletingNode = null;
		[NonSerialized]
		DialogueNode linkingParentNode = null;
		Vector2 scrollPosition;
		[NonSerialized]
		bool draggingCanvas = false;
		[NonSerialized]
		Vector2 draggingCanvasOffset;
		const float canvasSize =  4000;
		const float backgroundSize = 50;

		[MenuItem("Window/Dialogue Editor")]
		public static void ShowEditorWindow()
		{
			GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
		}

		[OnOpenAsset(1)]
		public static bool OnOpenAsset(int instanceID,int line)
		{
			Dialogue dialogue = EditorUtility.InstanceIDToObject(instanceID) as Dialogue; // cast to check if Dialogue
			if(dialogue !=null)
			{
				ShowEditorWindow();
				return true;
			}
			return false;
		}

		private void OnEnable()
		{
			Selection.selectionChanged += OnSelectionChanged; // event
		}

		private GUIStyle SetNodeGUIStyle(bool isPlayerSpeaking)
		{
			nodeStyle = new GUIStyle();
			nodeStyle.padding = new RectOffset(20, 20, 20, 20);
			nodeStyle.border = new RectOffset(12, 12, 12, 12);
			nodeStyle.normal.background = isPlayerSpeaking ? EditorGUIUtility.Load("node1") as Texture2D : EditorGUIUtility.Load("node0") as Texture2D;

			return nodeStyle;
		}

		private void OnSelectionChanged()
		{
			Dialogue newDialogue = Selection.activeObject as Dialogue;
			if(newDialogue !=null)
			{
				selectedDialogue = newDialogue;
				Repaint(); // updates UI
			}
		}

		private void OnGUI()
		{
			if (selectedDialogue == null)
			{
				EditorGUILayout.LabelField("No Dialogue Selected.");
				return;
			}

			if (selectedDialogue.GetAllNodes().FirstOrDefault() == null)
			{
				selectedDialogue.CreateNode(null);
			}
			else
			{
				ProcessEvents();

				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				SetEditorBackgroundTexture();

				foreach (DialogueNode node in selectedDialogue.GetAllNodes()) // loop through Getter's nodes text
				{
					DrawConnections(node);
				}
				foreach (DialogueNode node in selectedDialogue.GetAllNodes()) // loop through Getter's nodes text
				{
					DrawNode(node);
				}

				EditorGUILayout.EndScrollView();

				if (creatingNode != null)
				{
					selectedDialogue.CreateNode(creatingNode);
					creatingNode = null;
				}
				if (deletingNode != null)
				{
					selectedDialogue.DeleteNode(deletingNode);
					deletingNode = null;
				}
			}
		}

		private static void SetEditorBackgroundTexture()
		{
			Rect canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);
			Texture2D backgroundTexture = Resources.Load("background") as Texture2D;
			Rect textureCoordinates = new Rect(0, 0, canvasSize / backgroundSize, canvasSize / backgroundSize);
			GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, textureCoordinates);
		}

		private void DrawConnections(DialogueNode node)
		{
			Vector3 startPosition = new Vector2(node.GetRect().xMax, node.GetRect().center.y);
			foreach (DialogueNode childNode in selectedDialogue.GetAllChildren(node))
			{
				BezierCurveOffsetting(startPosition, childNode);
			}
		}

		private static void BezierCurveOffsetting(Vector3 startPosition, DialogueNode childNode)
		{
			Vector3 endPosition = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
			Vector3 controlPointOffset = endPosition - startPosition;
			controlPointOffset.y = 0;
			controlPointOffset.x *= 0.8f;
			Handles.DrawBezier(startPosition, endPosition,
				startPosition + controlPointOffset, endPosition - controlPointOffset,
				Color.blue, null, 5f);
		}

		private void ProcessEvents()
		{
			if (Event.current.type == EventType.MouseDown && draggingNode == null) // start dragging even
			{
				draggingNode = GetNodeAtPoint(Event.current.mousePosition + scrollPosition);
				if (draggingNode != null)
				{
					draggingOffset = draggingNode.GetRect().position - Event.current.mousePosition;
					Selection.activeObject = draggingNode;
				}
				else
				{
					draggingCanvas = true;
					draggingCanvasOffset = Event.current.mousePosition + scrollPosition;
					Selection.activeObject = selectedDialogue;
				}
			}
			else if (Event.current.type == EventType.MouseDrag && draggingNode != null)// dragging ongoing
			{				
				draggingNode.SetPosition(Event.current.mousePosition + draggingOffset);

				GUI.changed = true;
			}
			else if (Event.current.type == EventType.MouseDrag && draggingCanvas)
			{
				scrollPosition = draggingCanvasOffset - Event.current.mousePosition;

				GUI.changed = true;
			}
			else if (Event.current.type == EventType.MouseUp && draggingNode != null) // finish dragging
			{
				draggingNode = null;
			}
			else if (Event.current.type == EventType.MouseUp && draggingCanvas)
			{
				draggingCanvas = false;
			}

		}

		private void DrawNode(DialogueNode node)
		{
			GUILayout.BeginArea(node.GetRect(), SetNodeGUIStyle(node.GetIsPlayerSpeaking()));
		
			node.SetText(EditorGUILayout.TextField(node.GetText(), GUILayout.MinHeight(30)));
			GUI.skin.textField.wordWrap = true;
			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			Color defaultGUIColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Add"))
			{
				creatingNode = node;
			}

			DrawLinkButtons(node);

			GUI.backgroundColor = Color.red;
			if (GUILayout.Button("Remove"))
			{
				deletingNode = node;
			}
			GUI.backgroundColor = defaultGUIColor;
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		private void DrawLinkButtons(DialogueNode node)
		{
			GUI.backgroundColor = Color.yellow;
			if (linkingParentNode == null)
			{
				if (GUILayout.Button("Link"))
				{
					linkingParentNode = node;
				}
			}
			else if(linkingParentNode == node)
			{
				if(GUILayout.Button("Cancel"))
				{
					linkingParentNode = null;
				}
			}
			else if(linkingParentNode.GetChildren().Contains(node.name))
			{
				if (GUILayout.Button("UnLink"))
				{
					
					linkingParentNode.RemoveChild(node.name);
					linkingParentNode = null;
				}
			}
			else
			{
				if (GUILayout.Button("Link Child"))
				{					
					linkingParentNode.AddChild(node.name);
					linkingParentNode = null;
				}
			}
		}

		private DialogueNode GetNodeAtPoint(Vector2 point)
		{
			DialogueNode foundNode = null;
			foreach(DialogueNode node in selectedDialogue.GetAllNodes())
			{
				if(node.GetRect().Contains(point))
				{
					foundNode = node;
				}
			}
			return foundNode;
		}
	}

