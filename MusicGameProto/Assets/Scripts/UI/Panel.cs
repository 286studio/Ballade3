using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PanelSize
{
    public static float x = 940;
    public static float y = 380;
}
public class PanelPos
{
    public static float x = 475;
    public static float y = -333;
}

public enum PanelType
{
    Left, Right
}

public class Panel : MonoBehaviour
{
    public static Panel Left;
    public static Panel Right;
    public static float exitPos = 64;
    public static float longExitPos = PanelSize.x - exitPos;
    public static float bottomPos = 815;
    public static float alpha = 1f;

    // Panel State
    // 0=No Panel
    // 1=Left Panel
    // 2=Both Panels
    public static int state;
    static bool isBusy;
    static float animTime = 0.5f;
    public static void ShowLeft()
    {
        if (isBusy)
        {
            Debug.Log("Panel Operation Too Often. Voided.");
            return;
        }
        if (state != 0) return;

        isBusy = true;

        void OnShowLeftFinished()
        {
            Left.ShowBottom(true);
            Left.ShowExits(true);
            foreach (var exit in RhythmGameManager.exits)
                if (exit.panel == PanelType.Left)
                    exit.SetX(longExitPos);
            isBusy = false;
        }

        Left.StartAnimtion(new Vector2(0, PanelPos.y), new Vector2(0, PanelPos.y), 
            Vector2.one, new Vector2(2 * PanelSize.x, PanelSize.y),
            alpha, alpha, 
            OnFinished: OnShowLeftFinished);
        state = 1;
    }

    public static void HideLeft()
    {
        if (isBusy)
        {
            Debug.Log("Panel Operation Too Often. Voided.");
            return;
        }
        if (state != 1) return;
        isBusy = true;
        Left.ShowBottom(false);
        Left.ShowExits(false);
        Left.StartAnimtion(new Vector2(0, PanelPos.y), new Vector2(0, PanelPos.y), new Vector2(2 * PanelSize.x, PanelSize.y), new Vector2(2 * PanelSize.x, PanelSize.y), alpha, 0, 0, true, delegate { isBusy = false; });
        state = 0;
    }
    public static void ShowRight()
    {
        if (isBusy)
        {
            Debug.Log("Panel Operation Too Often. Voided.");
            return;
        }
        if (state != 1) return;

        isBusy = true;

        void MoveExits(float frac)
        {
            foreach (var exit in RhythmGameManager.exits)
                if (exit.panel == PanelType.Left)
                    exit.SetX(Mathf.Lerp(longExitPos, -exitPos, frac));
        }
        Left.OnAnimUpdate += MoveExits;
        
        void OnLeftMoveFinished()
        {
            Left.OnAnimUpdate -= MoveExits;
            foreach (var exit in RhythmGameManager.exits)
                if (exit.panel == PanelType.Left)
                    exit.SetX(-exitPos);
        }
        Left.StartAnimtion(new Vector2(0, PanelPos.y), new Vector2(-PanelPos.x, PanelPos.y), new Vector2(2 * PanelSize.x, PanelSize.y), new Vector2(PanelSize.x, PanelSize.y), alpha, alpha, OnFinished: OnLeftMoveFinished);

        void OnRightMoveFinished()
        {
            Right.ShowBottom(true);
            Right.ShowExits(true);
            isBusy = false;
        }
        Right.StartAnimtion(new Vector2(PanelPos.x, PanelPos.y), new Vector2(PanelPos.x, PanelPos.y), Vector2.one, new Vector2(PanelSize.x, PanelSize.y), alpha, alpha, animTime, false, OnRightMoveFinished);
        state = 2;
    }

    public static void HideRight()
    {
        if (isBusy)
        {
            Debug.Log("Panel Operation Too Often. Voided.");
            return;
        }
        if (state != 2) return;
        isBusy = true;
        void MoveExits(float frac)
        {
            foreach (var exit in RhythmGameManager.exits)
                if (exit.panel == PanelType.Left)
                    exit.SetX(Mathf.Lerp(-exitPos, longExitPos, frac));
        }
        Left.OnAnimUpdate += MoveExits;

        void OnLeftMoveFinished()
        {
            Left.OnAnimUpdate -= MoveExits;
            isBusy = false;
        }
        Left.StartAnimtion(new Vector2(-PanelPos.x, PanelPos.y), new Vector2(0, PanelPos.y), new Vector2(PanelSize.x, PanelSize.y), new Vector2(2 * PanelSize.x, PanelSize.y), alpha, alpha, animTime, false, OnLeftMoveFinished);

        Right.ShowBottom(false);
        Right.ShowExits(false);
        Right.StartAnimtion(new Vector2(PanelPos.x, PanelPos.y), new Vector2(PanelPos.x, PanelPos.y), new Vector2(PanelSize.x, PanelSize.y), new Vector2(PanelSize.x, PanelSize.y), alpha, 0, 0, true);
        state = 1;
    }

