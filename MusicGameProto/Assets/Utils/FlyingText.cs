using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlyingText : MonoBehaviour
{
    public string msg;
    public Color color = Color.white;
    [SerializeField] float lifetime;
    [SerializeField] float height;
    RectTransform rt;
    float frac, step, time;
    Vector2 canvasPoint;
    private void Start()
    {
        rt = GetComponent<RectTransform>();
        frac = 0;
        step = lifetime / 20;
        Text t = GetComponent<Text>();
        t.text = msg;
        t.color = color;
    }
    private void Update()
    {
        time += Time.deltaTime;
        if (time >= step)
        {
            time = 0;
            rt.anchoredPosition = canvasPoint + Vector2.Lerp(Vector2.zero, new Vector2(0, height), frac);
            frac += step;
            if (frac >= 1) Destroy(gameObject);
        }
    }
    public FlyingText Init(string _msg, Color c)
    {
        msg = _msg;
        color = c;
        return this;
    }

    public static void Create(string msg, Color c, Vector2 pos, Transform parent)
    {
        FlyingText ft = Instantiate(Resources.Load<GameObject>("FlyingText"), parent).GetComponent<FlyingText>().Init(msg, c);
        (ft.transform as RectTransform).anchoredPosition = pos;
        ft.canvasPoint = pos;
    }
}
