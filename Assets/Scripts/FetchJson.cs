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
            Debug.Log("δ�ҵ������ļ�����������Ĭ��");
        }
        else
        {
            Debug.Log("�ҵ������ļ�����Json����");


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
