//*********************❤*********************
// 
// 文件名（File Name）：	DealWithUDPMessage.cs
// 
// 作者（Author）：			LoveNeon
// 
// 创建时间（CreateTime）：	Don't Care
// 
// 说明（Description）：	接受到消息之后会传给我，然后我进行处理
// 
//*********************❤*********************

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

public class DealWithUDPMessage : MonoBehaviour {
    public static DealWithUDPMessage instance;


    public string dataTest;


    //private bool enterTrigger, exitTrigger;
    /// <summary>
    /// 消息处理
    /// </summary>
    /// <param name="_data"></param>
    public void MessageManage(string _data)
    {
        if (_data != "")
        {
            dataTest = _data;

            Debug.Log(dataTest);

            ValueSheet.TextsToSynthesizer.Enqueue(dataTest);

        }
       
    }






   

    public void Start()
    {
        if (instance == null) {
            instance = this;
        }
    }


    private void Update()
    {


        //Debug.Log("数据：" + dataTest);  
    }

}
