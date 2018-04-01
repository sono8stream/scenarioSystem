using System;
using System.Collections.Generic;

[Serializable]
public class UserData
{
    public Dictionary<string, int> variableDict;//テキストデータから読めるとイイかも

    public static UserData instance = new UserData();

    private UserData()
    {
        //自動ロード処理
        UserData userData = SaveManager.Load();
        if (userData != null)
        {
            instance = userData;
            return;
        }

        variableDict = SaveManager.LoadVariableDict();
        if (variableDict == null)
        {
            variableDict = new Dictionary<string, int>();
            variableDict.Add("test", 0);
            variableDict.Add("test2", 100);
        }
    }
}
