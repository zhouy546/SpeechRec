using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nlsCsharpSdk;
using UnityEngine.UI;
using System.Threading;
using System;
using System.IO;
using NAudio.Wave;
using System.Threading.Tasks;

public struct DemoSpeechRecognizerStruct
{
    public SpeechRecognizerRequest srPtr;
    public Thread sr_send_audio;
    public string uuid;
};

public struct RunParams
{
    public bool send_audio_flag;
    public bool audio_loop_flag;
};


public class SpeechRec : MonoBehaviour
{

    public static SpeechRec insance;

    public Text volumeThreholdText;
    public Text CurrentVolumeText;

    private NlsClient nlsClient = new NlsClient();

    private LinkedList<DemoSpeechRecognizerStruct> srList = null;
    private static Dictionary<string, RunParams> globalRunParams = new Dictionary<string, RunParams>();

    private NlsToken tokenPtr;
    private UInt64 expireTime;


    WaveInEvent waveIn = new WaveInEvent();
    [SerializeField]
    float volume;
    [SerializeField]
    static bool send_audio_flag = true;
    static bool isini = false;

    //[SerializeField]
    //bool is_Say_words = false;
    //[SerializeField]
    //bool is_Send_Data = true;

    [SerializeField]
    private string akId= "LTAI5tDVQCdzY1o3wKvpJZEo";
    [SerializeField]
    private string akSecret= "6Z2GTIRfo61tCmbPJYixALZggVd12H";
    [SerializeField]
    private string token;

    public InputField inputField;


    private bool running;  /* ˢ��Label��flag */


    public string debugString;

    static string cur_st_result;
    static string cur_st_completed;
    static string cur_st_closed;
    static int st_concurrency_number = 1;

    static string cur_sr_result;
    static string cur_sr_completed;
    static string cur_sr_closed;
    static int sr_concurrency_number = 1;

    static string cur_sy_completed;
    static string cur_sy_closed;
    static int sy_concurrency_number = 1;

    static string resultToSend="";
    static string ResultToSend = "";

    public Thread SendThread;
    public void Awake()
    {
        insance = this;

        EventCenter.AddListener(EventDefine.û��˵����ʱ, SayWordsTimeOut);

        EventCenter.AddListener(EventDefine.ini, ini);
    }

    // Start is called before the first frame update
    void Start()
    {
       // StartCoroutine(LOOPRec());
    }

    private void ini()
    {
        akId = ValueSheet.jsonBridge.AkId;

        akSecret = ValueSheet.jsonBridge.AkSecret;

        volumeThreholdText.text ="��ǰ������ֵ��"+ ValueSheet.jsonBridge.volumeThrehold.ToString(); ;

    }

    // Update is called once per frame
    void Update()
    {
        if (ResultToSend!=resultToSend)
        {
            ResultToSend = resultToSend;
            inputField.text = ResultToSend;
            SendUPDData.instance.udp_Send(inputField.text, ValueSheet.jsonBridge.TargetIP, ValueSheet.jsonBridge.TargetPort);

        }


        if (!send_audio_flag&&running)
        {
            send_audio_flag = true;

            EventCenter.Broadcast(EventDefine.û��˵����ʱ);
        }

        CurrentVolumeText.text = "��ǰ������С��" + volume.ToString();

    }



    private void OnApplicationQuit()
    {



        StopAllCoroutines();


        WaveInDispose();

        ReleaseRecognizer();

        Releasetoken();

        DeinitNls();
    }

    public void InitNls()
    {
        /* �����׽ӿڵ�ַ����, ��Ҫ��StartWorkThreadǰ����, Ĭ�Ͽɲ����ô˽ӿ� */
        //nlsClient.SetAddrInFamily("AF_INET4");

        /*
         * ����1���¼��ء��ڶಢ��(�ϰٲ���)����£�����ѡ�� 4 ������������������д 1 ��
         */
        nlsClient.StartWorkThread(1);
        debugString = "StartWorkThread and init NLS success.";
        Debug.Log(debugString);
        StartRecord();

        running = true;
        /*
         * �����߳�FlushLab�����ڽ�һЩtext��ʾ��UI��
         */
    }

    

