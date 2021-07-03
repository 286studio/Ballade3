using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Video;

public delegate void Void_Float(float f);
public delegate void Void_FloatInt(float f, int i);
public delegate void Void_Int(int i);

public enum NlerpMode
{
    InSine,
    OutSine
}

public enum Platform
{
    Default,
    Mobile
}

public class Utils
{
    public static Color[] colorList = new Color[]
    {
        new Color(1,0,0),
        new Color(1,1,0),
        new Color(0,1,0),
        //new Color(0,1,1),
        new Color(0,0,1),
        new Color(1,0,1),
    };
    public static Color GetRandomColor()
    {
        return colorList[Random.Range(0, colorList.Length)];
    }
    public static float Pow(float b, int p)
    {
        float result = 1;
        for (int i = 0; i < p; ++i) result *= b;
        return result;
    }
    public static Vector2 ScreenToCanvasPos(Vector2 pos)
    {
        float frac_x = pos.x / Screen.width;
        float frac_y = pos.y / Screen.height;
        float x = ScreenAdjust.canvasRes.x * (frac_x - 0.5f);
        float y = ScreenAdjust.canvasRes.y * (frac_y - 0.5f);
        return new Vector2(x, y);
    }

    public static Vector2 LerpWithoutClamp(Vector2 a, Vector2 b, float frac)
    {
        return a + (b - a) * frac;
    }

    public static Vector2 NLerp(Vector2 a, Vector2 b, float frac, NlerpMode curve)
    {
        if (frac <= 0) return a; else if (frac >= 1) return b;
        if (curve == NlerpMode.InSine) return a + (b - a) * Mathf.Sin(frac * Mathf.PI / 2);
        else if (curve == NlerpMode.OutSine) return a + (b - a) * (1 - Mathf.Sin((1 - frac) * Mathf.PI / 2));
        return a + (b - a) * frac;
    }
    public static void SetVideoVolume(VideoPlayer vp, float volume)
    {
        for (ushort u = 0; u < vp.audioTrackCount; ++u)
            vp.SetDirectAudioVolume(u, volume);
    }
    public static bool ControlKeyHeldDown() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    public static bool AltKeyHeldDown() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
}
