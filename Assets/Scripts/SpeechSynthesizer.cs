using nlsCsharpSdk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace speechSynthesizer {

    public struct RunParams
    {
        public bool send_audio_flag;
        public bool audio_loop_flag;
    };

    public struct DemoSpeechSynthesizerStruct
    {
        public SpeechSynthesizerRequest syPtr;
        public Thread sy_send_audio;
        public string uuid;
    };

    public class SpeechSynthesizer : MonoBehaviour
    {
        public static SpeechSynthesizer insance;

        public InputField inputField;

        private NlsClient nlsClient = new NlsClient();
        private static Dictionary<string, RunParams> globalRunParams = new Dictionary<string, RunParams>();
        private LinkedList<DemoSpeechSynthesizerStruct> syList = null;

        private NlsToken tokenPtr;
        private UInt64 expireTime;

        public string appKey;
        public string akId;
        public string akSecret;
        public string token;
        private string url;

        static int max_concurrency_num = 200;  /* �����õ���󲢷��� */
       // static bool running;  /* ˢ��Label��flag */
        //static string cur_nls_result;

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

        static string fileLinkUrl = "https://gw.alipayobjects.com/os/bmw-prod/0574ee2e-f494-45a5-820f-63aee583045a.wav";

        private string debugString;

        private static string filename;

        private static bool udpSendLocker =true;

        public void Awake()
        {
            insance = this;

            EventCenter.AddListener(EventDefine.ini, ini);

        }

        public async void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
               await StartSynthesizer();
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                await StopSynthesizer();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                await StartNextSynthesizer();
            }


            if (ValueSheet.TextsToSynthesizer.Count > 0 && !ValueSheet.IsSynthesizerRunning)
            {
                f_StartSynthesizer();
            }


            if (!udpSendLocker)
            {
                udpSendLocker = true;

                SendUPDData.instance.udp_Send(filename, ValueSheet.jsonBridge.WavTargetIP, ValueSheet.jsonBridge.WavTargetPort);

            }

        }

        private void ini()
        {
            appKey = ValueSheet.jsonBridge.AppKey;

            akId = ValueSheet.jsonBridge.AkId;

            akSecret = ValueSheet.jsonBridge.AkSecret;

        }

        public void OnApplicationQuit()
        {
            cancelSynthesizer();

            releaseSynthesizer();

            releaseToken();

            DeinitNls();
        }


        public async void f_StartSynthesizer()
        {

            ValueSheet.IsSynthesizerRunning = true;
            if (ValueSheet.isFirstSynthesizer)
            {
                ValueSheet.isFirstSynthesizer = false;

                await StartSynthesizer();
            }
            else
            {
                await StartNextSynthesizer();
            }
        }

        private async Task StartSynthesizer()
        {
            InitNls();

            await Task.Delay(500);

            createToken();

            await Task.Delay(500);

            createSynthesizer();

            await Task.Delay(500);

            startSynthesizer();
        }

        private async Task StartNextSynthesizer()
        {
            cancelSynthesizer();

            await Task.Delay(500);

            releaseSynthesizer();

            await Task.Delay(500);


            createSynthesizer();

            await Task.Delay(500);

            startSynthesizer();
        }

        private async Task StopSynthesizer()
        {
            cancelSynthesizer();

            await Task.Delay(500);

            releaseSynthesizer();

            await Task.Delay(500);

            releaseToken();

            await Task.Delay(500);

            DeinitNls();
        }

        #region NlsSdkButton
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
            /*
             * �����߳�FlushLab�����ڽ�һЩtext��ʾ��UI��
             */
        }


        // release sdk
        public void DeinitNls()
        {

            nlsClient.ReleaseInstance();
            debugString = "Release NLS success.";
            Debug.Log(debugString);

        }



        #endregion


        #region TokenButton
        // create token
        public void createToken()
        {
            int ret = -1;
            tokenPtr = nlsClient.CreateNlsToken();
            if (tokenPtr.native_token != IntPtr.Zero)
            {
                if (akId == null || akId.Length == 0)
                {
                    akId = ValueSheet.jsonBridge.AkId; //tAkId.Text;
                }
                if (akSecret == null || akSecret.Length == 0)
                {
                    akSecret = ValueSheet.jsonBridge.AkSecret; //tAkSecret.Text;
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
                    }
                    else
                    {
                        Debug.Log("ApplyNlsToken success");
                        token = tokenPtr.GetToken(tokenPtr);
                        Debug.Log(token);
                        expireTime = tokenPtr.GetExpireTime(tokenPtr);
                        debugString = "ExpireTime:" + expireTime.ToString();
                    }
                }
                else
                {
                    debugString = "CreateToken Failed, akId or Secret is null";
                }
            }
            else
            {
                debugString = "CreateToken Failed";
            }
        }

        // release token
        public void releaseToken()
        {
            if (tokenPtr.native_token != IntPtr.Zero)
            {
                nlsClient.ReleaseNlsToken(tokenPtr);
                tokenPtr.native_token = IntPtr.Zero;
                debugString = "ReleaseNlsToken Success";
            }
            else
            {
                debugString = "ReleaseNlsToken is nullptr";
            }
        }
        #endregion


        #region SynthesizerButton
        // create synthesizer
        public void createSynthesizer()
        {
            if (syList == null)
            {
                syList = new LinkedList<DemoSpeechSynthesizerStruct>();
            }
            else
            {
                Debug.Log("synthesizer list is existed, release first...");
            }

            for (int i = 0; i < sy_concurrency_number; i++)
            {
                /*
                 * ����һ���û�����Ľṹ�壬����SDK���������ڻص��л�ȡ
                 */
                DemoSpeechSynthesizerStruct syStruct;
                syStruct = new DemoSpeechSynthesizerStruct();
                syStruct.syPtr = nlsClient.CreateSynthesizerRequest(TtsVersion.ShortTts);
                if (syStruct.syPtr.native_request != IntPtr.Zero)
                {
                    Debug.Log("CreateSynthesizerRequest Success");

                    /*
                     * �ṹ��RunParams�д���uuid������״̬����˴������һһ��Ӧ
                     */
                    syStruct.uuid = System.Guid.NewGuid().ToString("N");
                    RunParams demo_params = new RunParams();
                    demo_params.send_audio_flag = false;
                    demo_params.audio_loop_flag = false;
                    globalRunParams[syStruct.uuid] = demo_params;
                }
                else
                {
                    Debug.Log( "CreateSynthesizerRequest Failed");
                }
                syList.AddLast(syStruct);
            }

            cur_sy_closed = "null";
            cur_sy_completed = "null";
        }

        // start synthesizer
        public void startSynthesizer()
        {
            int ret = -1;
            if (syList == null)
            {
                Debug.Log("synthesizer list is null, create first...");
                return;
            }
            else
            {
                LinkedListNode<DemoSpeechSynthesizerStruct> syStruct = syList.First;
                int sy_count = syList.Count;
                for (int i = 0; i < sy_count; i++)
                {
                    DemoSpeechSynthesizerStruct sy = syStruct.Value;
                    if (sy.syPtr.native_request != IntPtr.Zero)
                    {
                        if (appKey == null || appKey.Length == 0)
                        {
                            appKey = ValueSheet.jsonBridge.AppKey;
                        }
                        if (token == null || token.Length == 0)
                        {
                            token = token;
                        }
                        if (appKey == null || token == null ||
                            appKey.Length == 0 || token.Length == 0)
                        {
                            Debug.Log("Start failed, token or appkey is empty");
                            return;
                        }

                        string text = inputField.text = ValueSheet.TextsToSynthesizer.Dequeue();
                        sy.syPtr.SetAppKey(sy.syPtr, appKey);
                        sy.syPtr.SetToken(sy.syPtr, token);
                        sy.syPtr.SetUrl(sy.syPtr, url);
                        sy.syPtr.SetText(sy.syPtr, text);
                        sy.syPtr.SetVoice(sy.syPtr, "zhida");
                        sy.syPtr.SetVolume(sy.syPtr, 50);
                        sy.syPtr.SetFormat(sy.syPtr, "wav");
                        sy.syPtr.SetSampleRate(sy.syPtr, 16000);
                        sy.syPtr.SetSpeechRate(sy.syPtr, 0);
                        sy.syPtr.SetPitchRate(sy.syPtr, 0);
                        sy.syPtr.SetEnableSubtitle(sy.syPtr, true);

                        sy.syPtr.SetOnSynthesisCompleted(sy.syPtr, DemoOnSynthesisCompleted, sy.uuid);
                        sy.syPtr.SetOnBinaryDataReceived(sy.syPtr, DemoOnBinaryDataReceived, sy.uuid);
                        sy.syPtr.SetOnTaskFailed(sy.syPtr, DemoOnSynthesisTaskFailed, sy.uuid);
                        sy.syPtr.SetOnChannelClosed(sy.syPtr, DemoOnSynthesisClosed, sy.uuid);
                        sy.syPtr.SetOnMetaInfo(sy.syPtr, DemoOnMetaInfo, sy.uuid);

                        ret = sy.syPtr.Start(sy.syPtr);
                        if (ret != 0)
                        {
                            Debug.Log("Synthesizer Start failed.");
                        }
                        else
                        {
                            Debug.Log("Transcriber Start success.");
                        }
                    }
                    else
                    {
                    }
                    syStruct = syStruct.Next;
                    if (syStruct == null)
                    {
                        break;
                    }
                }
            }

            if (ValueSheet.TextsToSynthesizer.Count > 0)
            {

            }
        }

        // cancel synthesizer
        public void cancelSynthesizer()
        {
            int ret = -1;
            if (syList == null)
            {
                Debug.Log ("synthesizer list is null, create first...");
                return;
            }
            else
            {
                LinkedListNode<DemoSpeechSynthesizerStruct> syStruct = syList.First;
                int sy_count = syList.Count;
                for (int i = 0; i < sy_count; i++)
                {
                    DemoSpeechSynthesizerStruct sy = syStruct.Value;
                    if (sy.syPtr.native_request != IntPtr.Zero)
                    {
                        if (sy.syPtr.native_request != IntPtr.Zero)
                        {
                            ret = sy.syPtr.Cancel(sy.syPtr);
                        }

                        if (ret != 0)
                        {
                            Debug.Log ("Synthesizer Cancel failed");
                        }
                        else
                        {
                            Debug.Log ("Synthesizer Cancel success");
                        }
                    }
                    else
                    {
                    }
                    syStruct = syStruct.Next;
                    if (syStruct == null)
                    {
                        break;
                    }
                }
            }
        }

        // release synthesizer
        public void releaseSynthesizer()
        {
            if (syList == null)
            {
                Debug.Log ("synthesizer list is null, create first...");
                return;
            }
            else
            {
                int sy_count = syList.Count;
                for (int i = 0; i < sy_count; i++)
                {
                    LinkedListNode<DemoSpeechSynthesizerStruct> syStruct = syList.Last;
                    DemoSpeechSynthesizerStruct sy = syStruct.Value;
                    if (sy.syPtr.native_request != IntPtr.Zero)
                    {
                        nlsClient.ReleaseSynthesizerRequest(sy.syPtr);
                        sy.syPtr.native_request = IntPtr.Zero;
                        globalRunParams.Remove(sy.uuid);

                        Debug.Log ("ReleaseSynthesizerRequest Success");
                    }
                    else
                    {
                        Debug.Log ("ReleaseSynthesizerRequest is nullptr");
                    }
                    syList.RemoveLast();
                }
                syList.Clear();
            }

            cur_sy_closed = "null";
            cur_sy_completed = "null";
        }
        #endregion


        #region SynthesizerCallback
        private CallbackDelegate DemoOnBinaryDataReceived =
            (ref NLS_EVENT_STRUCT e, ref string uuid) =>
            {
                Debug.LogFormat("DemoOnBinaryDataReceived uuid = {0}", uuid);
                Debug.LogFormat("DemoOnBinaryDataReceived taskId = {0}", e.taskId);
                Debug.LogFormat("DemoOnBinaryDataReceived dataSize = {0}", e.binaryDataSize);
                //cur_sy_completed = e.taskId + ", binaryDataSize : " + e.binaryDataSize;

                /*
                 * ����ע�⣺�˻ص�Ƶ�ʽϸߣ���Ҫ����������յ���Ƶ���ݵ�ת�档
                 * ����д����Ƶ�����׳��ֶ�λص�ͬһʱ��ͬʱ����ͬһ��fs�����⡣
                 */
#if True
                FileStream fs;
                filename = e.taskId + ".wav";
                Debug.LogFormat("DemoOnBinaryDataReceived current filename = {0}", filename);
                if (File.Exists(filename))
                {
                    fs = new FileStream(filename, FileMode.Append, FileAccess.Write);
                }
                else
                {
                    fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
                }
                fs.Write(e.binaryData, 0, e.binaryDataSize);
                fs.Close();
#endif
            };
        private CallbackDelegate DemoOnSynthesisClosed =
            (ref NLS_EVENT_STRUCT e, ref string uuid) =>
            {
                Debug.LogFormat("DemoOnSynthesisClosed user uuid = {0}", uuid);
                string msg = System.Text.Encoding.Default.GetString(e.msg);
                Debug.LogFormat("DemoOnSynthesisClosed msg = {0}", msg);
                cur_sy_closed = "msg : " + msg;

                udpSendLocker =ValueSheet.IsSynthesizerRunning = false;

            };
        private CallbackDelegate DemoOnSynthesisTaskFailed =
            (ref NLS_EVENT_STRUCT e, ref string uuid) =>
            {
                Debug.LogFormat("DemoOnSynthesisTaskFailed user uuid = {0}", uuid);
                string msg = System.Text.Encoding.Default.GetString(e.msg);
                Debug.LogFormat("DemoOnSynthesisTaskFailed msg = {0}", msg);
                cur_sy_completed = "msg : " + msg;

            };
        private CallbackDelegate DemoOnSynthesisCompleted =
            (ref NLS_EVENT_STRUCT e, ref string uuid) =>
            {
                Debug.LogFormat("DemoOnSynthesisCompleted user uuid = {0}", uuid);
                string msg = System.Text.Encoding.Default.GetString(e.msg);
                Debug.LogFormat("DemoOnSynthesisCompleted msg = {0}", msg);
                cur_sy_completed = "result : " + msg;

                ValueSheet.IsSynthesizerRunning = false;

            };
        private CallbackDelegate DemoOnMetaInfo =
            (ref NLS_EVENT_STRUCT e, ref string uuid) =>
            {
                Debug.LogFormat("DemoOnMetaInfo user uuid = {0}", uuid);
                string msg = System.Text.Encoding.Default.GetString(e.msg);
                Debug.LogFormat("DemoOnMetaInfo msg = {0}", msg);
                //cur_sy_completed = "metaInfo : " + e.msg;
            };
        #endregion
    }

}

