using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandMessages
{
    List<string> messageCommands;
    List<string> imageCommands;
    List<string> soundCommands;

    public CommandMessages()
    {
        InitializeMessageCommands();
        InitializeImageCommands();
        InitializeSoundCommands();
    }

    void InitializeMessageCommands()
    {
        messageCommands = new List<string>();
        messageCommands.Add("メッセージ");
        messageCommands.Add("入力待ち・初期化");
        messageCommands.Add("入力待ち");
        messageCommands.Add("初期化");
        messageCommands.Add("速度変更");
        messageCommands.Add("選択肢追加");
        messageCommands.Add("選択待ち");
    }

    void InitializeImageCommands()
    {
        imageCommands = new List<string>();
        imageCommands.Add("キャラ名設定");
        imageCommands.Add("キャラ画像変更");
        imageCommands.Add("フキダシ追加");
        imageCommands.Add("キャラ移動");
        imageCommands.Add("背景画像変更");
    }

    void InitializeSoundCommands()
    {
        soundCommands = new List<string>();
        soundCommands.Add("BGM再生");
        soundCommands.Add("BGM停止");
        soundCommands.Add("BGM再開");
        soundCommands.Add("SE再生");
    }

    public string GetCommandMessage(string commandText)
    {
        string[] targetTexts = commandText.Split('\\');
        int commandNo;

        if (!(commandText.StartsWith("[")
            && targetTexts.Length == 3
            &&int.TryParse(targetTexts[1],out commandNo))) return commandText;
        
        string message = "";
        string keyMessage = targetTexts[2].TrimEnd(']');

        //特殊コマンドを処理
        switch (targetTexts[0][1])
        {
            case 'm'://message
                message = GetCommandMessage(messageCommands, commandNo);
                break;
            case 'i'://image
                message = GetCommandMessage(imageCommands, commandNo);
                break;
            case 's'://sound
                message = GetCommandMessage(soundCommands, commandNo);
                break;
            default:
                return commandText;
        }
        return string.Format("  [{0}]:{1}",message,keyMessage);
    }

    string GetCommandMessage(List<string> list,int index)
    {
        if (index < 0 || list.Count <= index) return "";

        return list[index];
    }
}
