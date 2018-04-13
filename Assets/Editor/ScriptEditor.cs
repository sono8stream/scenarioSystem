using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;

public class ScriptEditor : EditorWindow
{
    const string SCRIPT_FOLDER_PATH = "Assets/Resources/ScenarioScript/";
    const string CHARA_FOLDER_PATH = "Assets/Resources/Portrait/";
    const string SCENERY_FOLDER_PATH = "Assets/Resources/Scenery/";
    const string BGM_FOLDER_PATH = "Assets/Resources/BGM/";
    const string SE_FOLDER_PATH = "Assets/Resources/SE/";
    
    static VariableEditor variableEditor;
    static string[] sceneNames;

    bool okDialog = false;

    TextAsset script = null;
    string scriptName;
    List<string> scriptLines = new List<string>();
    Vector2 scriptScrollPosition = Vector2.zero;
    int varIndex = 0;

    CommandMessages messages = new CommandMessages();
    string inputMessage = "";
    int messageSpeed;
    string choiceName;

    int charaNo;
    int moveLength;
    Sprite charaSprite = null;
    Sprite scenerySprite = null;

    AudioClip bgm;
    AudioClip se;

    TextAsset changeScript = null;
    int sceneIndex;

    int changeVarIndex = 0;
    string[] operators = new string[5] { "=", "+=", "-=", "*=", "/=" };
    int operatorIndex=0;
    int changeVal;
    int subVarIndex = 0;

    [MenuItem("Editor/ScriptEditor")]
    private static void OnCreate()
    {
        //最もよくわかっていない部分　この順番じゃないとダメっぽい
        ScriptEditor editor = GetWindow<ScriptEditor>();
        editor.position = new Rect(150, 150, 1020, 600);//サイズ変更
        variableEditor = GetWindow<VariableEditor>(typeof(ScriptEditor));
        editor.Focus();
        variableEditor.Initialize();

        InitializeSceneNames();
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
            }
            EditorGUILayout.Space();

