using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using speechRec;
public class RecCtr : MonoBehaviour
{
    public static RecCtr instance;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        //await IniRec();

        EventCenter.AddListener(EventDefine.ini, STARTREC);
    }

    // Update is called once per frame
    async void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            await startRec();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            await SpeechRec.insance.stopRec();
        }
    }

    


    public async void STARTREC() {

        await startRec();
    }


    public async void STOPREC()
    {
        await SpeechRec.insance.stopRec();

    }

    async Task IniRec()
    {
        SpeechRec.insance.InitNls();
        await Task.Delay(500);
        SpeechRec.insance.Createtoken();
    }

    async Task startRec()
    {
        SpeechRec.insance.InitNls();
        await Task.Delay(500);
        SpeechRec.insance.Createtoken();
        await Task.Delay(200);

        SpeechRec.insance.CreateRecognizer();
        await Task.Delay(200);
        SpeechRec.insance.StartRecognizer();
    }

}
