using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class RecCtr : MonoBehaviour
{
    public static RecCtr instance;

    // Start is called before the first frame update
    async void Start()
    {
        instance = this;
       await IniRec();
    }

    // Update is called once per frame
    async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            await startRec();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            await SpeechRec.insance.stopRecAndReset();
        }
    }


    public async void STARTREC() {

        await startRec();
    }


    public async void STOPREC()
    {
        await SpeechRec.insance.stopRecAndReset();

    }

    async Task IniRec()
    {
        SpeechRec.insance.InitNls();
        await Task.Delay(500);
        SpeechRec.insance.Createtoken();
    }

    async Task startRec()
    {
        SpeechRec.insance.CreateRecognizer();
        await Task.Delay(200);
        SpeechRec.insance.StartRecognizer();
    }

}
