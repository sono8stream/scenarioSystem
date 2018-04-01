using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SampleScriptableObject : ScriptableObject
{
    [SerializeField]
    int sampleIntValue;
    [SerializeField]
    string text;

    public int SampleIntValue
    {
        get { return sampleIntValue; }
#if UNITY_EDITOR
        set { sampleIntValue = Mathf.Clamp(value, 0, int.MaxValue); }
#endif
    }
}