            using (new GUILayout.VerticalScope(GUILayout.Width(300)))
            {
                MessageGUI();
                ImageGUI();
                SoundGUI();
                SceneGUI();
                VariableGUI();
            }
        }

        using (new GUILayout.HorizontalScope(GUI.skin.box))
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("・メッセージ");
                //max length: 54*3=162

                inputMessage = EditorGUILayout.TextArea(inputMessage,
                    GUILayout.Width(600), GUILayout.Height(45));
                using (new GUILayout.HorizontalScope(GUILayout.Width(600)))
                {
                    string[] strs = Regex.Split(inputMessage, "\r\n|\r|\n");
                    if (GUILayout.Button("書き込み(入力待ち初期化)",
                        EditorStyles.miniButtonLeft))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine(s);
                        }
                        InsertLine(@"[m\1\0]");
                    }
                    if (GUILayout.Button("書き込み(入力待ち)",
                        EditorStyles.miniButtonMid))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine(s);
                        }
                        InsertLine(@"[m\2\0]");
                    }
                    if (GUILayout.Button("書き込み",
                        EditorStyles.miniButtonMid))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine(s);
                        }
                    }
                    if (GUILayout.Button("コメント",
                        EditorStyles.miniButtonRight))
                    {
                        foreach (string s in strs)
                        {
                            InsertLine("//" + s);
                        }
                    }
                }
            }

            using (new GUILayout.VerticalScope())
            {
                if (variableEditor != null && variableEditor.AllVariableNames != null)
                {
                    varIndex = EditorGUILayout.Popup(
                        "変数", varIndex, variableEditor.AllVariableNames);
                }
                if (GUILayout.Button("変数表示"))
                {
                    inputMessage += variableEditor.GetVariableNameByIndex(varIndex);
                }
            }
        }
        GUILayout.FlexibleSpace();

        SaveButton();
    }

    #region file management

    void SaveButton()
    {
        if (GUILayout.Button("Save") && !string.IsNullOrEmpty(scriptName))
        {
            string rawScript = "";
            if (scriptLines.Count > 0)
            {
                foreach (string s in scriptLines)
                {
                    rawScript += s + Environment.NewLine;
                }
                rawScript = rawScript.Substring(
                    0, rawScript.Length - Environment.NewLine.Length);
            }
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
        scriptLines.Insert(index, text);
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
        GUI.FocusControl(focus);//表示更新のため、フォーカスを変える
    }

    int SelectedIndex()
    {
        int index;
        string indexText = GUI.GetNameOfFocusedControl();
        if (indexText.Equals("") || !int.TryParse(indexText, out index)
            || index >= scriptLines.Count)
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

    #region command management
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

    void ImageGUI()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("・キャラ番号(0～3)");
                charaNo = Mathf.Clamp(EditorGUILayout.IntField(charaNo), 0, 3);
            }

            using (new GUILayout.HorizontalScope())
            {
                charaSprite = EditorGUILayout.ObjectField("・キャラ画像",
                        charaSprite, typeof(Sprite), false, GUILayout.Width(300)) as Sprite;
                if (charaSprite != null)
                {
                    string charaPath = CHARA_FOLDER_PATH + charaSprite.name;
                    if (!(File.Exists(charaPath + ".png")
                        || File.Exists(charaPath + ".jpg"))) charaSprite = null;
                }

                if (GUILayout.Button("キャラ画像変更") && charaSprite != null)
                {
                    InsertLine(string.Format(@"[i\1\{0}:{1}]", charaNo, charaSprite.name));
                }
            }

            if (GUILayout.Button("フキダシ追加"))
            {
                InsertLine(string.Format(@"[i\2\{0}]", charaNo));
            }

            if (GUILayout.Button("フキダシ非表示"))
            {
                InsertLine(@"[i\2\-1]");
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("・移動幅");
                moveLength = EditorGUILayout.IntField(moveLength);
                if (GUILayout.Button("キャラ移動"))
                {
                    InsertLine(string.Format(@"[i\3\{0}:{1}]", charaNo, moveLength));
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                scenerySprite = EditorGUILayout.ObjectField("・背景画像",
                        scenerySprite, typeof(Sprite), false, GUILayout.Width(300)) as Sprite;
                if (scenerySprite != null)
                {
                    string sceneryPath
                        = SCENERY_FOLDER_PATH + scenerySprite.name;
                    if (!(File.Exists(sceneryPath + ".png")
                        || File.Exists(sceneryPath + ".jpg"))) scenerySprite = null;
                }

                if (GUILayout.Button("背景画像変更") && scenerySprite != null)
                {
                    InsertLine(string.Format(@"[i\4\{0}]", scenerySprite.name));
                }
            }
        }
    }

    void SoundGUI()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            using (new GUILayout.HorizontalScope())
            {
                bgm = EditorGUILayout.ObjectField(
                    "・BGM", bgm, typeof(AudioClip), false) as AudioClip;
                if (bgm != null)
                {
                    string bgmPath
                        = BGM_FOLDER_PATH + bgm.name;
                    if (!(File.Exists(bgmPath + ".mp3")
                        || File.Exists(bgmPath + ".wav"))) bgm = null;
                }

                if (GUILayout.Button("BGM変更") && bgm != null)
                {
                    InsertLine(string.Format(@"[s\0\{0}]", bgm.name));
                }
            }

            if (GUILayout.Button("BGM停止"))
            {
                InsertLine(@"[s\1\0]");
            }

            if (GUILayout.Button("BGM再開"))
            {
                InsertLine(@"[s\2\0]");
            }

            using (new GUILayout.HorizontalScope())
            {
                se = EditorGUILayout.ObjectField(
                    "・SE", se, typeof(AudioClip), false) as AudioClip;
                if (se != null)
                {
                    string sePath
                        = SE_FOLDER_PATH + se.name;
                    if (!(File.Exists(sePath + ".mp3")
                        || File.Exists(sePath + ".wav"))) se = null;
                }

                if (GUILayout.Button("SE変更") && se != null)
                {
                    InsertLine(string.Format(@"[s\3\{0}]", se.name));
                }
            }
        }
    }

    void SceneGUI()
    {
        using(new GUILayout.VerticalScope(GUI.skin.box))
        {
            using (new GUILayout.HorizontalScope())
            {
                changeScript = EditorGUILayout.ObjectField(
                    "・スクリプト", changeScript, typeof(TextAsset), false) as TextAsset;
                if (changeScript != null)
                {
                    string scriptPath
                        = SCRIPT_FOLDER_PATH + changeScript.name;
                    if (!(File.Exists(scriptPath + ".txt"))) changeScript = null;
                }

                if (GUILayout.Button("スクリプト切替"))
                {
                    InsertLine(string.Format(@"[e\0\{0}]", changeScript.name));
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (sceneNames != null)
                {
                    sceneIndex = EditorGUILayout.Popup(
                        "・シーン", sceneIndex, sceneNames);
                }
                if (GUILayout.Button("シーン切り替え"))
                {
                    InsertLine(string.Format(@"[e\1\{0}]", sceneIndex));
                }
            }
        }
    }

    void VariableGUI()
    {
        using (new GUILayout.HorizontalScope(GUI.skin.box))
        {
            if (variableEditor != null && variableEditor.AllVariableNames != null)
            {
                changeVarIndex = EditorGUILayout.Popup(
                    "・変数操作", changeVarIndex, variableEditor.AllVariableNames);
            }

            operatorIndex = EditorGUILayout.Popup("", operatorIndex, operators);
            changeVal = EditorGUILayout.IntField(changeVal);

            using(new GUILayout.VerticalScope())
            {
                if (GUILayout.Button("変数変更"))
                {
                    /*InsertLine(string.Format(@"[v\0\{0}:{1}:{2}]",
                        variableEditor.AllVariableNames[changeVarIndex]));*/
                    inputMessage += variableEditor.GetVariableNameByIndex(varIndex);
                }

            }

            using (new GUILayout.VerticalScope())
            {

            }
        }
    }
    #endregion

    static void InitializeSceneNames()
    {
        int sceneCount = SceneManager.sceneCount;
        sceneNames = new string[sceneCount];
        for (int i = 0; i < sceneCount; i++)
        {
            sceneNames[i] = SceneManager.GetSceneAt(i).name;
        }
    }
}