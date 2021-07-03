using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenAdjust : MonoBehaviour
{
    [SerializeField] CanvasScaler cs;
    public static Vector2 canvasRes;
    private void Awake()
    {
        cs = GetComponent<CanvasScaler>();
        float curSrceenRatio = (float) Screen.width / Screen.height;
        float defaultSrceenRatio = (float)DefRes.x / DefRes.y;
        canvasRes = new Vector2(DefRes.x, DefRes.y);
        if (curSrceenRatio >= defaultSrceenRatio) // Match Height
        {
            canvasRes.x = canvasRes.y * curSrceenRatio;
            cs.matchWidthOrHeight = 1;
        }
        else // Match Width
        {
            canvasRes.y = canvasRes.x / curSrceenRatio;
            cs.matchWidthOrHeight = 0;
        }
    }
}
