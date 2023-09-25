using System.Collections;
using System.Collections.Generic;


public static class ValueSheet
{

  //  public static string appKey = "qrKYAlt8R80O4N5c";

    public static string url;

    public static Root jsonBridge;

    public static bool FirstSayWords =true;

    public static Queue<byte[]> AudioStream = new Queue<byte[]>();


    public static bool isFirstSynthesizer = true;

    public static bool IsSynthesizerRunning = false;

    public static Queue<string> TextsToSynthesizer = new Queue<string>();

    
}
public class Root
{

    public string AppKey;

    public string AkId;

    public string AkSecret;

    public string TargetIP;

    public int TargetPort;

    public string WavTargetIP;

    public int WavTargetPort;

    public double volumeThrehold;

    public double TimeThrehold;

}