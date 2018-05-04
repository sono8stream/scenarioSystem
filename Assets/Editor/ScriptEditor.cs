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
    const int messageWinWidth = 600;
    const int commandWidth = 400;
    const int labelWidth = 120;

    static VariableEditor variableEditor;
    static string[] sceneNames;

    bool okDialog = false;

    TextAsset script = null;
    string scriptName;
    List<string> scriptLines = new List<string>() { "" };
    List<bool> scriptToggles = new List<bool>() { false };
    Vector2 windowScrollPos = Vector2.zero;
    Vector2 scriptScrollPos = Vector2.zero;
    Vector2 commandScrollPos = Vector2.zero;
    int varIndex = 0;
    bool onDetectScroll = false;
    float maxScrollY;
    float maxY = 10000;

    CommandMessages messages = new CommandMessages();
    string inputMessage = "";
    int messageSpeed;
    string choiceName;
    List<string> choiceNameList = new List<string>();

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
    string[] conditionOperators = new string[5] { "<", "<=", ">", ">=", "=" };
    int operatorIndex = 0;
    int conditionOperatorIndex = 0;
    int changeVal;
    int subVarIndex = 0;

    ScriptUndo scriptUndo;

    [MenuItem("Editor/ScriptEditor")]
    private static void OnCreate()
    {
        //最もよくわかっていない部分　この順番じゃないとダメっぽい
        ScriptEditor editor = GetWindow<ScriptEditor>();
        editor.position = new Rect(100, 100, 1050, 510);//サイズ変更
        variableEditor = GetWindow<VariableEditor>(typeof(ScriptEditor));
        editor.Focus();
        variableEditor.Initialize();

        InitializeSceneNames();
    }

    private void OnGUI()
    {
        windowScrollPos = EditorGUILayout.BeginScrollView(windowScrollPos);
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
        }//ファイル読み込み
        EditorGUILayout.Space();

        using (new GUILayout.HorizontalScope(GUILayout.Height(450)))
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScriptGUI();
                TextBoxGUI();
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                commandScrollPos 
                    = EditorGUILayout.BeginScrollView(commandScrollPos);
                MessageGUI();
                ImageGUI();
                SoundGUI();
                SceneGUI();
                VariableGUI();
                EditorGUILayout.EndScrollView();
            }
        }

        GUILayout.FlexibleSpace();

        SaveButton();
        EditorGUILayout.EndScrollView();
    }

    #region file management

    void SaveButton()
    {
        if (GUILayout.Button("Save") && !string.IsNullOrEmpty(scriptName))
        {
            string rawScript = "";
            foreach (string s in scriptLines)
            {
                rawScript += s + Environment.NewLine;
            }
            if (scriptLines.Count > 0)
            {
                rawScript = rawScript.Substring(
                    0, rawScript.Length - Environment.NewLine.Length * 2);//最後の空行消す
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
        scriptLines.Add("");
        int lineCount = scriptLines.Count;
        scriptToggles = new List<bool>();
        //scriptToggles.Add(true);
        for (int i = 0; i < lineCount; i++)
        {
            scriptToggles.Add(false);
        }

        scriptUndo = new ScriptUndo(scriptLines);
        onDetectScroll = true;
        scriptScrollPos.y = maxY;
        Debug.Log(scriptToggles.Count);
        
    }
    #endregion

    #region script box management
    void ScriptGUI()
    {
        using (new GUILayout.VerticalScope(GUILayout.Width(messageWinWidth)))
        {
            scriptScrollPos = EditorGUILayout.BeginScrollView(
                scriptScrollPos, GUI.skin.box);
            WriteScript();
            EditorGUILayout.EndScrollView();

            SwitchFocusedToggle();
            if (onDetectScroll && scriptScrollPos.y != maxY)
            {
                maxScrollY = scriptScrollPos.y;
                onDetectScroll = false;
                JumpLine(scriptToggles.IndexOf(true) - 1);
                Debug.Log("Detect");
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("空行追加"))
                {
                    InsertLine(" ");
                }
                if (GUILayout.Button("↓コピー"))
                {
                    int index = scriptToggles.FindIndex(x => x);
                    if (index >= 0)
                    {
                        inputMessage = scriptLines[index];
                    }
                }
                /*if (GUILayout.Button("全選択 解除", EditorStyles.miniButtonMid))
                {
                    scriptToggles = scriptToggles.Select(x => x = false).ToList();
                }*/
                if (GUILayout.Button("行削除"))
                {
                    RemoveSelectedLine();
                }
            }

            if (scriptUndo == null) scriptUndo = new ScriptUndo(scriptLines);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("一つ戻す"))
                {
                    scriptUndo.Undo();
                }
                if (GUILayout.Button("やり直す"))
                {
                    scriptUndo.Redo();
                }
            }
        }
    }

    void WriteScript()
    {
        int cnt = 0;
        foreach (string s in scriptLines)
        {
            using (new GUILayout.HorizontalScope())
            {
                scriptToggles[cnt] = EditorGUILayout.Toggle(
                    scriptToggles[cnt],GUILayout.Width(15));
                GUI.SetNextControlName(cnt.ToString());//毎回セットする必要あり
                EditorGUILayout.SelectableLabel(messages.GetCommandMessage(s),
                    GUI.skin.textField, GUILayout.Height(16));
            }
            cnt++;
        }
    }

    void InsertLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        /*int length = scriptToggles.Count;
        for (int i = 0; i < length; i++)
        {
            if (!scriptToggles[i]) continue;

            scriptLines.Insert(i, text);
            scriptToggles.Insert(i, false);
            length++;
            i++;
            Debug.Log("Inserted");
        }

        if (!scriptToggles[scriptToggles.Count - 1] && length == scriptToggles.Count)
        {
            int index = scriptToggles.Count - 1;
            scriptLines.Insert(index, text);
            scriptToggles.Insert(index, false);
            scriptToggles[index + 1] = true;
        }*/
        int index = scriptToggles.IndexOf(true);
        if (index == -1)
        {
            index = scriptToggles.Count - 1;
        }
        scriptUndo.AddInsertOperation(index);
        scriptLines.Insert(index, text);
        scriptToggles.Insert(index, false);
        scriptToggles[index + 1] = true;

        scriptScrollPos.y = maxY;
        onDetectScroll = true;
    }

    void RemoveSelectedLine()
    {
        int index = scriptToggles.IndexOf(true);
        if (index == -1 || index == scriptToggles.Count - 1) return;

        scriptUndo.AddRemoveOperation(index, scriptLines[index]);
        scriptLines.RemoveAt(index);
        scriptToggles.RemoveAt(index);
        scriptToggles[index] = true;
        /*int checkLength = scriptToggles.Count - 1;
        /*for(int i = 0; i < checkLength; i++)
        {
            if (!scriptToggles[i]) continue;

            scriptLines.RemoveAt(i);
            scriptToggles.RemoveAt(i);
            checkLength--;
            i--;
        }*/

        scriptScrollPos.y = maxY;
        onDetectScroll = true;
    }

    void SwitchFocusedToggle()
    {
        int index;
        string indexText = GUI.GetNameOfFocusedControl();
        if (indexText.Equals("") || !int.TryParse(indexText, out index)
            || index >= scriptLines.Count) return;

        index = scriptToggles[index] ? scriptToggles.Count - 1 : index;
        scriptToggles = scriptToggles.Select(x => false).ToList();
        scriptToggles[index] = true;
        GUI.FocusControl("");
    }

    void JumpLine(int index)
    {
        if (index == -1) index = 0;
        scriptScrollPos.y = maxScrollY * index / (scriptToggles.Count - 1);
        Repaint();
        Debug.Log(scriptScrollPos.y);
    }
    #endregion

    #region command management
    void TextBoxGUI()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box,
            GUILayout.Width(messageWinWidth)))
        {
            GUILayout.Label("・メッセージ");
            //max length: 54*3=162

            inputMessage = EditorGUILayout.TextArea(inputMessage,
                /*GUILayout.Width(messageWinWidth),*/ GUILayout.Height(45));
            using (new GUILayout.HorizontalScope())
            {
                string[] strs = Regex.Split(inputMessage, "\r\n|\r|\n");
                if (GUILayout.Button("書き込み(入力待ち初期化)"))
                {
                    foreach (string s in strs)
                    {
                        InsertLine(s);
                    }
                    InsertLine(@"[m\1\0]");
                }
                if (GUILayout.Button("書き込み(入力待ち)"))
                {
                    foreach (string s in strs)
                    {
                        InsertLine(s);
                    }
                    InsertLine(@"[m\2\0]");
                }
                if (GUILayout.Button("書き込み"))
                {
                    foreach (string s in strs)
                    {
                        InsertLine(s);
                    }
                }
                if (GUILayout.Button("ジャンプ"))
                {
                    if (strs.Length > 0 && !string.IsNullOrEmpty(strs[0]))
                    {
                        InsertLine("→" + strs[0]);
                    }
                }
                if (GUILayout.Button("ラベル"))
                {
                    if (strs.Length > 0 && !string.IsNullOrEmpty(strs[0]))
                    {
                        InsertLine("ー" + strs[0]);
                    }
                }
                if (GUILayout.Button("コメント"))
                {
                    foreach (string s in strs)
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            InsertLine("//" + s);
                        }
                    }
                }
                if (GUILayout.Button("クリア"))
                {
                    foreach (string s in strs)
                    {
                        inputMessage = "";
                    }
                }
            }

            if (variableEditor == null || variableEditor.AllVariableNames == null)
            {
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                varIndex = EditorGUILayout.Popup(
                    "・変数", varIndex, variableEditor.AllVariableNames);
                if (GUILayout.Button("変数入力"))
                {
                    inputMessage += variableEditor.GetVariableNameByIndex(varIndex);
                }
            }
        }
    }

    void MessageGUI()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box,
            GUILayout.Width(commandWidth)))
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("・表示速度(0～10)",
                    GUILayout.Width(labelWidth));
                messageSpeed = Mathf.Clamp(EditorGUILayout.IntField(messageSpeed), 0, 10);
                if (GUILayout.Button("速度変更"))
                {
                    InsertLine(string.Format(@"[m\4\{0}]", messageSpeed.ToString()));
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("・選択肢", GUILayout.Width(labelWidth));
                choiceName = EditorGUILayout.TextField(choiceName);
                if (GUILayout.Button("選択肢追加"))
                {
                    InsertLine(string.Format(@"[m\5\{0}]", choiceName));
                    choiceNameList.Add(choiceName);
                }
            }

            if (GUILayout.Button("選択待ち"))
            {
                InsertLine(@"[m\6\0]");
                int choiceCnt = choiceNameList.Count;
                for (int i = 0; i < choiceCnt; i++)
                {
                    InsertLine("ー" + choiceNameList[i]);
                    InsertLine(" ");
                }
                choiceNameList = new List<string>();
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("オート開始"))
                {
                    InsertLine(@"[m\7\100]");
                }
                if (GUILayout.Button("オート終了"))
                {
                    InsertLine(@"[m\8\0]");
                }
            }
        }
    }

    void ImageGUI()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box,
            GUILayout.Width(commandWidth)))
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("・キャラ番号(0～3)",
                    GUILayout.Width(labelWidth));
                charaNo = Mathf.Clamp(EditorGUILayout.IntField(charaNo), 0, 3);
            }

            using (new GUILayout.HorizontalScope())
            {
                charaSprite = EditorGUILayout.ObjectField("・キャラ画像",
                        charaSprite, typeof(Sprite), false,
                        GUILayout.Height(50)) as Sprite;
                if (charaSprite != null)
                {
                    string charaPath = CHARA_FOLDER_PATH + charaSprite.name;
                    if (!(File.Exists(charaPath + ".png")
                        || File.Exists(charaPath + ".jpg"))) charaSprite = null;
                }

                if (GUILayout.Button("キャラ画像変更") && charaSprite != null)
                {
                    InsertLine(string.Format(@"[i\1\{0}:{1}]",
                        charaNo, charaSprite.name));
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
                EditorGUILayout.LabelField("・移動幅",GUILayout.Width(labelWidth));
                moveLength = EditorGUILayout.IntField(moveLength);
                if (GUILayout.Button("キャラ移動"))
                {
                    InsertLine(string.Format(@"[i\3\{0}:{1}]", charaNo, moveLength));
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                scenerySprite = EditorGUILayout.ObjectField("・背景画像",
                        scenerySprite, typeof(Sprite), false,
                        GUILayout.Height(50)) as Sprite;
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
        using (new GUILayout.VerticalScope(GUI.skin.box,
            GUILayout.Width(commandWidth)))
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
        using (new GUILayout.VerticalScope(GUI.skin.box,
            GUILayout.Width(commandWidth)))
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
        if (variableEditor == null || variableEditor.AllVariableNames == null)
        {
            return;
        }

        using (new GUILayout.HorizontalScope(GUI.skin.box,
        GUILayout.Width(commandWidth)))
        {
            EditorGUILayout.LabelField("・変数操作",
                GUILayout.Width(100));
            changeVarIndex = EditorGUILayout.Popup(
                 changeVarIndex, variableEditor.AllVariableNames,
                 GUILayout.Width(80));

            operatorIndex = EditorGUILayout.Popup("", operatorIndex, operators,
                GUILayout.Width(30));

            using (new GUILayout.VerticalScope())
            {
                changeVal
                    = EditorGUILayout.IntField(changeVal, GUILayout.Width(80));
                subVarIndex = EditorGUILayout.Popup(
                    "", subVarIndex, variableEditor.AllVariableNames,
                    GUILayout.Width(80));
            }

            using (new GUILayout.VerticalScope())
            {
                if (GUILayout.Button("変数変更"))
                {
                    InsertLine(string.Format(@"[v\0\{0}:{1}:{2}]",
                        variableEditor.GetVariableNameByIndex(changeVarIndex),
                        operators[operatorIndex], changeVal));
                }
                if (GUILayout.Button("変数変更"))
                {
                    InsertLine(string.Format(@"[v\0\{0}:{1}:{2}]",
                        variableEditor.GetVariableNameByIndex(changeVarIndex),
                        operators[operatorIndex],
                        variableEditor.GetVariableNameByIndex(subVarIndex)));
                }
            }
        }
        using (new GUILayout.HorizontalScope(GUI.skin.box,
        GUILayout.Width(commandWidth)))
        {
            EditorGUILayout.LabelField("・変数操作",
                GUILayout.Width(100));
            changeVarIndex = EditorGUILayout.Popup(
                   changeVarIndex, variableEditor.AllVariableNames,
                   GUILayout.Width(80));

            conditionOperatorIndex = EditorGUILayout.Popup("",
                conditionOperatorIndex, conditionOperators, GUILayout.Width(30));

            using (new GUILayout.VerticalScope())
            {
                changeVal
                    = EditorGUILayout.IntField(changeVal, GUILayout.Width(80));
                subVarIndex = EditorGUILayout.Popup(
                    "", subVarIndex, variableEditor.AllVariableNames,
                    GUILayout.Width(80));
            }

            using (new GUILayout.VerticalScope())
            {
                if (GUILayout.Button("変数分岐"))
                {
                    string varName = variableEditor.GetVariableNameByIndex(changeVarIndex);
                    string condition = conditionOperators[conditionOperatorIndex];
                    InsertLine(string.Format(@"[v\1\{0}:{1}:{2}]",
                        varName, condition, changeVal));
                    InsertLine(string.Format(
                        "ー{0}{1}{2}", varName, condition, changeVal));
                    InsertLine(string.Format(
                        "ーNot {0}{1}{2}", varName, condition, changeVal));
                }
                if (GUILayout.Button("変数分岐"))
                {
                    string varName = variableEditor.GetVariableNameByIndex(changeVarIndex);
                    string condition = conditionOperators[conditionOperatorIndex];
                    string subVarName = variableEditor.GetVariableNameByIndex(subVarIndex);
                    InsertLine(string.Format(
                        @"[v\1\{0}:{1}:{2}]", varName, condition, subVarName));
                    InsertLine(string.Format(
                        "ー{0}{1}{2}", varName, condition, subVarName));
                    InsertLine(string.Format(
                        "ーNot {0}{1}{2}", varName, condition, subVarName));

                }
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

    string Translater(string english, string japanese)
    {
        return japanese;
    }
}