    public static void HideBoth()
    {
        if (isBusy)
        {
            Debug.Log("Panel Operation Too Often. Voided.");
            return;
        }
        if (state != 2) return;

        isBusy = true;

        Left.ShowBottom(false);
        Left.ShowExits(false);
        Left.StartAnimtion(new Vector2(-PanelPos.x, PanelPos.y), new Vector2(-PanelPos.x, PanelPos.y), new Vector2(PanelSize.x, PanelSize.y), new Vector2(PanelSize.x, PanelSize.y), alpha, 0, 0, true);
        
        Right.ShowBottom(false);
        Right.ShowExits(false);
        Right.StartAnimtion(new Vector2(PanelPos.x, PanelPos.y), new Vector2(PanelPos.x, PanelPos.y), new Vector2(PanelSize.x, PanelSize.y), new Vector2(PanelSize.x, PanelSize.y), alpha, 0, 0, true, delegate { isBusy = false; });
        state = 0;
    }

    public Bottom bottom;
    [HideInInspector] public PanelType panelType;
    [Header("TempUI")] 
    [SerializeField] Image frame;
    [SerializeField] Image bg;
    [SerializeField] Image shadow;

    public RectTransform rt;

    Vector2 startSize, endSize;
    Vector2 startPos, endPos;
    float startAlpha, endAlpha;
    float startTime;

    public event Void_Float OnAnimUpdate;

    private void OnValidate()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        Visible = false;
        bottom.GetComponent<RectTransform>().anchoredPosition = new Vector2(panelType == PanelType.Left ? -bottomPos : bottomPos, PanelPos.y);
        ShowBottom(false);
    }

    private void Update()
    {
        float diff = Time.time - startTime;
        if (diff >= 0 && diff <= animTime)
        {
            float frac = diff / animTime;
            // size lerp
            SetSize(Vector2.Lerp(startSize, endSize, frac));
            // position lerp
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, frac);
            // alpha lerp
            if (startAlpha != endAlpha)
                foreach (var g in GetComponentsInChildren<Graphic>())
                {
                    var c = g.color;
                    c.a = Mathf.Lerp(startAlpha, endAlpha, frac);
                    g.color = c;
                }
            OnAnimUpdate?.Invoke(frac);
        }
    }

    bool visible = true;
    public bool Visible
    {
        set
        {
            if (visible != value)
            {
                visible = value;
                foreach (var g in GetComponentsInChildren<Graphic>()) g.enabled = visible;
            }
        }
    }

    public void ShowBottom(bool show)
    {
        bottom.gameObject.SetActive(show);
    }

    public void ShowExits(bool show)
    {
        foreach (var exit in RhythmGameManager.exits)
        {
            if (exit.panel == panelType)
            {
                exit.obj.SetActive(show);
                exit.idctor.SetActive(show);
            }
        }
    }

    public void SetSize(Vector2 size)
    {
        frame.rectTransform.sizeDelta = size;
        shadow.rectTransform.sizeDelta = bg.rectTransform.sizeDelta = size + 42 * Vector2.one;
    }

    void StartAnimtion(Vector2 startPos, Vector2 endPos, Vector2 startSize, Vector2 endSize, float startAlpha, float endAlpha, float delay = 0, bool disappear = false, UnityAction OnFinished = null)
    {
        StartCoroutine(Animation(startPos, endPos, startSize, endSize, startAlpha, endAlpha, delay, disappear, OnFinished));
    }

    IEnumerator Animation(Vector2 startPos, Vector2 endPos, Vector2 startSize, Vector2 endSize, float startAlpha, float endAlpha, float delay, bool disappear, UnityAction OnFinished)
    {
        Visible = true;
        if (delay > 0) yield return new WaitForSeconds(delay);
        this.startPos = startPos;
        this.endPos = endPos;
        this.startSize = startSize;
        this.endSize = endSize;
        this.startAlpha = startAlpha;
        this.endAlpha = endAlpha;
        foreach (var g in GetComponentsInChildren<Graphic>())
        {
            var c = g.color;
            c.a = this.startAlpha;
            g.color = c;
        }
        startTime = Time.time;
        yield return new WaitForSeconds(animTime);
        rt.anchoredPosition = endPos;
        SetSize(endSize);
        foreach (var g in GetComponentsInChildren<Graphic>())
        {
            var c = g.color;
            c.a = this.endAlpha;
            g.color = c;
        }
        Visible = !disappear;
        OnFinished?.Invoke();
    }
}
