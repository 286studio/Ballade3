using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Harp : RhythmObject
{
    public bool rouxian;
    public int roufa;
    public int rousu;
    public int width;
    public float timeLast;
    public float limitTime;
    public float cooldown;
    [SerializeField] Image harpImage;
    [SerializeField] Image harpImage2;
    public override RhythmType Type => RhythmType.Rouxian;

    public static Harp ins;
    static int i;
    static int[] heights = new int[6] { 325, 195, 65, -65, -195, -325 };
    public static int GetHeight() { i = (i + 1) % 6; return heights[i]; }

    float animTime = 0.5f;
    float startTime = 0;
    bool harpImageIn = false;
    bool harpImageOut = false;
    float harpImageStartHeight = 1;
    float harpImageEndHeight = DefRes.y - BlockSize.y * 4;
    float harpImageOverHeight = DefRes.y - BlockSize.y * 3;
    bool interacting = false;

    Rect touchField; // 0=max; 1=middle; 2=min
    float center_x;
    float score_x;
    Vector2 hintAnimStart;
    Vector2 hintAnimEnd;
    float hintAnimTotalTime;
    float hintAnimTime;
    bool hintPingpong;
    RectTransform hint;
    float trailDelayCount;

    float quality, volume;


    public enum LyricsState { None, Parsing, FadingOut }
    LyricsState lyricsState;
    Text[] lyricsTexts;
    Text[] lyricsAnimTexts;
    float[] lyricsCharStartTimes;
    Vector2[] lyricsStartPositions;
    float lyricsTimeStep;
    float lyricsTime;
    int curlc;
    float lyricsAnimTime = 1.5f;
    float lyricsAlpha = 1f;

    protected override void Start()
    {
        if (ins) Destroy(gameObject); else ins = this;
        base.Start();
        var pos = rt.anchoredPosition;
        pos.y = GetBottom();
        rt.anchoredPosition = pos;

        float x = rt.anchoredPosition.x - BlockSize.x / 2f;
        float y = rt.anchoredPosition.y + BlockSize.y / 2f;
        float w = exits[exit + width - 1].y_bot - exits[exit].y_top;
        float h = harpImageEndHeight;
        touchField = new Rect(x, y, w, h);
        center_x = x + w / 2;
        score_x = center_x;

        harpImage2.enabled = harpImage.enabled = true;
        harpImage2.rectTransform.sizeDelta = harpImage.rectTransform.sizeDelta = new Vector2(touchField.width, harpImageStartHeight);
        harpImage2.rectTransform.anchoredPosition = harpImage.rectTransform.anchoredPosition = new Vector2(touchField.width / 2 - BlockSize.x / 2, BlockSize.y / 2f);
        harpImageIn = true;
        startTime = Time.time;

        if (rouxian)
        {
            float hintAnim_x1 = 0;
            float hintAnim_x2 = w;
            if (roufa == 0) // 深揉
            {
                hintAnim_x1 = 0;
                hintAnim_x2 = w - BlockSize.x;
            }
            else if (roufa == 1) // 浅揉
            {
                hintAnim_x1 = -BlockSize.x / 2 + 0.3f * w;
                hintAnim_x2 = -BlockSize.x / 2 + 0.7f * w;
            }
            if (rousu == 0) // 快揉
            {
                hintAnimTotalTime = 1.5f * limitTime;
            }
            else if (rousu == 1) // 慢揉
            {
                hintAnimTotalTime = 2.25f * limitTime;
            }
            hintAnimStart = new Vector2(hintAnim_x1, -rt.anchoredPosition.y);
            hintAnimEnd = new Vector2(hintAnim_x2, -rt.anchoredPosition.y);
        }
    }

    protected override void Update()
    {
        if (rouxian) UpdateRouxian();
        UpdateAnimation();
    }
    void UpdateAnimation()
    {
        if (rouxian)
        {
            hintAnimTime += Time.deltaTime;
            if (hintAnimTime >= hintAnimTotalTime)
            {
                hintPingpong = !hintPingpong;
                hintAnimTime = 0;
            }
            float hfrac = hintPingpong ? 1 - hintAnimTime / hintAnimTotalTime : hintAnimTime / hintAnimTotalTime;
            var dotPos = Vector2.Lerp(hintAnimStart, hintAnimEnd, hfrac);
            if (hintPingpong) dotPos.y = BlockSize.y / 2 + touchField.height / 2 + BlockSize.y / 2 * Mathf.Sin(2 * Mathf.PI * hfrac);
            else dotPos.y = BlockSize.y / 2 + touchField.height / 2 + BlockSize.y / 2 * Mathf.Cos(2 * Mathf.PI * hfrac + 0.5f * Mathf.PI);
            //hint.anchoredPosition = dotPos;

            if (trailDelayCount >= 0.05f)
            {
                var trail = Instantiate(Resources.Load<GameObject>("Trail"), transform).GetComponent<RectTransform>();
                trail.anchoredPosition = dotPos;
                if (roufa == 1) trail.localScale *= 0.75f;
                trailDelayCount = 0;
            }
            else
            {
                trailDelayCount += Time.deltaTime;
            }
        }

        if (harpImageIn)
        {
            float frac = (Time.time - startTime) / animTime;
            if (frac >= 1)
            {
                harpImageIn = false;
                frac = 1;
                StartCoroutine(Interacting());
            }
            float y = frac <= 0.8 ? (harpImageStartHeight + frac * 1.25f * (harpImageOverHeight - harpImageStartHeight)) : (harpImageOverHeight + (frac - 0.8f) * 5 * (harpImageEndHeight - harpImageOverHeight));
            harpImage2.rectTransform.sizeDelta = harpImage.rectTransform.sizeDelta = new Vector2(touchField.width, y);
        }
        if (harpImageOut)
        {
            float frac = (Time.time - startTime) / animTime;
            if (frac >= 1)
            {
                harpImageOut = false;
                frac = 1;
                harpImage2.enabled = harpImage.enabled = false;
                Timeline.SetParam("Quality", 0);
                Timeline.SetParam("SingerOn", 1);
                Destroy(gameObject);
            }
            harpImage2.rectTransform.sizeDelta = harpImage.rectTransform.sizeDelta = new Vector2(touchField.width, harpImageEndHeight + frac * (harpImageStartHeight - harpImageEndHeight));
        }
    }

    class RouxianData
    {
        public int leftToRight; // no use
        public int rightToLeft; // no use
        public Vector2 lastPos;
        public float leftOrRightMax;
        public float startTime;
        public float cooldownStartTime;
        public int touchCount;
        public float untouchTime;
    }
    RouxianData rd = new RouxianData();

    void UpdateRouxian()
    {
        bool getTouched = false;
        Vector2 touchPos = new Vector2();
        for (int i = 0; i < Input.touchCount; ++i)
        {
            touchPos = Utils.ScreenToCanvasPos(Input.GetTouch(i).position);
            if (touchField.Contains(touchPos))
            {
                getTouched = true;
                break;
            }
        }
        if (getTouched)
        {
            if (rd.lastPos != touchPos) ++rd.touchCount;
            rd.leftOrRightMax = Mathf.Max(rd.leftOrRightMax, Mathf.Abs(touchPos.x - center_x) / touchField.width * 2);
            if (touchPos.x > center_x && rd.lastPos.x < center_x)
            {
                ++rd.leftToRight;
                RouxianValidation(touchPos.y);
            }
            if (touchPos.x < center_x && rd.lastPos.x > center_x)
            {
                ++rd.rightToLeft;
                RouxianValidation(touchPos.y);
            }
            rd.lastPos = touchPos;
        }
        else
        {
            rd.untouchTime += Time.deltaTime;
            if (rd.untouchTime > 1)
            {
                rd.startTime = Time.time;
                rd.untouchTime = 0;
            }
        }

        if (Timeline.ins.hasMusic && !ins.harpImageOut)
        {
            bool qualityIncreasing = true;
            var lastScoreRecord = allScores[allScores.Count - 1];
            if (allScores.Count > 1) qualityIncreasing = lastScoreRecord.score == 2;
            bool volumeIncreasing = Time.time >= lastScoreRecord.time && Time.time - lastScoreRecord.time < 2f;
            quality = Mathf.Clamp(quality + (qualityIncreasing ? 1 : -0.1f) * Time.deltaTime, 0, 1);
            volume = Mathf.Clamp(volume + (volumeIncreasing ? 1 : -1) * Time.deltaTime * 2, 0, 1);
            Timeline.SetParam("Quality", 1 - quality);
            Timeline.SetParam("SingerOn", volume);
            //print("Quality: " + quality + "(" + (qualityIncreasing ? "increasing" : "decreasing") + ") SingerOn: " + volume + "(" + (volumeIncreasing ? "increasing" : "decreasing") + ")");
        }

        if (lyricsState == LyricsState.Parsing)
        {
            if (lyricsAlpha < 1)
            {
                for (int i = 0; i < lyricsTexts.Length; ++i) lyricsTexts[i].color = new Color(0.5f, 0.5f, 0.5f, lyricsAlpha);
                lyricsAlpha += Time.deltaTime * 2;
            }

            if (lyricsTime >= lyricsTimeStep)
            {
                if (curlc >= lyricsAnimTexts.Length)
                {
                    if (lyricsTime >= lyricsTimeStep + lyricsAnimTime)
                    {
                        lyricsState = LyricsState.FadingOut;
                        for (int i = 0; i < lyricsTexts.Length; ++i) if (lyricsAnimTexts[i]) Destroy(lyricsAnimTexts[i].gameObject);
                        lyricsAlpha = 1f;
                    }
                }
                else
                {
                    if (lastScorePos != null && volume > 0.1f)
                    {
                        lyricsAnimTexts[curlc] = Instantiate(lyricsTexts[curlc].gameObject, transform).GetComponent<Text>();
                        lyricsAnimTexts[curlc].color = new Color(1, 0, 0.75f);
                        lyricsCharStartTimes[curlc] = Time.time;
                        lyricsStartPositions[curlc] = lastScorePos.Value - rt.anchoredPosition;
                    }
                    ++curlc;
                    lyricsTime = 0;
                }
            }
            lyricsTime += Time.deltaTime;

            for (int i = 0; i < lyricsCharStartTimes.Length; ++i)
            {
                if (lyricsAnimTexts[i])
                {
                    float frac = (Time.time - lyricsCharStartTimes[i]) / lyricsAnimTime;
                    if (frac >= 1)
                    {
                        lyricsAnimTexts[i].rectTransform.anchoredPosition = lyricsTexts[i].rectTransform.anchoredPosition + new Vector2(Random.Range(0, 10), Random.Range(0, 10));
                    }
                    else
                    {
                        lyricsAnimTexts[i].rectTransform.anchoredPosition = Vector3.Lerp(lyricsStartPositions[i], lyricsTexts[i].rectTransform.anchoredPosition, frac);
                    }
                }
            }
        }
        else if (lyricsState == LyricsState.FadingOut)
        {
            lyricsAlpha -= Time.deltaTime * 2;
            if (lyricsAlpha >= 0)
            {
                for (int i = 0; i < lyricsTexts.Length; ++i)
                {
                    Color c = lyricsTexts[i].color;
                    c.a = lyricsAlpha;
                    lyricsTexts[i].color = c;
                }
            }
            else
            {
                for (int i = 0; i < lyricsTexts.Length; ++i) Destroy(lyricsTexts[i].gameObject);
                lyricsState = LyricsState.None;
            }
        }
    }
    void RouxianValidation(float scoreHeight)
    {
        if (rd.touchCount >= 4)
        {
            int score = 2;
            if (roufa == 0) // 深揉
            {
                if (rd.leftOrRightMax < 0.5f) --score;
            }
            else if (roufa == 1) // 浅揉
            {
                if (rd.leftOrRightMax > 0.5f) --score;
            }

            float time = Time.time - rd.startTime;
            float altime = Time.time - rd.cooldownStartTime;
            if (rousu == 0) // 快揉
            {
                if (time > limitTime) --score;
                if (altime >= cooldown)
                {
                    Score(score, new Vector2(score_x, scoreHeight));
                    rd.cooldownStartTime = Time.time;
                }
            }
            else if (rousu == 1) // 慢揉
            {
                if (time < limitTime) --score;
                Score(score, new Vector2(score_x, scoreHeight));
            }

            rd.leftOrRightMax = 0;
            rd.startTime = Time.time;
        }
        rd.touchCount = 0;
    }
    IEnumerator Interacting()
    {
        interacting = true;
        yield return new WaitForSeconds(timeLast - 1);
        print("time " + timeLast + " passed");
        harpImageOut = true;
        startTime = Time.time;
        interacting = false;
    }

    public static bool Contains(KeyData keyData)
    {
        if (!ins) return false;
        if (!ins.interacting) return false;
        int exit = int.Parse(keyData.prop["Exit"]);
        return exit >= ins.exit && exit < ins.exit + ins.width;
    }

    public static bool CreateLyrics(string lyrics, float lyricsTimeLast)
    {
        if (!ins) return false;
        if (ins.lyricsState != LyricsState.None) return false;
        ins.lyricsTexts = new Text[lyrics.Length];
        float step = (ins.touchField.width - BlockSize.x / 2f) / lyrics.Length;
        for (int i = 0; i < lyrics.Length; ++i)
        {
            var c = Instantiate(Resources.Load<GameObject>("LyricsChar"), ins.transform).GetComponent<Text>();
            c.rectTransform.anchoredPosition = new Vector2(i * step, ins.touchField.height + BlockSize.y);
            c.text = lyrics[i].ToString();
            c.color = new Color(0.5f, 0.5f, 0.5f, 0);
            ins.lyricsTexts[i] = c;
        }
        ins.lyricsTime = ins.lyricsTimeStep = lyricsTimeLast / lyrics.Length;
        ins.lyricsAnimTexts = new Text[lyrics.Length];
        ins.lyricsCharStartTimes = new float[lyrics.Length];
        ins.lyricsStartPositions = new Vector2[lyrics.Length];
        ins.lyricsAlpha = 0;
        ins.curlc = 0;
        ins.lyricsState = LyricsState.Parsing;
        return true;
    }
    protected override void CheckActivateCondition()
    {
        throw new System.NotImplementedException();
    }

    protected override void Update_Activated()
    {
        throw new System.NotImplementedException();
    }
}