    // release sdk
    public void DeinitNls()
    {
        running = false;
        nlsClient.ReleaseInstance();
        debugString = "Release NLS success.";
        Debug.Log(debugString);

        WaveInDispose();

    }


    #region TokenButton
    // create token
    public void Createtoken()
    {
        int ret = -1;
        tokenPtr = nlsClient.CreateNlsToken();
        if (tokenPtr.native_token != IntPtr.Zero)
        {
            if (akId == null || akId.Length == 0)
            {
                //akId = tAkId.text;
            }
            if (akSecret == null || akSecret.Length == 0)
            {
                //akSecret = tAkSecret.text;
            }

            if (akId != null && akSecret != null && akId.Length > 0 && akSecret.Length > 0)
            {
                tokenPtr.SetAccessKeyId(tokenPtr, akId);
                tokenPtr.SetKeySecret(tokenPtr, akSecret);

                ret = tokenPtr.ApplyNlsToken(tokenPtr);
                if (ret < 0)
                {
                    Debug.Log("ApplyNlsToken failed");
                    debugString = tokenPtr.GetErrorMsg(tokenPtr);
                    Debug.Log(debugString);
                }
                else
                {
                    Debug.Log("ApplyNlsToken success");
                    token = tokenPtr.GetToken(tokenPtr);
                    Debug.Log(token);
                    expireTime = tokenPtr.GetExpireTime(tokenPtr);
                    debugString = "ExpireTime:" + expireTime.ToString();
                    Debug.Log(debugString);

                }
            }
            else
            {
                debugString = "CreateToken Failed, akId or Secret is null";
                Debug.Log(debugString);

            }
        }
        else
        {
            debugString = "CreateToken Failed";
            Debug.Log(debugString);

        }
    }

    // release token
    public void Releasetoken()
    {
        if (tokenPtr.native_token != IntPtr.Zero)
        {
            nlsClient.ReleaseNlsToken(tokenPtr);
            tokenPtr.native_token = IntPtr.Zero;
            debugString = "ReleaseNlsToken Success";
            Debug.Log(debugString);

        }
        else
        {
            debugString = "ReleaseNlsToken is nullptr";
            Debug.Log(debugString);

        }
    }
    #endregion


    private void StartRecord()
    {
        Debug.Log("��ʼ��¼��˷�");

        waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

        waveIn.DataAvailable += OnDataAvailable;

        waveIn.StartRecording();
    }


    DemoSpeechRecognizerStruct sr_node;
    [SerializeField]
    float currentTime = 0;
    /// <summary>
    /// һ�仰ʶ�����Ƶ�����߳�.
    /// </summary>
    private void SRAudioLab(object request)
    {
        sr_node = (DemoSpeechRecognizerStruct)request;

        send_audio_flag = true;

        //EventCenter.Broadcast(EventDefine.��ʼ��������);
        Debug.Log("SRAudioLab Loop");
        while (true)
        {
            if (ValueSheet.AudioStream.Count > 0)
            {
                Debug.Log(ValueSheet.AudioStream.Count);

                Debug.Log("��������");
                 Thread.Sleep(20);

                try
                {
                    byte[] buffer = ValueSheet.AudioStream.Dequeue();

                    sr_node.srPtr.SendAudio(sr_node.srPtr, buffer, (UInt64)buffer.Length, EncoderType.ENCODER_PCM);
                }
                catch (Exception e)
                {

                    Debug.LogWarning(e.ToString());
                }

            }
        }
    }



