using System.Collections;
using System.Collections.Generic;


public static class ValueSheet
{

    public static string appKey = "KdhnnlXtjtz8eBYm";

    public static string url;

    public static Root jsonBridge;

    public static bool FirstSayWords =true;

    public static Queue<byte[]> AudioStream = new Queue<byte[]>();

    
}
public class Root
{
    /// <summary>
    /// 
    /// </summary>
    public string AkId;
    /// <summary>
    /// 
    /// </summary>
    public string AkSecret;
    /// <summary>
    /// 
    /// </summary>
    public string TargetIP;
    /// <summary>
    /// 
    /// </summary>
    public int TargetPort;

    public double volumeThrehold;

    public double TimeThrehold;

}