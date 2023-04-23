using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
public class FetchJson : MonoBehaviour
{
    private IEnumerator GetJsonData()
    {
        string spath = Application.streamingAssetsPath + "/information.json";
        FileInfo info = new FileInfo(spath);
        if (!info.Exists)
        {
            Debug.Log("未找到配置文件，重新生成默认");
        }
        else
        {
            Debug.Log("找到配置文件，从Json载入");


            WWW www = new WWW(spath);

            yield return www;

            string jsonString = System.Text.Encoding.UTF8.GetString(www.bytes);

            Debug.Log(jsonString);

            ValueSheet.jsonBridge = JsonMapper.ToObject<Root>(www.text);

            EventCenter.Broadcast(EventDefine.ini);
        }
    }




    private void Start()
    {
        StartCoroutine(GetJsonData());
    }
}