    private void OnDataAvailable(object sender, WaveInEventArgs args)
    {
        // Calculate the volume of the audio data
        float max = 0;
        byte[] buffer = args.Buffer; //args.Buffer;


        int bytesToCopy = Math.Min(buffer.Length, args.BytesRecorded);
        Buffer.BlockCopy(args.Buffer, 0, buffer, 0, bytesToCopy);



        for (int i = 0; i < args.BytesRecorded; i += 2)
        {
            short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
            float sample32 = sample / 32768f;
            max = Math.Max(max, Math.Abs(sample32));
        }

        volume = max;


        if (ValueSheet.FirstSayWords)
        {

            if (volume >(float)ValueSheet.jsonBridge.volumeThrehold)
            {
                currentTime = 0;
                ValueSheet.FirstSayWords = false;
                ValueSheet.AudioStream.Enqueue(buffer);

            }

        }
        else
        {
            if (volume > (float)ValueSheet.jsonBridge.volumeThrehold)
            {
                currentTime = 0;
                ValueSheet.AudioStream.Enqueue(buffer);

            }
            else
            {
                currentTime += 0.16f;
                if (currentTime >= ValueSheet.jsonBridge.TimeThrehold)
                {
                    StopRecognizer();
                }
            }
        }
    }

    private async void SayWordsTimeOut()
    {

        currentTime = 0;
        ValueSheet.FirstSayWords = true;

        await resetTalk();
    }

    private async Task resetTalk()
    {

        StopRecognizer();
        await Task.Delay(100);

        ReleaseRecognizer();

        await Task.Delay(100);

        CreateRecognizer();

        await Task.Delay(100);

        StartRecognizer();
    }

    public async Task stopRec()
    {
        StopRecognizer();
        await Task.Delay(100);

        ReleaseRecognizer();

        await Task.Delay(100);

        Releasetoken();

        await Task.Delay(100);

        DeinitNls();
    }


    public void WaveInDispose()
    {

        Debug.Log("ֹͣ��¼��˷�");
        Debug.Log("WaveInDispose run");
        waveIn.StopRecording();
        waveIn.Dispose();
        waveIn.DataAvailable -= OnDataAvailable;

    }

    #region RecognizerButton
    // create recognizer
    public void CreateRecognizer()
    {



        Debug.Log("����ʶ��");

        if (srList == null)
        {
            srList = new LinkedList<DemoSpeechRecognizerStruct>();
        }
        else
        {
           Debug.Log("recognizer list is existed, release first...");
        }

        for (int i = 0; i < sr_concurrency_number; i++)
        {
            DemoSpeechRecognizerStruct srStruct;
            srStruct = new DemoSpeechRecognizerStruct();
            srStruct.srPtr = nlsClient.CreateRecognizerRequest();
            if (srStruct.srPtr.native_request != IntPtr.Zero)
            {
                Debug.Log("CreateRecognizerRequest Success");

                srStruct.uuid = System.Guid.NewGuid().ToString("N");
                RunParams demo_params = new RunParams();
                demo_params.send_audio_flag = false;
                demo_params.audio_loop_flag = false;
                globalRunParams[srStruct.uuid] = demo_params;
            }
            else
            {
                Debug.Log("CreateRecognizerRequest Failed");
            }
            srList.AddLast(srStruct);
        }
        cur_sr_result = "null";
        cur_sr_closed = "null";
        cur_sr_completed = "null";
    }

