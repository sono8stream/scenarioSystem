using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

public class VariableEditor : EditorWindow
{
    const string VARIABLE_PATH = "Assets/Resources/Variable/variableList.txt";

    TextAsset variablesAsset;
    string variablesName = "";
    List<string> variables;
    Vector2 variableScrollPosition;

    static void OnCreate()
    {
        GetWindow<VariableEditor>(typeof(ScriptEditor));
    }

    private void OnGUI()
    {
        using (new GUILayout.HorizontalScope())
        {
            variableScrollPosition = EditorGUILayout.BeginScrollView(
                    variableScrollPosition, GUI.skin.box);
            WriteVariable();
            EditorGUILayout.EndScrollView();
        }
    }

    public void Initialize()
    {
        LoadVariables();
    }

    void LoadVariables()
    {
        variablesAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(VARIABLE_PATH);
        variables = new List<string>();
        variables.AddRange(Regex.Split(variablesAsset.text, "\r\n|\r|\n"));
    }

    void WriteVariable()
    {
        if (variables == null) return;

        //string[] strs = Regex.Split(scriptText, Environment.NewLine);
        int cnt = 0;
        //foreach (string s in strs)
        foreach (string s in variables)
        {
            GUI.SetNextControlName("v" + cnt.ToString());//毎回セットする必要あり
            EditorGUILayout.SelectableLabel(s, GUI.skin.textField, GUILayout.Height(16));
            cnt++;
        }
    }
}