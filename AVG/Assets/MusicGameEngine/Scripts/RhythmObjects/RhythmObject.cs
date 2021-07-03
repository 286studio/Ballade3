using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void Void_0Arg();
public enum RhythmType
{
    None,
    FallingBlock, // 点按
    LongFallingBlock, // 长按
    HorizontalMove, // 单次左右滑
    Rouxian,
    Misc,
}

public struct SoundStruct
{
    public string id;
    public float delay;
    public bool played { private set; get; }
    public float createTime;
    public void Play(int score)
    {
        if (GeneralSettings.midiTrack)
        {
            if (score == 1)
            {
                FMODUnity.RuntimeManager.PlayOneShot("event:/" + id);
                Timeline.SetParam("KeyOn", 0);
            }
            else if (score == 2)
            {
                Timeline.SetParam("KeyOn", 1);
            }
            else
            {
                Timeline.SetParam("KeyOn", 0);
                FMODUnity.RuntimeManager.PlayOneShot("event:/WRONG");
            }
        }
        else
        {
            if (score > 0)
            {
                FMODUnity.RuntimeManager.PlayOneShot("event:/" + id);
            }
            else
            {
                FMODUnity.RuntimeManager.PlayOneShot("event:/WRONG");
            }
        }
        played = true;
    }
}

public abstract class RhythmObject : MonoBehaviour
{
    public bool noUpdate;
    [HideInInspector] public int perfectScore = 20;
    [HideInInspector] public int goodScore = 10;
    [HideInInspector] public int badScore = 0;
    [HideInInspector] public int exit;
    [HideInInspector] public PanelType panel; // 0=left, 1=right

    [HideInInspector] public SoundStruct[] sound = new SoundStruct[0];

    protected Sprite[] noteImages;

    [SerializeField] Graphic[] coloringParts;
    public RhythmObject parent = null;
    protected ExitData[] exits;
    protected int exitCount;
    protected bool activated;
    protected bool fallBelowBottom = true;
    public float fallingTime = 3;
    public RectTransform rt;
    protected Vector2 start, end;

    public event Void_0Arg OnBottomReached;
    public event Void_Float OnFallingFracUpdated;
    public event Void_Int OnScored;

    protected float createTime;
    protected Vector2? lastScorePos = null;
    protected bool noAnim;

    int curNote = 1;

    bool destroyPending;

    protected bool autoMode => RhythmGameManager.ins.autoMode;

    protected class ScoreRecord
    {
        public float time;
        public int score; // 2=Perfect, 1=good, 0=miss
        public ScoreRecord(float scoringTime, int score)
        {
            time = scoringTime;
            this.score = score;
        }
    }
    protected List<ScoreRecord> allScores = new List<ScoreRecord>();

    protected virtual void Start()
    {
        Vector2 size = rt.sizeDelta;
        size.x = BlockSize.x;
        size.y = BlockSize.y;
        rt.sizeDelta = size;

        createTime = Time.time;
        for(int i = 0; i < sound.Length; ++i) sound[i].createTime = createTime;
    }
    protected virtual void Update()
    {
        if (noUpdate) return;
        Update_Falling();
        if (destroyPending)
            Update_DestroyPending(); // 检查此对象是否准备被删除
        else
        {
            if (!activated)
                CheckActivateCondition();
            else
                Update_Activated();
        }
    }
    public virtual RhythmObject Initialize(int _exit, PanelType _panel, Color? c = null, int _perfectScore = 20, int _goodScore = 10, int _badScore = 0)
    {
        if (c != null) foreach (Graphic g in coloringParts) g.color = c.Value;
        rt = transform as RectTransform;
        exit = _exit;
        panel = _panel;
        exits = RhythmGameManager.exits;
        exitCount = GeneralSettings.exitCount;
        perfectScore = _perfectScore;
        goodScore = _goodScore;
        badScore = _badScore;
        transform.position = GetExit().obj.transform.position;
        end = start = (transform as RectTransform).anchoredPosition;
        end.x = GetBottom() + (panel == PanelType.Left ? -BlockSize.x : BlockSize.x) / 2f; // 延迟声效
        return this;
    }
    public virtual RhythmObject Initialize_LevelEditor(int _exit, PanelType _panel, Color? c = null)
    {
        if (c != null) foreach (Graphic g in coloringParts) g.color = c.Value;
        rt = transform as RectTransform;
        exit = _exit;
        panel = _panel;
        exits = LevelPage.exits;
        exitCount = 3;
        return this;
    }
    protected virtual void Activate()
    {
        if (!GetExit().current && !activated)
        {
            activated = true;
            GetExit().current = this;
        }
    }
    protected void Deactivate()
    {
        if (GetExit().current == this)
        {
            foreach (Graphic g in GetComponentsInChildren<Graphic>()) g.enabled = false;
            GetExit().current = null;
        }
    }
    public abstract RhythmType Type { get; }

