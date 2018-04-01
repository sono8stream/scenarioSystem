using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

public class WindowSample : EditorWindow
{
    //const string NEW_LINE = "\r\n";//for win

    bool okDialog = false;
    TextAsset script = null;
    //string scriptText;
    List<string> scriptLines = new List<string>();
    string scriptName;
    string inputMessage;
    Vector2 scriptScrollPosition = Vector2.zero;
    CommandMessages messages = new CommandMessages();

    [MenuItem("Editor/ScriptEditor")]
    private static void OnCreate()
    {
        GetWindow<WindowSample>("スクリプトエディタ");
    }

    private void OnGUI()
    {
        using (new GUILayout.HorizontalScope())
        {
            TextAsset tempScript = EditorGUILayout.ObjectField("・スクリプト",
                    script, typeof(TextAsset), false, GUILayout.Width(300)) as TextAsset;
            EditorGUILayout.LabelField("・保存ファイル名");
            scriptName = EditorGUILayout.TextField(scriptName);
            if (tempScript != script)
            {
                script = tempScript;
                //scriptText = script.text;
                /*scriptLines = new List<string>();
                scriptLines.AddRange(Regex.Split(script.text, "\r\n|\r|\n"));*/
                LoadScript();
                scriptName = script.name;
            }
        }
        EditorGUILayout.Space();

        using (new GUILayout.HorizontalScope(GUILayout.Width(900)))
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(600)))
            {
                scriptScrollPosition = EditorGUILayout.BeginScrollView(
                        scriptScrollPosition, GUI.skin.box);
                WriteScript();
                EditorGUILayout.EndScrollView();
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("空行追加", EditorStyles.miniButtonLeft))
                    {
                        InsertLine("");
                    }
                    if (GUILayout.Button("削除", EditorStyles.miniButtonRight))
                    {
                        RemoveSelectedLine();
                    }
                }
                EditorGUILayout.Space();

                GUILayout.Label("・メッセージ");
                //max length: 54*3=162
                inputMessage
                    = EditorGUILayout.TextArea(inputMessage, GUILayout.Height(45));
                using (new GUILayout.HorizontalScope())
                {
                    string[] strs = Regex.Split(inputMessage, "\r\n|\r|\n");
                    if (GUILayout.Button("書き込み(入力待ち初期化)", EditorStyles.miniButtonLeft))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine(s);
                        }
                        InsertLine(@"[m\1\0]");
                    }
                    if (GUILayout.Button("書き込み(入力待ち)", EditorStyles.miniButtonMid))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine(s);
                        }
                        InsertLine(@"[m\2\0]");
                    }
                    if (GUILayout.Button("書き込み", EditorStyles.miniButtonMid))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine(s);
                        }
                    }
                    if (GUILayout.Button("コメント", EditorStyles.miniButtonRight))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine("//"+s);
                        }
                    }
                }
            }

            using (new GUILayout.VerticalScope(GUILayout.Width(300)))
            {
                using(new GUILayout.VerticalScope(GUI.skin.box))//メッセージコマンド
                {
                    if (GUILayout.Button("文字表示"))
                    {

                    }
                }
                GUILayout.FlexibleSpace();

                SaveButton();
            }
        }
        EditorGUILayout.Space();
        //Debug.Log(GUI.GetNameOfFocusedControl());
    }

    #region file management

    void SaveButton()
    {
        if (GUILayout.Button("Save")&&!string.IsNullOrEmpty(scriptName))
        {
            string rawScript = "";
            foreach (string s in scriptLines)
            {
                rawScript += s + Environment.NewLine;
            }
            rawScript = rawScript.Substring(0, rawScript.Length - Environment.NewLine.Length);
            SaveScript(rawScript);
        }
    }

    void SaveScript(string scriptText)
    {
        string path = Application.dataPath + string.Format(
                "\\Script\\ScenarioSystem\\ScenarioScript\\{0}.txt", scriptName);

        if (File.Exists(path))
        {
            Debug.Log("Another File Exists!");

            okDialog = EditorUtility.DisplayDialog(
                "ファイル重複", "上書きしますか?", "OK", "キャンセル");

            if (okDialog)
            {
                using (StreamWriter sw = new StreamWriter(path, false))
                {
                    sw.Write(scriptText);
                    sw.Flush();
                    sw.Close();
                }
                Debug.Log("Over Write Data!");
            }
        }
        else
        {
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.Write(scriptText);
                sw.Flush();
                sw.Close();
            }
            Debug.Log("Save Data!");
        }
    }

    void LoadScript()
    {
        string path = Application.dataPath + string.Format(
                "\\Script\\ScenarioSystem\\ScenarioScript\\{0}.txt", script.name);
        if (!File.Exists(path)) return;

        using (StreamReader sr = new StreamReader(path, false))
        {
            scriptLines = new List<string>();
            string line;
            while((line=sr.ReadLine())!=null)
            {
                scriptLines.Add(line);
            }
            sr.Close();
        }
    }
    #endregion

    void WriteScript()
    {
        if (script == null) return;

        //string[] strs = Regex.Split(scriptText, Environment.NewLine);
        int cnt = 0;
        //foreach (string s in strs)
        foreach (string s in scriptLines)
        {
            GUI.SetNextControlName(cnt.ToString());//毎回セットする必要あり
            EditorGUILayout.SelectableLabel(messages.GetCommandMessage(s),
                GUI.skin.textField, GUILayout.Height(16));
            cnt++;
        }
    }

    void InsertLine(string text)
    {
        //scriptText += text + Environment.NewLine;
        int index = SelectedIndex();
        //if (scriptLines.Count == 0) index = 0;
        Debug.Log(index);
        scriptLines.Insert(index,text);
        GUI.FocusControl(index.ToString());
    }

    void RemoveSelectedLine()
    {
        int index;
        string indexText = GUI.GetNameOfFocusedControl();
        if (indexText.Equals("") || !int.TryParse(indexText, out index)
            || index >= scriptLines.Count) return;

        scriptLines.RemoveAt(index);
        if (scriptLines.Count == 0) return;

        string focus = index > 0 ? (index - 1).ToString() : "";
        GUI.FocusControl(focus);//表示更新のため、フォーカスを外す
        Repaint();
    }

    int SelectedIndex()
    {
        int index;
        string indexText = GUI.GetNameOfFocusedControl();
        if (indexText.Equals("") || !int.TryParse(indexText, out index)
            ||index>=scriptLines.Count)
        {
            index = scriptLines.Count;
        }
        else
        {
            index++;
        }
        return index;
    }
}