    // start recognizer
    public void StartRecognizer()
    {
        Debug.Log("��ʼʶ��");

        int ret = -1;
        if (srList == null)
        {
            Debug.Log("recognizer list is null, create first...");
            return;
        }
        else
        {
            LinkedListNode<DemoSpeechRecognizerStruct> srStruct = srList.First;
            int sr_count = srList.Count;
            for (int i = 0; i < sr_count; i++)
            {
                DemoSpeechRecognizerStruct sr = srStruct.Value;
                if (sr.srPtr.native_request != IntPtr.Zero)
                {
                    if (ValueSheet.jsonBridge.AppKey == null || ValueSheet.jsonBridge.AppKey.Length == 0)
                    {
                        ValueSheet.jsonBridge.AppKey = "qrKYAlt8R80O4N5c";
                    }
                    if (token == null || token.Length == 0)
                    {
                       // token = tToken.text;
                    }
                    if (ValueSheet.jsonBridge.AppKey == null || token == null ||
                        ValueSheet.jsonBridge.AppKey.Length == 0 || token.Length == 0)
                    {
                        Debug.Log("Start failed, token or appkey is empty");
                        return;
                    }

                    sr.srPtr.SetAppKey(sr.srPtr, ValueSheet.jsonBridge.AppKey);
                    sr.srPtr.SetToken(sr.srPtr, token);
                    sr.srPtr.SetUrl(sr.srPtr, "wss://nls-gateway-cn-shanghai.aliyuncs.com/ws/v1");
                    sr.srPtr.SetFormat(sr.srPtr, "pcm");
                    sr.srPtr.SetSampleRate(sr.srPtr, 16000);
                    sr.srPtr.SetIntermediateResult(sr.srPtr, true);
                    sr.srPtr.SetPunctuationPrediction(sr.srPtr, true);
                    sr.srPtr.SetInverseTextNormalization(sr.srPtr, true);

                    sr.srPtr.SetOnRecognitionStarted(sr.srPtr, DemoOnRecognitionStarted, sr.uuid);
                    sr.srPtr.SetOnChannelClosed(sr.srPtr, DemoOnRecognitionClosed, sr.uuid);
                    sr.srPtr.SetOnTaskFailed(sr.srPtr, DemoOnRecognitionTaskFailed, sr.uuid);
                    sr.srPtr.SetOnRecognitionResultChanged(sr.srPtr, DemoOnRecognitionResultChanged, sr.uuid);
                    sr.srPtr.SetOnRecognitionCompleted(sr.srPtr, DemoOnRecognitionCompleted, sr.uuid);

                    ret = sr.srPtr.Start(sr.srPtr);
                    if (ret != 0)
                    {
                        Debug.Log("recognizer Start failed");
                    }
                    else
                    {
                        if (globalRunParams[sr.uuid].audio_loop_flag == false)
                        {
                            RunParams demo_params = new RunParams();
                            demo_params.audio_loop_flag = true;
                            demo_params.send_audio_flag = globalRunParams[sr.uuid].send_audio_flag;

                            globalRunParams.Remove(sr.uuid);
                            globalRunParams.Add(sr.uuid, demo_params);

                            SendThread =sr.sr_send_audio = new Thread(new ParameterizedThreadStart(SRAudioLab));
                            SendThread.Start((object)sr);
                        }

                        Debug.Log("Recognizer Start success");
                    }
                }
                else
                {
                }
                srStruct = srStruct.Next;
                if (srStruct == null)
                {
                    break;
                }
            }
        }
    }

    // stop recognizer
    public void StopRecognizer()
    {

        SendThread.Abort();
        Debug.Log("ֹͣʶ��");
        int ret = -1;
        if (srList == null)
        {
            Debug.Log("recognizer list is null, create first...");
            return;
        }
        else
        {
            LinkedListNode<DemoSpeechRecognizerStruct> srStruct = srList.First;
            int sr_count = srList.Count;
            for (int i = 0; i < sr_count; i++)
            {
                DemoSpeechRecognizerStruct sr = srStruct.Value;
                if (sr.srPtr.native_request != IntPtr.Zero)
                {
                    if (sr.srPtr.native_request != IntPtr.Zero)
                    {
                        RunParams demo_params = new RunParams();
                        demo_params.audio_loop_flag = globalRunParams[sr.uuid].audio_loop_flag;
                        demo_params.send_audio_flag = false;

                        globalRunParams.Remove(sr.uuid);
                        globalRunParams.Add(sr.uuid, demo_params);

                        ret = sr.srPtr.Stop(sr.srPtr);
                    }

                    if (ret != 0)
                    {
                        Debug.Log("Recognizer Stop failed");
                    }
                    else
                    {
                        Debug.Log("Recognizer Stop success");
                    }
                }
                else
                {
                }
                srStruct = srStruct.Next;
                if (srStruct == null)
                {
                    break;
                }
            }
        }
    }