    protected virtual void CheckActivateCondition()
    {
        if (panel == PanelType.Left)
        {
            if (rt.anchoredPosition.x < GetBottom() + 1.5 * BlockSize.x)
            {
                Activate();
            }
        }
        else
        {
            if (rt.anchoredPosition.x > GetBottom() - 1.5 * BlockSize.x)
            {
                Activate();
            }
        }
    }

    bool bottomReached;
    protected void Update_Falling()
    {
        float frac = (Time.time - createTime) / fallingTime;
        if (frac >= 1 && !bottomReached)
        {
            // 到达底部
            if (sound.Length > 0)
            {
                if (allScores.Count > 0 && allScores[0].score == 2)
                {
                    // 如果已经按过Perfect，则声音再正确的时间播放
                    sound[0].Play(2);
                }
            }
            OnBottomReached?.Invoke();
            bottomReached = true;
        }
        if (!fallBelowBottom) if (frac > 1) frac = 1;
        start.x = GetExit().obj.GetComponent<RectTransform>().anchoredPosition.x;
        rt.anchoredPosition = Utils.LerpWithoutClamp(start, end, frac);
        OnFallingFracUpdated?.Invoke(frac);

        // 单键对应多音符处理
        if (bottomReached && allScores.Count > 0 && curNote < sound.Length)
        {
            // print(Time.time - createTime - fallingTime);
            if (!sound[curNote].played && Time.time - createTime - fallingTime >= sound[curNote].delay)
            {
                // 时间已到
                sound[curNote].Play(allScores[allScores.Count - 1].score);
                ++curNote;
            }
        }
    }
    protected abstract void Update_Activated();
    protected void Update_DestroyPending()
    {
        if (sound.Length > 0)
        {
            if (Time.time - createTime - fallingTime >= sound[sound.Length - 1].delay)
                Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    protected virtual void Score(int s, Vector2? pos = null, bool flashBottom = true, int sndIdx = 0)
    {
        int _score;
        string _text;
        Color _color;
        switch (s)
        {
            default:
                _score = badScore;
                _text = "Miss";
                _color = Color.grey;
                ++Scoring.missCount;
                break;
            case 1:
                _score = goodScore;
                _text = "Good";
                _color = Color.red;//green
                ++Scoring.goodCount;
                break;
            case 2:
                _score = perfectScore;
                _text = "Perfect";
                _color = Color.yellow;
                ++Scoring.perfectCount;
                break;
        }
        RhythmGameManager.UpdateScore(_score);
        lastScorePos = pos == null ? GetExit().center : pos.Value;
        if (coloringParts.Length > 0 && !noAnim && s >= 1) BlockEnlarge.Create(noteImages[sndIdx], _color, lastScorePos.Value, lastScorePos.Value + new Vector2(panel == PanelType.Left ? BlockSize.x : -BlockSize.x, BlockSize.y), rt.parent);
        
        if (s >= 1)
        {
            if (sound.Length > 0 && allScores.Count == 0 && s == 1) sound[0].Play(1); // 按到good时声效不会按时播放
            if (flashBottom && coloringParts.Length > 0) Bottom.SetColor(panel, coloringParts[0].color * 0.75f); // 底边变色
        }
        else
        {
            if (sound.Length > 0 && allScores.Count == 0) sound[0].Play(0); // 按到miss时声效不会按时播放
            FlyingText.Create(_text, _color, lastScorePos.Value, rt.parent);
        }
        allScores.Add(new ScoreRecord(Time.time - createTime, s));
        OnScored?.Invoke(s);
    }

    public bool TouchedByBinggui()
    {
        Debug.LogError("Tuogui is Deprecated");
        return false;
    }

    public static void DestroyRhythmObject(RhythmObject ro)
    {
        ro.destroyPending = true;
    }

    protected float GetBottom()
    {
        return RhythmGameManager.GetBottom(panel);
    }

    protected ExitData GetExit(int offset = 0)
    {
        return exits[exit + offset + (int)panel * exitCount];
    }

    protected struct AutoModeStruct
    {
        public float time;
    }
    protected AutoModeStruct ams;
}
