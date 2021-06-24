using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DoubleSizedFading : MonoBehaviour
{
    [SerializeField] Text textComp;
    public float animTime;
    float time = 0;
    Color startColor;
    private void Start()
    {
        startColor = textComp.color;
    }
    private void Update()
    {
        time += Time.unscaledDeltaTime;
        textComp.transform.localScale = Vector3.one * (1 + time / animTime);
        var c = startColor;
        c.a = 1 - time / animTime;
        textComp.color = c;
        if (time >= animTime)
        {
            Destroy(gameObject);
        }
    }
}
