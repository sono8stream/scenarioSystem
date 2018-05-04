using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class ScriptUndo
{
    public bool canUndo, canRedo;

    const int maxRecordLength = 5;

    int nowRecordIndex;
    List<Action> operationRecord;
    List<string> scriptLines;

    public ScriptUndo(List<string> script)
    {
        canUndo = false;
        canRedo = false;

        nowRecordIndex = 0;
        operationRecord = new List<Action>();
        scriptLines = script;
    }

    public void AddInsertOperation(int index)
    {
        AddOperation(() => InsertOperation(index));
    }

    public void AddRemoveOperation(int index, string removedText)
    {
        AddOperation(() => RemoveOperation(index, removedText));
    }

    void InsertOperation(int index)
    {
        string text = scriptLines[index];
        scriptLines.RemoveAt(index);
        operationRecord[nowRecordIndex] = () => RemoveOperation(index, text);
    }

    void RemoveOperation(int index, string removedText)
    {
        scriptLines.Insert(index, removedText);
        Debug.Log(index);
        operationRecord[nowRecordIndex] = () => InsertOperation(index);
    }

    public void Undo()
    {
        if (!canUndo) return;

        operationRecord[nowRecordIndex].Invoke();
        if (nowRecordIndex == 0)
        {
            canUndo = false;
        }
        nowRecordIndex--;

        canRedo = true;
    }

    public void Redo()
    {
        if (!canRedo) return;

        nowRecordIndex++;
        Debug.Log(nowRecordIndex);
        operationRecord[nowRecordIndex].Invoke();
        if (nowRecordIndex == operationRecord.Count - 1)
        {
            canRedo = false;
        }
        canUndo = true;
    }

    /// <summary>
    /// redoを無効化して操作を追加
    /// 操作数が上限に至った場合は最も古い操作を消去
    /// </summary>
    /// <param name="action">追加するアクション</param>
    void AddOperation(Action action)
    {
        operationRecord = operationRecord.Take(nowRecordIndex + 1).ToList();
        operationRecord.Add(action);
        while(operationRecord.Count > maxRecordLength)
        {
            operationRecord.RemoveAt(0);
        }
        nowRecordIndex = operationRecord.Count - 1;
        canUndo = true;
        canRedo = false;
    }
}