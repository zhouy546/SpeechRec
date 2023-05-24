using System.Collections;
using System.Collections.Generic;


public static class ValueSheet
{

  //  public static string appKey = "qrKYAlt8R80O4N5c";

    public static string url;

    public static Root jsonBridge;

    public static bool FirstSayWords =true;

    public static Queue<byte[]> AudioStream = new Queue<byte[]>();

    
}
public class Root
{

    public string AppKey;
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