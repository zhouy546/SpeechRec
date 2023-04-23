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

    private NlsClient nlsClient = new NlsClient();

    private LinkedList<DemoSpeechRecognizerStruct> srList = null;
    private static Dictionary<string, RunParams> globalRunParams = new Dictionary<string, RunParams>();

    private NlsToken tokenPtr;
    private UInt64 expireTime;


    WaveInEvent waveIn = new WaveInEvent();
    [SerializeField]
    float volume;
    [SerializeField]
    bool is_Start_Recording = false;
    [SerializeField]
    bool is_Say_words = false;
    [SerializeField]
    bool is_Send_Data = true;

    [SerializeField]
    private string akId= "LTAI5tDVQCdzY1o3wKvpJZEo";
    [SerializeField]
    private string akSecret= "6Z2GTIRfo61tCmbPJYixALZggVd12H";
    [SerializeField]
    private string token;

    public InputField inputField;


    private bool running;  /* 刷新Label的flag */


    public string debugString;

    //[SerializeField]
    //private Text tAkId;

    //[SerializeField]
    //private Text tAkSecret;

    //[SerializeField]
    //private Text tToken;

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
    public void Awake()
    {
        insance = this;

        EventCenter.AddListener(EventDefine.ini, ini);
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LOOPRec());
    }

    private void ini()
    {
        akId = ValueSheet.jsonBridge.AkId;

        akSecret = ValueSheet.jsonBridge.AkSecret;

        

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
    }

    IEnumerator LOOPRec()
    {
        if (!is_Send_Data)
        {
            is_Send_Data = true;
            StopRecognizer();
            yield return new WaitForSeconds(0.5f);

            ReleaseRecognizer();
            yield return new WaitForSeconds(0.1f);

            CreateRecognizer();
            yield return new WaitForSeconds(0.1f);

            StartRecognizer();
        }
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(LOOPRec());
    }

    public async Task stopRecAndReset()
    {
        StopAllCoroutines();

        is_Send_Data = true;
        //resetLoop();
        StopRecognizer();
        await Task.Delay(500);
        ReleaseRecognizer();
        await Task.Delay(200);

        Releasetoken();
        await Task.Delay(200);

        DeinitNls();
        await Task.Delay(100);

        StartCoroutine(LOOPRec());
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
        /* 设置套接口地址类型, 需要在StartWorkThread前调用, 默认可不调用此接口 */
        //nlsClient.SetAddrInFamily("AF_INET4");

        /*
         * 启动1个事件池。在多并发(上百并发)情况下，建议选择 4 。若单并发，则建议填写 1 。
         */
        nlsClient.StartWorkThread(1);
        debugString = "StartWorkThread and init NLS success.";
        Debug.Log(debugString);

        running = true;
        /*
         * 启动线程FlushLab，用于将一些text显示到UI上
         */
    }

    

    // release sdk
    public void DeinitNls()
    {
        nlsClient.ReleaseInstance();
        debugString = "Release NLS success.";
        Debug.Log(debugString);
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

    public byte[] AudioToByte(AudioClip clip)
    {
        float[] floatData = new float[clip.samples * clip.channels];
        clip.GetData(floatData, 0);
        byte[] outData = new byte[floatData.Length];

        return outData;
    }


    DemoSpeechRecognizerStruct sr_node;
    /// <summary>
    /// 一句话识别的音频推送线程.
    /// </summary>
    private void SRAudioLab(object request)
    {
        sr_node = (DemoSpeechRecognizerStruct)request;

        startRecord(sr_node);

    }

    [SerializeField]
    float currentTime = 0;
    [SerializeField]
    float timeHold = 2;
    private void startRecord(DemoSpeechRecognizerStruct sr_node)
    {
        waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

        waveIn.DataAvailable += OnDataAvailable;
        waveIn.DataAvailable += sendData;
        is_Start_Recording = true;
        is_Send_Data = true;
        waveIn.StartRecording();
    }

    private void sendData(object sender, WaveInEventArgs e)
    {
        byte[] buffer = e.Buffer;
        // Copy the captured audio data to the buffer.
        int bytesToCopy = Math.Min(buffer.Length, e.BytesRecorded);
        Buffer.BlockCopy(e.Buffer, 0, buffer, 0, bytesToCopy);

        if (is_Send_Data&&volume>0.15f)
        {
            sr_node.srPtr.SendAudio(sr_node.srPtr, buffer, (UInt64)buffer.Length, EncoderType.ENCODER_PCM);
        }
    }


    private void OnDataAvailable(object sender, WaveInEventArgs args)
    {
        // Calculate the volume of the audio data
        float max = 0;
        byte[] buffer = args.Buffer;

        int bytesToCopy = Math.Min(buffer.Length, args.BytesRecorded);
        Buffer.BlockCopy(args.Buffer, 0, buffer, 0, bytesToCopy);


        for (int i = 0; i < args.BytesRecorded; i += 2)
        {
            short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
            float sample32 = sample / 32768f;
            max = Math.Max(max, Math.Abs(sample32));
        }

        volume = max;

        if (volume > 0.2)
        {
            currentTime = 0;
            is_Say_words = true;
        }
        else
        {
            if (is_Say_words)
            {

                currentTime += 0.16f;
                if (currentTime >= timeHold)
                {
                    resetLoop();
                }
            }
        }
    }

    private /*async*/ void resetLoop()
    {
        is_Send_Data = false;
        is_Say_words = false;

        currentTime = 0;
    }


    public void WaveInDispose()
    {
        if (is_Start_Recording)
        {
            Debug.Log("WaveInDispose run");
            waveIn.StopRecording();
            waveIn.Dispose();
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.DataAvailable -= sendData;
            is_Start_Recording = false;
        }



    }

    #region RecognizerButton
    // create recognizer
    public void CreateRecognizer()
    {
        Debug.Log("建立识别");

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
        Debug.Log("开始识别");

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
                    if (ValueSheet.appKey == null || ValueSheet.appKey.Length == 0)
                    {
                        ValueSheet.appKey = "KdhnnlXtjtz8eBYm";
                    }
                    if (token == null || token.Length == 0)
                    {
                       // token = tToken.text;
                    }
                    if (ValueSheet.appKey == null || token == null ||
                        ValueSheet.appKey.Length == 0 || token.Length == 0)
                    {
                        Debug.Log("Start failed, token or appkey is empty");
                        return;
                    }

                    sr.srPtr.SetAppKey(sr.srPtr, ValueSheet.appKey);
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

                            sr.sr_send_audio = new Thread(new ParameterizedThreadStart(SRAudioLab));
                            sr.sr_send_audio.Start((object)sr);
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
        Debug.Log("停止识别");
        WaveInDispose();
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
        Debug.Log("释放识别");

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

                /*
                 * 更新状态机send_audio_flag为true，表示请求成功，可以开始传送音频
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

                /*
                 * 这里可更新状态机为false，表示请求完成，可以停止传递音频和推出传递音频的线程
                 * 此处demo为循环运行，没有做停止此次请求的处理
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
                 * 更新状态机为false，表示请求完成，可以停止传递音频和推出传递音频的线程
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
