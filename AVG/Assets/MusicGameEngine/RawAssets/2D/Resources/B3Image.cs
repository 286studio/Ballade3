using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class B3Image : MonoBehaviour
{
    public float fadeIn = 0.5f;
    Image image;
    float time;
    bool fadingIn = true;

    private void Awake()
    {
        image = GetComponent<Image>();
        float ratio = image.rectTransform.sizeDelta.x / image.rectTransform.sizeDelta.y;
        float targetRatio = DefRes.x / DefRes.y;
        image.rectTransform.sizeDelta = ratio > targetRatio ? new Vector2(DefRes.x, DefRes.x / ratio) : new Vector2(DefRes.y * ratio, DefRes.y);
        image.color = Color.clear;
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (fadingIn)
        {
            time += Time.deltaTime;
            float frac = time / fadeIn;
            if (frac <= 1) image.color = new Color(1, 1, 1, frac);
            else
            {
                image.color = Color.white;
                fadingIn = false;
            }
        }
    }
}
