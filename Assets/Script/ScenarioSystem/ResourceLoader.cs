using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceLoader
{
    [SerializeField]
    Sprite defaultSprite;

    const string defaultSpriteName = "none";
    const string defaultBgmName = "none";

    Dictionary<string, Sprite> charaSpriteDict;//読み込んだスプライトの実参照
    Dictionary<string, Sprite> sceneSpriteDict;//読み込んだスプライトの実参照
    Dictionary<string, AudioClip> bgmDict;//読み込んだスプライトの実参照
    Dictionary<string, AudioClip> seDict;//読み込んだスプライトの実参照

    bool isLoading,loadFailed;
    ScenarioProcessor scenarioProcessor;

    public void Initialize(ScenarioProcessor processor)
    {
        scenarioProcessor = processor;
        LoadDefault();
    }

    void LoadDefault()
    {
        charaSpriteDict = new Dictionary<string, Sprite>();
        charaSpriteDict.Add(defaultSpriteName, defaultSprite);

        sceneSpriteDict = new Dictionary<string, Sprite>();
        sceneSpriteDict.Add(defaultSpriteName, defaultSprite);

        bgmDict = new Dictionary<string, AudioClip>();
        bgmDict.Add(defaultBgmName, null);

        seDict = new Dictionary<string, AudioClip>();
        seDict.Add(defaultBgmName, null);
    }

    /// <summary>
    /// codeに一致するリソースを読み込む
    /// ロード済みならそのまま返し、ロードしていないときロード、存在していないときnullを返す
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public Sprite GetCharaSprite(string name)
    {
        return GetResource(name, defaultSpriteName, charaSpriteDict, "Portrait");
    }

    public Sprite GetSceneSprite(string name)
    {
        return GetResource(name, defaultSpriteName, sceneSpriteDict, "Scenery");
    }

    public AudioClip GetBGM(string name)
    {
        return GetResource(name,defaultBgmName, bgmDict, "BGM");
    }

    public AudioClip GetSE(string name)
    {
        return GetResource(name, defaultBgmName, seDict, "SE");
    }

    Type GetResource<Type>(string name, string defaultName,
        Dictionary<string, Type> resourceDict, string folderName)
        where Type : class
    {
        if (resourceDict.ContainsKey(name))
        {
            Debug.Log("Loaded Success");
            return resourceDict[name];
        }
        else if (!isLoading)//うまいアルゴリズムが書けない
        {
            if (loadFailed)//ロード失敗
            {
                Debug.Log("Loaded Default");
                loadFailed = false;
                return resourceDict[defaultName];
            }
            else//ロード
            {
                string path = folderName + "/" + name;
                ResourceRequest request = Resources.LoadAsync(path, typeof(Type));
                scenarioProcessor.StartCoroutine(CheckLoadDone(
                    name, request, resourceDict));
                Debug.Log(path);
                return default(Type);
            }
        }
        return default(Type);
    }

    IEnumerator CheckLoadDone<Type>(string name, ResourceRequest request,
        Dictionary<string, Type> resourceDict)
        where Type : class
    {
        isLoading = true;
        while (!request.isDone) yield return null;

        Type resource = request.asset as Type;
        if (resource == null)
        {
            loadFailed = true;
        }
        else
        {
            Debug.Log("load succeed");
            resourceDict.Add(name, resource);
        }
        Debug.Log("load done");
        isLoading = false;
    }
}