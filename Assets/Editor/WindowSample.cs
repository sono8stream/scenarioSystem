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
    const string SCRIPT_FOLDER_PATH = "Assets/Resources/ScenarioScript/";
    const string CHARA_FLODER_PATH = "Assets/Resources/Portrait/";
    const string SCENERY_FLODER_PATH = "Assets/Resources/Scenery/";
    const string BGM_FLODER_PATH = "Assets/Resources/BGM/";
    const string SE_FLODER_PATH = "Assets/Resources/SE/";

    bool okDialog = false;

    TextAsset script = null;
    string scriptName;
    List<string> scriptLines = new List<string>();
    Vector2 scriptScrollPosition = Vector2.zero;

    CommandMessages messages = new CommandMessages();
    string inputMessage = "";
    int messageSpeed;
    string choiceName;

    Sprite charaSprite;
    Sprite scenerySprite;

    AudioClip bgm;
    AudioClip se;

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
                scriptName = script.name;
                LoadScript();
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
                MessageGUI();

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

    void SaveScript(string text)
    {
        string path = SCRIPT_FOLDER_PATH + string.Format("{0}.txt", scriptName);

        if (File.Exists(path) && !EditorUtility.DisplayDialog(
                "ファイル重複", "上書きしますか?", "OK", "キャンセル")) return;

        using (StreamWriter sw = new StreamWriter(path, false))
        {
            sw.Write(text);
            sw.Flush();
            sw.Close();
        }
        Debug.Log("Save Data!");
        AssetDatabase.Refresh();
        //AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        LoadScript();
    }

    void LoadScript()
    {
        string path = SCRIPT_FOLDER_PATH + string.Format("{0}.txt", scriptName);
        script = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (!File.Exists(path))
        {
            script = null;
            return;
        }

        scriptLines = new List<string>();
        scriptLines.AddRange(Regex.Split(script.text, "\r\n|\r|\n"));
    }
    #endregion

    #region script box management
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
    #endregion

    #region message management
    void MessageGUI()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            using (new GUILayout.HorizontalScope())
            {
                messageSpeed = Mathf.Clamp(EditorGUILayout.IntField(messageSpeed), 0, 10);
                if (GUILayout.Button("速度変更"))
                {
                    InsertLine(string.Format(@"[m\4\{0}]", messageSpeed.ToString()));
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                choiceName = EditorGUILayout.TextField(choiceName);
                if (GUILayout.Button("選択肢追加"))
                {
                    InsertLine(string.Format(@"[m\5\{0}]", choiceName));
                }
            }

            if (GUILayout.Button("選択待ち"))
            {
                InsertLine(@"[m\6\0]");
            }
        }
    }
    #endregion
}
