using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorExWindow : EditorWindow
{
    [MenuItem("Window/EditorEX")]
    static void Open()
    {
        GetWindow<EditorExWindow>("EditorEx");
    }

    bool toggle = false;
    bool inspectorTitlebar = false;
    string textField = "";
    string textArea = "";
    string password = "";
    float horizontalScrollbar = 0f;
    float verticalScrollbar = 0f;
    float horizontalSlider = 0f;
    float verticalSlider = 0f;
    int toolbar = 0;
    int selectionGrid = 0;
    Object objectField = null;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ようこそ！Uniyエディタ拡張の深淵へ！");
        GUILayout.Label("Label:UnityEngine側の系");
        if (GUILayout.Button("Button")) Debug.Log("Button!");
        if (GUILayout.RepeatButton("RepeatButton")) Debug.Log("RepeatButton!");
        toggle = GUILayout.Toggle(toggle, "Toggle");
        objectField = EditorGUILayout.ObjectField(
            "ObjectField", objectField, typeof(Object), true);
        if (objectField != null)
        {
            inspectorTitlebar 
                = EditorGUILayout.InspectorTitlebar(inspectorTitlebar, objectField);
            if (inspectorTitlebar)
            {
                EditorGUILayout.LabelField("ﾁﾗｯﾁﾗｯ");
            }
        }

        GUILayout.Label("TextField");
        textField = GUILayout.TextField(textField);
        GUILayout.Label("TextArea");
        textArea = GUILayout.TextArea(textArea);
        GUILayout.Label("PasswordField");
        password = GUILayout.PasswordField(password, '*');
        GUILayout.Label("HorizontalScrollbar");
        float horizontalSize = 50f;
        horizontalScrollbar = GUILayout.HorizontalScrollbar(
            horizontalScrollbar, horizontalSize, 0f, 100f);
        GUILayout.Label("VerticalScrollbar");
        float verticalSize = 50f;
        verticalScrollbar = GUILayout.VerticalScrollbar(
            verticalScrollbar, verticalSize, 0f, 100f);
        GUILayout.Label("HorizontalSlider");
        horizontalSlider = GUILayout.HorizontalSlider(horizontalSlider, 0f, 100f);
        GUILayout.Label("VerticalSlider");
        verticalSlider = GUILayout.VerticalSlider(verticalSlider, 0f, 100f);
        GUILayout.Label("Toolbar");
        toolbar = GUILayout.Toolbar(toolbar,
            new string[] { "Tool1", "Tool2", "Tool3", "Tool4" });
        GUILayout.Label("SelectionGrid");
        selectionGrid = GUILayout.SelectionGrid(selectionGrid,
            new string[] { "Grid1", "Grid2", "Grid3", "Grid4" }, 3);
        GUILayout.Box("Box");
        GUILayout.Label("ここからSpace");
        GUILayout.Space(100);
        GUILayout.Label("ここまでSpace");
        GUILayout.Label("ここからFlexibleSpace");
        GUILayout.FlexibleSpace();
        GUILayout.Label("ここまでFlexibleSpace");
    }
}
