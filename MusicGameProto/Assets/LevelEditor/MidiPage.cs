using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MidiPage : MonoBehaviour
{
    public static MidiPage ins;
    public static Color[] trackColors = new Color[]
    {
        new Color(1,0,0), new Color(1,1,0), new Color(0,1,0), new Color(0,1,1), new Color(0,0,1), new Color(1,0,1),
        new Color(1,0.5f,0), new Color(0.5f,1,0), new Color(0,1,0.5f), new Color(0,0.5f,1), new Color(0.5f,0,1), new Color(1,0,0.5f)
    };
    public static float lengthPerSec = 100;
    public static float heightStep = 15;
    [SerializeField] Text trackTitle;
    [SerializeField] Text midiSelectInfo;
    [SerializeField] Text midiInfo;
    [SerializeField] MidiOperation midiOperationPanel;
    [Header("Prefab")]
    [SerializeField] GameObject trackToggle;
    [SerializeField] GameObject noteBar;
    [SerializeField] GameObject play;
    public static GameObject[] trackOfbars;
    Transform pageNode;
    ScrollRect scroll;
    RectTransform contentNode;
    Vector2 trackToggleStart = new Vector2(-815, 400);
    Vector2 trackToggleSpaces = new Vector2(240, -50);
    GameObject midiRuler;
    GameObject timeRuler;
    public enum MidiPageState
    {
        None,
        Playback
    }

    public static float ContentWidth;
    public static float ContentHeight;
    public static float ContentStart;

    float playbackPos;
    RectTransform playbackTick;
    MidiPageState state;
    float startTime;
    float selectStartTime;
    float endTime;
    float absoluteStartTime;
    List<NoteBar> allNotes;
    public HashSet<NoteBar> selected;
    int curNote;
    GameObject playButton;
    GameObject center;

    private void Awake()
    {
        ins = this;
    }
    private void Start()
    {
        int idx = (int)EditingPage.Midi;
        pageNode = Instantiate(MusicalLevelEditor.ins.node, MusicalLevelEditor.ins.subPages[idx].transform).transform;
        pageNode.name = "Instantiates";
        scroll = MusicalLevelEditor.ins.scrolls[idx];
        contentNode = MusicalLevelEditor.ins.contents[idx];
    }
    private void Update()
    {
        if (state == MidiPageState.Playback)
        {
            playbackPos = Time.time - absoluteStartTime + startTime;
            if (playbackPos >= endTime)
            {
                state = MidiPageState.None;
                playbackPos = startTime = selectStartTime;
                curNote = FindNote(startTime);
                playbackTick.anchoredPosition = new Vector2(ContentStart + startTime * lengthPerSec, 0);
                playButton.GetComponentInChildren<Text>().text = "播放";
            }
            else
            {
                playbackTick.anchoredPosition = new Vector2(ContentStart + playbackPos * lengthPerSec, 0);
                if (playbackPos >= allNotes[curNote].note.startTimeInSec)
                {
                    if (allNotes[curNote].isActiveAndEnabled) allNotes[curNote].PlaySound();
                    ++curNote;
                }
            }
        }

        // debug
        if (center)
        {
            MusicalLevelEditor.ins.mousePositionInScroll[0] = (center.transform as RectTransform).anchoredPosition = Utils.ScreenToCanvasPos(Input.mousePosition) + GetScrollLocalCenter() - (scroll.transform as RectTransform).anchoredPosition;
        }

        // control
        if (Input.GetMouseButtonDown(1)) // mouse right click
        {
            DeselectAll();
        }
    }

    int FindNote(float time) => FindNote(0, allNotes.Count - 1, time);
    int FindNote(int st, int ed, float time)
    {
        if (ed - st < 16)
        {
            for (int i = st; i <= ed; ++i)
                if (allNotes[i].note.startTimeInSec >= time)
                    return i;
            return ed;
        }
        int mid = (st + ed) / 2;
        if (time > allNotes[mid].note.startTimeInSec) return FindNote(mid + 1, ed, time);
        else if (time < allNotes[mid].note.startTimeInSec) return FindNote(st, mid - 1, time);
        else return mid;
    }


    public void LoadMidi(string filename)
    {
        // MidiPage setup
        MidiTranslator.filename = filename;
        MidiTranslator.MakeNotes();
        MidiTranslator.PrepareAllTracks();
        trackTitle.text = "音轨数：" + MidiTranslator.tracks.Length;
        trackOfbars = new GameObject[MidiTranslator.tracks.Length];
        for (int i = 0; i < MidiTranslator.tracks.Length; ++i)
        {
            var t = Instantiate(trackToggle, pageNode);
            (t.transform as RectTransform).anchoredPosition = trackToggleStart + new Vector2(i % 8 * trackToggleSpaces.x, i / 8 * trackToggleSpaces.y);
            t.GetComponent<TrackToggle>().trackNum = i;
            Text text = t.GetComponentInChildren<Text>();
            text.text = i + ". " + MidiTranslator.trackNames[i];
            text.color = trackColors[i];
            trackOfbars[i] = Instantiate(MusicalLevelEditor.ins.node, contentNode);
        }

        // set size of ScrollRect
        float contentTopEdge = ((MidiTranslator.tracks.Length - 1) / 8 + 1) * trackToggleSpaces.y + 20 + trackToggleStart.y;
        float contentBottomEdge = -1080 / 2 + 10;
        scroll.onValueChanged.AddListener(OnMidiPageDragged);
        RectTransform scrollRT = scroll.transform as RectTransform;
        scrollRT.sizeDelta = new Vector2(1900, contentTopEdge - contentBottomEdge);
        scrollRT.anchoredPosition = new Vector2(0, (contentTopEdge + contentBottomEdge) / 2f);

        // Play Button
        playButton = Instantiate(play, pageNode);
        playButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, contentTopEdge - 35);
        playButton.GetComponent<Button>().onClick.AddListener(OnPlayButtonClicked);
        center = Instantiate(MusicalLevelEditor.ins.barImage, contentNode);
        center.name = "DEBUG";
        center.GetComponent<Image>().raycastTarget = false;
        (center.transform as RectTransform).sizeDelta = new Vector2(5, 5);

        // Select Pad
        MusicalLevelEditor.ins.selectPads[0] = Instantiate(MusicalLevelEditor.ins.timeSelectPad, pageNode).GetComponent<SelectPad>();
        MusicalLevelEditor.ins.selectPads[0].page = EditingPage.Midi;
        MusicalLevelEditor.ins.selectPads[0].GetComponent<RectTransform>().anchoredPosition = new Vector2(700, contentTopEdge - 35);

        // ScrollRect Content Setup
        ContentHeight = 128 * heightStep + 70;
        ContentWidth = 1900 + (MidiTranslator.endTime + 60) * lengthPerSec;
        ContentStart = -ContentWidth / 2 + 80;
        startTime = 0;
        endTime = MidiTranslator.endTime;
        float contentBottomStart = -ContentHeight / 2f + 50;
        contentNode.sizeDelta = new Vector2(ContentWidth - scrollRT.sizeDelta.x, ContentHeight);

        // 竖轴 - 音符标尺
        midiRuler = Instantiate(MusicalLevelEditor.ins.node, contentNode);
        midiRuler.name = "MidiRuler";
        (midiRuler.transform as RectTransform).anchoredPosition = new Vector2(ContentStart, 0);
        for (int i = 0; i < 128; ++i)
        {
            var to = Instantiate(MusicalLevelEditor.ins.textObj, midiRuler.transform);
            to.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, contentBottomStart + i * heightStep);
            to.GetComponent<Text>().text = i + "-";
        }
        var to2 = Instantiate(MusicalLevelEditor.ins.textObj, midiRuler.transform);
        to2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, contentBottomStart + 128 * heightStep);
        to2.GetComponent<Text>().text = "音符";
        var line = Instantiate(MusicalLevelEditor.ins.barImage, midiRuler.transform);
        line.GetComponent<Image>().color = Color.black;
        var rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, ContentHeight);

        // 横轴 - 时间标尺
        timeRuler = Instantiate(MusicalLevelEditor.ins.node, contentNode);
        timeRuler.name = "TimeRuler";
        (timeRuler.transform as RectTransform).anchoredPosition = new Vector2(0, contentBottomStart - 20);
        line = Instantiate(MusicalLevelEditor.ins.barImage, timeRuler.transform);
        line.GetComponent<Image>().color = Color.black;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ContentWidth, 2);
        for (int i = 0; i * lengthPerSec < ContentWidth; ++i)
        {
            float pos_x = ContentStart + i * lengthPerSec;
            line = Instantiate(MusicalLevelEditor.ins.barImage, contentNode);
            line.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, ContentHeight);
            rt.anchoredPosition = new Vector2(pos_x, 0);
            var to = Instantiate(MusicalLevelEditor.ins.textObj2, timeRuler.transform);
            to.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos_x, -8);
            to.GetComponent<Text>().text = "^\n" + i;
        }
        // 时间标尺交互
        line = Instantiate(MusicalLevelEditor.ins.rulerInteractable, timeRuler.transform);
        line.GetComponent<RulerClickable>().page = EditingPage.Midi;
        line.GetComponent<Image>().color = new Color(1, 0, 0, 0.5f);
        line.name = "Time Ruler Interactable";
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ContentWidth, 40);
        rt.anchoredPosition = new Vector2(0, -20);

        // 终点线
        line = Instantiate(MusicalLevelEditor.ins.barImage, contentNode);
        line.GetComponent<Image>().color = Color.red;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, ContentHeight);
        rt.anchoredPosition = new Vector2(ContentStart + MidiTranslator.endTime * lengthPerSec, 0);

        allNotes = new List<NoteBar>();
        selected = new HashSet<NoteBar>();
        for (int i = 0; i < MidiTranslator.tracks.Length; ++i)
        {
            for (int j = 0; j < MidiTranslator.tracks[i].Count; ++j)
            {
                Note n = MidiTranslator.tracks[i][j];
                var bar = Instantiate(noteBar, trackOfbars[i].transform);
                var notebar = bar.GetComponent<NoteBar>();
                notebar.note = n;
                bar.GetComponent<Image>().color = trackColors[n.track];
                rt = bar.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(ContentStart + n.startTimeInSec * lengthPerSec, contentBottomStart + n.note * heightStep);
                rt.sizeDelta = new Vector2(n.lengthInSec * lengthPerSec, heightStep);
                allNotes.Add(notebar);
            }
        }
        allNotes.Sort((x, y) => x.note.startTimeInSec.CompareTo(y.note.startTimeInSec));
        for (int i = 0; i < allNotes.Count; ++i) allNotes[i].num = i;

        // 播放指针
        line = Instantiate(MusicalLevelEditor.ins.barImage, contentNode);
        line.GetComponent<Image>().color = Color.black;
        rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2, ContentHeight);
        rt.anchoredPosition = new Vector2(ContentStart, 0);
        playbackPos = 0;
        playbackTick = rt;
        RefreshInfoPanel();
    }
    void RefreshInfoPanel()
    {
        if (selected.Count == 0)
        {
            midiSelectInfo.text = "已选择" + selected.Count + "个音符";
            (midiSelectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, -500);
            midiInfo.transform.parent.gameObject.SetActive(false);
            midiOperationPanel.gameObject.SetActive(false);
        }
        else if (selected.Count == 1)
        {
            midiSelectInfo.text = "已选择" + selected.Count + "个音符";
            (midiSelectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, 16);
            midiInfo.transform.parent.gameObject.SetActive(true);
            midiOperationPanel.gameObject.SetActive(true);
            Note n = null;
            int num = -1;
            foreach (var nb in selected)
            {
                n = nb.note;
                num = nb.num;
                break;
            }
            string info = "Midi信息(" + num + ")";
            info += "\n音符值：" + n.note;
            info += "\n开始时间：" + n.startTimeInSec;
            info += "\n持续时间：" + n.lengthInSec;
            info += "\n结束时间：" + (n.startTimeInSec + n.lengthInSec);
            info += "\n音轨：";
            info += n.track + "(" + MidiTranslator.trackNames[n.track];
            info += ")\n入键速度：" + n.InitialVelocity;
            info += "\n出键速度：" + n.FinalVelocity;
            info += "\n\n内嵌信息\n";
            ins.midiInfo.text = info;
        }
        else
        {
            midiSelectInfo.text = "已选择" + selected.Count + "个音符";
            (midiSelectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, -242);
            midiInfo.transform.parent.gameObject.SetActive(false);
            midiOperationPanel.gameObject.SetActive(true);
        }
    }
    public static void SelectNote(NoteBar nb, bool adding)
    {
        if (!adding) DeselectAll();
        ins.selected.Add(nb);
        ins.RefreshInfoPanel();
    }
    public static void DeselectNote(NoteBar nb)
    {
        ins.selected.Remove(nb);
        ins.RefreshInfoPanel();
    }
    public static void DeselectAll()
    {
        if (ins.selected.Count == 0) return;
        List<NoteBar> l = new List<NoteBar>();
        foreach (var nb in ins.selected) l.Add(nb);
        ins.selected.Clear();
        foreach (var nb in l) nb.RefreshSelectedState();
        ins.RefreshInfoPanel();
    }
    public static void SelectRange(float startTime, float endTime)
    {
        DeselectAll();
        int stIdx = ins.FindNote(startTime);
        int edIdx = ins.FindNote(endTime);
        ins.curNote = stIdx;
        ins.selectStartTime = ins.startTime = startTime;
        ins.endTime = endTime;
        for (int i = stIdx; i <= edIdx; ++i)
        {
            if (ins.allNotes[i].gameObject.activeInHierarchy && ins.allNotes[i].note.startTimeInSec > startTime && ins.allNotes[i].note.startTimeInSec <= endTime)
            {
                ins.selected.Add(ins.allNotes[i]);
                ins.allNotes[i].RefreshSelectedState();
            }
        }
        ins.RefreshInfoPanel();
    }

    public static bool IsSelected(NoteBar nb) => ins.selected.Contains(nb);
    void OnPlayButtonClicked()
    {
        if (state == MidiPageState.Playback)
        {
            state = MidiPageState.None;
            startTime = playbackPos;
            playButton.GetComponentInChildren<Text>().text = "播放";
        }
        else if (state == MidiPageState.None)
        {
            absoluteStartTime = Time.time;
            state = MidiPageState.Playback;
            playButton.GetComponentInChildren<Text>().text = "暂停";
        }
    }
    void OnMidiPageDragged(Vector2 center)
    {
        Vector2 scrollSize = scroll.GetComponent<RectTransform>().sizeDelta;
        float x = (-0.5f + center.x) * (ContentWidth - scrollSize.x) - scrollSize.x / 2 + 80;
        float y = (-0.5f + center.y) * (ContentHeight - scrollSize.y) - scrollSize.y / 2 + 50;
        (midiRuler.transform as RectTransform).anchoredPosition = new Vector2(x, 0);
        (timeRuler.transform as RectTransform).anchoredPosition = new Vector2(0, y);
    }
    Vector2 GetScrollLocalCenter()
    {
        Vector2 center = scroll.normalizedPosition;
        Vector2 scrollSize = scroll.GetComponent<RectTransform>().sizeDelta;
        return new Vector2((-0.5f + center.x) * (ContentWidth - scrollSize.x), (-0.5f + center.y) * (ContentHeight - scrollSize.y));
    }
}
