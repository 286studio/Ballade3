using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VocalText : RhythmObject
{
    public Vector2 startPos, endPos;
    public int rotation;
    public int length;
    public string voEvent;
    public string text;
    public float beatInterval;
    public int num;
    public Color animColor;
    public int maxMiss;
    public int beatLifetime;
    public bool noAnim;

    [SerializeField] Text textComp;
    [SerializeField] Text numComp;
    public override RhythmType Type => RhythmType.Misc;

    Beat[] beats;
    int state = 0;
    FMOD.Studio.EventInstance voIns;
    Color startColor, endColor;
    float frac;
    float startFadingTime;

    Text animNum;
    Vector2 animNumStartPos;
    Text animText;
    Vector2 animTextStartPos;

    protected override void Start()
    {
        base.Start();
        Apply();
        startColor = textComp.color;
        endColor = new Color(0, 0, 0, 0);

        animNum = Instantiate(numComp.gameObject, transform).GetComponent<Text>();
        animNum.color = animColor;
        animNumStartPos = animNum.rectTransform.anchoredPosition = numComp.rectTransform.anchoredPosition;
        animNum.gameObject.SetActive(false);

        animText = Instantiate(textComp.gameObject, transform).GetComponent<Text>();
        animText.color = animColor;
        animTextStartPos = animText.rectTransform.anchoredPosition = textComp.rectTransform.anchoredPosition;
        animText.gameObject.SetActive(false);
    }
    protected override void Update()
    {
        switch (state)
        {
            case 0: // 飞入
                frac = (Time.time - createTime) / fallingTime;
                if (frac >= 1)
                {
                    StartCoroutine(CreateBeats());
                    state = 1;
                }
                else rt.anchoredPosition = Utils.NLerp(startPos, endPos, frac, NlerpMode.OutSine);
                break;
            case 1: // 等待按键
                if (!RhythmGameManager.exits[exit].current)
                {
                    RhythmGameManager.exits[exit].current = this;
                }
                animNum.gameObject.SetActive(RhythmGameManager.exits[exit].current == this);
                animNum.rectTransform.anchoredPosition = animNumStartPos + new Vector2(Random.Range(0, 10), Random.Range(0, 10));
                break;
            case 2: // 全部按键成功
                if (!noAnim) animText.rectTransform.anchoredPosition = animTextStartPos + new Vector2(Random.Range(0, 10), Random.Range(0, 10));
                FMOD.Studio.PLAYBACK_STATE pbs;
                voIns.getPlaybackState(out pbs);
                if (pbs == FMOD.Studio.PLAYBACK_STATE.STOPPED) Destroy(gameObject);
                break;
            case 3: // 失败
                frac = Time.time - startFadingTime;
                if (frac >= 1)
                    Destroy(gameObject);
                else 
                    textComp.color = Color.Lerp(startColor, endColor, frac);
                break;
        }
    }

    protected override void CheckActivateCondition()
    {
        throw new System.NotImplementedException();
    }

    protected override void Update_Activated()
    {
        throw new System.NotImplementedException();
    }
    void Apply()
    {
        rt.anchoredPosition = startPos;
        rt.localEulerAngles = new Vector3(0, 0, rotation);
        textComp.text = text;
        numComp.text = num.ToString();
        voIns = FMODUnity.RuntimeManager.CreateInstance("event:/" + voEvent);
    }
    IEnumerator CreateBeats()
    {
        if (length == 1)
        {
            beats = new Beat[1];
            beats[0] = (Beat)Instantiate(Resources.Load<GameObject>("Beat"), rt).GetComponent<Beat>().Initialize(exit, PanelType.Right, textComp.color, perfectScore, goodScore, badScore);
            beats[0].fallingTime = 0.25f * beatLifetime;
            beats[0].lifetime = 0.75f * beatLifetime;
            beats[0].parent = this;
            beats[0].rt.anchoredPosition = Vector2.zero;
            beats[0].OnScored += OnOnlyBeatScored;
        }
        else
        {
            beats = new Beat[length];
            for (int i = 0; i < length; ++i)
            {
                beats[i] = (Beat)Instantiate(Resources.Load<GameObject>("Beat"), rt).GetComponent<Beat>().Initialize(exit, PanelType.Right, textComp.color, perfectScore, goodScore, badScore);
                beats[i].fallingTime = 0.25f * beatLifetime;
                beats[i].lifetime = 0.75f * beatLifetime;
                beats[i].parent = this;
                beats[i].rt.anchoredPosition = new Vector2(-Beat.radius * (length - 1) + Beat.radius * 2 * i, 0);
                if (i == 0)
                {
                    beats[i].OnScored += OnBeatFirstScored;
                    yield return new WaitForSeconds(beatInterval);
                }
                else if (i != length - 1)
                {
                    beats[i].OnScored += OnBeatScored;
                    yield return new WaitForSeconds(beatInterval);
                }
                else
                {
                    beats[i].OnScored += OnBeatLastScored;
                }
            }
        }
    }
    void OnBeatFirstScored(int s)
    {
        if (s == 0) --maxMiss;
        if (!RhythmGameManager.exits[exit].current)
        {
            RhythmGameManager.exits[exit].current = this;
        }
    }
    void OnBeatScored(int s)
    {
        if (s == 0) --maxMiss;
    }
    void OnBeatLastScored(int s)
    {
        if (s == 0) --maxMiss;
        if (maxMiss >= 0)
        {
            Destroy(numComp.gameObject);
            Destroy(animNum.gameObject);
            if (noAnim) Destroy(animText.gameObject);
            animText.gameObject.SetActive(true);
            voIns.start();
            state = 2;
        }
        else
        {
            startFadingTime = Time.time;
            Destroy(numComp.gameObject);
            Destroy(animNum.gameObject);
            Destroy(animText.gameObject);
            state = 3;
        }
    }
    void OnOnlyBeatScored(int s)
    {
        if (s == 0) --maxMiss;
        if (!RhythmGameManager.exits[exit].current)
        {
            RhythmGameManager.exits[exit].current = this;
        }
        if (maxMiss >= 0)
        {
            Destroy(numComp.gameObject);
            Destroy(animNum.gameObject);
            animText.gameObject.SetActive(true);
            voIns.start();
            state = 2;
        }
        else
        {
            startFadingTime = Time.time;
            Destroy(numComp.gameObject);
            Destroy(animNum.gameObject);
            Destroy(animText.gameObject);
            state = 3;
        }
    }
}
