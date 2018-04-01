using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioProcessor : MonoBehaviour
{
    [SerializeField]
    TextAsset testText;
    [SerializeField]
    ResourceLoader resourceLoader;
    [SerializeField]
    MessageProcessor messenger;
    [SerializeField]
    ImageProcessor imager;
    //[SerializeField]
    //Logger logger;
    SoundProcessor sounder;

    TextLoader textLoader;
    List<CommandProcessor> processorList;
    int processIndex;

    // Use this for initialization
    void Start()
    {
        Debug.Log(testText.text);
        textLoader = new TextLoader(testText.text);
        resourceLoader.Initialize(this);

        messenger.Initialize(textLoader);
        imager.Initialize(resourceLoader);
        sounder = new SoundProcessor();
        sounder.Initialize(resourceLoader);

        processorList = new List<CommandProcessor>();
        processorList.Add(messenger);
        processorList.Add(imager);
        processorList.Add(sounder);
        processIndex = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (processIndex == -1 && !SelectProcessor()) return;//もう読み込めない

        if (processorList[processIndex].Process())
        {
            processIndex = -1;
        }
    }

    bool SelectProcessor()
    {
        string targetText = textLoader.ReadLine();
        if (targetText == null) return false;

        int index = 0;
        if (targetText.StartsWith("[")
            && targetText.Split('\\').Length == 3)//特殊コマンドを処理
        {
            index = processorList.FindIndex(x => x.Trigger == targetText[1]);
        }
        if (index == -1) return false;

        processIndex = index;
        processorList[processIndex].ProcessBegin(targetText);
        Debug.Log(processIndex);
        return true;
    }
}