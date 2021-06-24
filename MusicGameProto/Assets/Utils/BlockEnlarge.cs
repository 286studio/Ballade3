using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlockEnlarge : MonoBehaviour
{
    [SerializeField] Graphic[] coloringParts;
    [SerializeField] float lifetime;
    [SerializeField] Vector3 startScale;
    [SerializeField] Vector3 endScale;
    [SerializeField] bool fades;
    Color startColor;
    float startTime;
    Color endColor;
    RectTransform rt;
    Vector2 startPos;
    [HideInInspector] public Vector2 endPos;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        foreach (Graphic g in coloringParts) g.color = startColor;
        startTime = Time.time;
        rt.localScale = startScale;
        startPos = rt.anchoredPosition;
        if (fades)
        {
            endColor = startColor;
            endColor.a = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float frac = (Time.time - startTime) / lifetime / 2;
        if (frac >= 2)
        {
            Destroy(gameObject);
        }
        else if (frac >= 1)
        {
            rt.localScale = Vector3.Lerp(startScale, endScale, frac - 1);
            if (fades) foreach (Graphic g in coloringParts) g.color = Color.Lerp(startColor, endColor, frac - 1);
        }
        else
        {
            rt.anchoredPosition = Utils.NLerp(startPos, endPos, frac, NlerpMode.OutSine);
        }
    }

    public static void Create(Sprite sprite, Color c, Vector2 startPos, Vector2 endPos, Transform parent)
    {
        BlockEnlarge be = Instantiate(Resources.Load<GameObject>("BlockEnlarge"), parent).GetComponent<BlockEnlarge>();
        be.startColor = c;
        be.GetComponent<Image>().sprite = sprite;
        (be.transform as RectTransform).anchoredPosition = startPos;
        be.endPos = endPos + new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
    }
}
