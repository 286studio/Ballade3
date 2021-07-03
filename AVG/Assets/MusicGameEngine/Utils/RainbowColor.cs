using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RainbowColor : MonoBehaviour
{
    [SerializeField] Graphic subject;
    [SerializeField] Color[] colorList;
    int i;
    float delay = 0.5f;
    float time;
    private void Update()
    {
        time += Time.deltaTime;
        if (time >= delay)
        {
            time = 0;
            i = (i + 1) % colorList.Length;
        }
        subject.color = Color.Lerp(colorList[i], colorList[(i + 1) % colorList.Length], time / delay);
    }
}
