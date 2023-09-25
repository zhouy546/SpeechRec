//*********************❤*********************
// 
// 文件名（File Name）：	GetUDPMessage.cs
// 
// 作者（Author）：			LoveNeon
// 
// 创建时间（CreateTime）：	Don't Care
// 
// 说明（Description）：	只负责接受消息，不进行处理
// 
//*********************❤*********************
using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public class GetUDPMessage: MonoBehaviour
{

    [Tooltip("消息处理类")] public DealWithUDPMessage m_messageManage;
    [Tooltip("接受端口号")] public static int m_ReceivePort = 29010;

    private Socket m_newsock;//定义一个socket变量
    public static IPEndPoint m_ip;//定义一个IP地址和端口号
    private int m_recv;//定义一个接受值的变量
    private byte[] m_data = new byte[1024];//定义一个二进制的数组用来获取客户端发过来的数据包
    private string m_mydata;
    private List<string> m_array_data = new List<string>();
    Thread test;
    /// <summary>
    /// 设置网络
    /// </summary>
    void Start()
    {
        InitializationUdp();
    }

    public void InitializationUdp() {
        //得到本机IP，设置TCP端口号        
        m_ip = new IPEndPoint(IPAddress.Any, m_ReceivePort);//设置自身的IP和端口号，在这里IPAddress.Any是自动获取本机IP
        m_newsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//实例化socket对象设置寻址方案为internetwork（IP版本的4存放）,设置Soket的类型，为Dgram（支持数据报形式的数据），设置协议的类型，为UDP
        //绑定网络地址
        m_newsock.Bind(m_ip);//绑定IP
        test = new Thread(BeginListening);//定义一个子线程
        test.Start();//子线程开始
    }

    public void StopUdp() {

    }

	/// <summary>
	/// 更新
	/// </summary>
    void Update()
    {
        //Debug.Log("GetUPDMessage.cs:");
        //判断是否有数据
        if (m_array_data.Count <= 0)
        {           
            return;
        }
        //如果有数据 则循环遍历传入处理类
        for (int i = m_array_data.Count-1; i >= 0; --i)
        {
            m_messageManage.MessageManage(m_array_data[i]);
            m_array_data.RemoveAt(i);//传入后移除
        }
        //Debug.Log(m_array_data.Count + "+" + m_array_data.Capacity);
        m_array_data.Clear();//此步为了让集合的容量跟着清空

    }
	/// <summary>
	/// 线程接受
	/// </summary>
    void BeginListening()
    {

        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);//实例化一个网络端点，设置为IPAddress.Any为自动获取跟我通讯的IP，0代表所有的地址都可以
        EndPoint Remote = (EndPoint)(sender);//实例化一个地址结束点来标识网络路径
        //  Debug.Log(Encoding.ASCII.GetString(data, 0, recv));//输出二进制转换为string类型用来测试
        while (true)
        {
            m_data = new byte[1024];//实例化data
            m_recv = m_newsock.ReceiveFrom(m_data, ref Remote);//将数据包接收到的数据放入缓存点，并存储终节点
            m_mydata = System.Text.Encoding.GetEncoding("gb2312").GetString(m_data, 0, m_recv);
            m_array_data.Add(m_mydata);//加入数组            
        }
    }
	/// <summary>
	/// 退出后关闭网络
	/// </summary>
    void OnApplicationQuit()
    {
        test.Abort();
        m_newsock.Close();
    }
}