    // release recognizer
    public void ReleaseRecognizer()
    {
        Debug.Log("�ͷ�ʶ��");

        if (srList == null)
        {
            Debug.Log("recognizer list is null, create first...");
            return;
        }
        else
        {
            int sr_count = srList.Count;
            for (int i = 0; i < sr_count; i++)
            {
                LinkedListNode<DemoSpeechRecognizerStruct> srStruct = srList.Last;
                DemoSpeechRecognizerStruct sr = srStruct.Value;
                if (sr.srPtr.native_request != IntPtr.Zero)
                {
                    nlsClient.ReleaseRecognizerRequest(sr.srPtr);
                    sr.srPtr.native_request = IntPtr.Zero;
                    globalRunParams.Remove(sr.uuid);

                    Debug.Log("ReleaseRecognizerRequest Success");
                }
                else
                {
                    Debug.Log("RecognizerRequest is nullptr");
                }
                srList.RemoveLast();
            }
            srList.Clear();
        }

        cur_sr_result = "null";
        cur_sr_closed = "null";
        cur_sr_completed = "null";
    }
    #endregion

    #region RecognizerCallback
    private CallbackDelegate DemoOnRecognitionStarted =
          (ref NLS_EVENT_STRUCT e, ref string uuid) =>
          {
              Debug.LogFormat("DemoOnRecognitionStarted user uuid = {0}", uuid);
              string msg = System.Text.Encoding.Default.GetString(e.msg);
              Debug.LogFormat("DemoOnRecognitionStarted msg = {0}", msg);

              cur_sr_completed = "msg : " + msg;
              send_audio_flag = true;

              /*
               * ����״̬��send_audio_flagΪtrue����ʾ����ɹ������Կ�ʼ������Ƶ
               */
              RunParams demo_params = new RunParams();
              demo_params.send_audio_flag = true;
              demo_params.audio_loop_flag = globalRunParams[uuid].audio_loop_flag;

              globalRunParams.Remove(uuid);
              globalRunParams.Add(uuid, demo_params);
          };
    private CallbackDelegate DemoOnRecognitionClosed =
        (ref NLS_EVENT_STRUCT e, ref string uuid) =>
        {
            string msg = System.Text.Encoding.Default.GetString(e.msg);
            Debug.LogFormat("DemoOnRecognitionClosed = {0}", msg);
            cur_sr_closed = "msg : " + msg;
            send_audio_flag = false;

            /*
             * ����ɸ���״̬��Ϊfalse����ʾ������ɣ�����ֹͣ������Ƶ���Ƴ�������Ƶ���߳�
             * �˴�demoΪѭ�����У�û����ֹͣ�˴�����Ĵ���
             */

        };
    private CallbackDelegate DemoOnRecognitionTaskFailed =
        (ref NLS_EVENT_STRUCT e, ref string uuid) =>
        {
            Debug.LogFormat("DemoOnRecognitionTaskFailed user uuid = {0}", uuid);
            string msg = System.Text.Encoding.Default.GetString(e.msg);
            Debug.LogFormat("DemoOnRecognitionTaskFailed = {0}", msg);
            cur_sr_completed = "msg : " + msg;

                /*
                 * ����״̬��Ϊfalse����ʾ������ɣ�����ֹͣ������Ƶ���Ƴ�������Ƶ���߳�
                 */
            RunParams demo_params = new RunParams();
            demo_params.send_audio_flag = false;
            demo_params.audio_loop_flag = false;

            globalRunParams.Remove(uuid);
            globalRunParams.Add(uuid, demo_params);
        };
    private CallbackDelegate DemoOnRecognitionResultChanged =
        (ref NLS_EVENT_STRUCT e, ref string uuid) =>
        {
            //Debug.LogFormat("DemoOnRecognitionResultChanged user uuid = {0}", uuid);
            string result = System.Text.Encoding.GetEncoding("gb2312").GetString(e.result);
           // resultToSend = result;
            Debug.LogFormat("DemoOnRecognitionResultChanged result = {0}", result);
            cur_sr_result = "middle result : " + result;

           
        };
    private CallbackDelegate DemoOnRecognitionCompleted =
        (ref NLS_EVENT_STRUCT e, ref string uuid) =>
        {
            //Debug.LogFormat("DemoOnRecognitionCompleted user uuid = {0}", uuid);
            string result = System.Text.Encoding.GetEncoding("gb2312").GetString(e.result);
            Debug.LogFormat("DemoOnRecognitionCompleted result = {0}", result);
            resultToSend = result;
            cur_sr_completed = "final result : " + result;
        };
#endregion
}
