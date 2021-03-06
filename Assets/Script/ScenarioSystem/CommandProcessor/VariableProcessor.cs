﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VariableProcessor : CommandProcessor
{
    int[] tempVariables;
    TextLoader textLoader;

    public void Initialize(TextLoader loader)
    {
        trigger = 'v';
        tempVariables = new int[10];
        commandList = new List<System.Func<bool>>();
        commandList.Add(ChangeVariable);
        commandList.Add(BranchByVariable);
        textLoader = loader;
    }

    bool ChangeVariable()//代入先変数名:演算子:値
    {
        string[] s = keyText.Split(':');
        if (s.Length != 3) return true;

        int baseValue = GetVariableValue(s[0]);
        int changeValue = GetVariableValue(s[2]);
        switch ((s[1][0]))
        {
            case '+':
                SetVariableValue(s[0], baseValue + changeValue);
                break;
            case '-':
                SetVariableValue(s[0], baseValue - changeValue);
                break;
            case '*':
                SetVariableValue(s[0], baseValue * changeValue);
                break;
            case '/':
                SetVariableValue(s[0], baseValue / changeValue);
                break;
            case '=':
                SetVariableValue(s[0], changeValue);
                break;
        }
        return true;
    }

    bool BranchByVariable()
    {
        string[] s = keyText.Split(':');
        if (s.Length != 3) return true;

        int baseValue = GetVariableValue(s[0]);
        int comparedValue = GetVariableValue(s[2]);
        bool on = false;
        switch ((s[1][0]))
        {
            case '<':
                if (s[1].Length == 2 && s[1][1] == '=')
                {
                    on = baseValue <= comparedValue;
                }
                else
                {
                    on= baseValue < comparedValue;
                }
                break;
            case '>':
                if (s[1].Length == 2 && s[1][1] == '=')
                {
                    on = baseValue >= comparedValue;
                }
                else
                {
                    on = baseValue > comparedValue;
                }
                break;
            case '=':
                on = baseValue == comparedValue;
                break;
        }

        string label = string.Format("{0}{1}{2}", s[0], s[1], s[2]);
        if (!on) label = label.Insert(0, "Not ");
        textLoader.JumpLabel(label);
        return true;
    }    

    public int GetVariableValue(string valueText)
    {
        int value;
        if (int.TryParse(valueText, out value)) return value;//ただの数値

        string exactVarName = valueText.Substring(1, valueText.Length - 2);
        switch (valueText[0])//変数チェック
        {
            case '_'://通常変数
                if (!UserData.instance.variableDict.ContainsKey(exactVarName)) break;
                return UserData.instance.variableDict[exactVarName];
            case '@'://一次変数
                int index;
                if (!(int.TryParse(exactVarName, out index)
                    && (0 <= index && index < tempVariables.Length))) break;
                return tempVariables[index];
        }
        return 0;
    }

    void SetVariableValue(string varName, int value)
    {
        string exactVarName = varName.Substring(1, varName.Length - 2);
        switch (varName[0])
        {
            case '_'://通常変数
                if (!UserData.instance.variableDict.ContainsKey(exactVarName)) return;
                UserData.instance.variableDict[exactVarName] = value;
                return;
            case '@'://一次変数
                int index;
                if (!(int.TryParse(exactVarName, out index)
                    && (0 <= index && index < tempVariables.Length))) break;
                tempVariables[index] = value;
                return;
        }
    }
}