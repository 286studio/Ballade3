using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelPage : MonoBehaviour
{
    public static string SavePath { get => "Assets/MusicalLevels/Resources/" + Interpreter.platform + "/" + MidiTranslator.filename; }
    public static LevelPage ins;
    public static float lengthPerSec = PanelSize.x * 2 / 3;
    public static float ContentWidth;
    public static float ContentHeight = 410;
    public static float ContentStart;
    public static ExitData[] exits;
    public static Color[] metaEventColor = new Color[7]
    {
        new Color(0, 0, 0, 1),
        new Color(0, 0, 1, 0.5f),
        new Color(0, 0, 1, 0.5f),
        new Color(0, 0, 1, 0.5f),
        new Color(0, 0, 1, 0.5f),
        new Color(0, 0, 1, 0.5f),
        new Color(1, 0, 0, 1),
    };
    public static string[] chineseMetaEvent = new string[7]
    {
        "通用", "开启左键盘", "隐藏左键盘", "开启右键盘", "隐藏右键盘", "隐藏左右键盘", "游戏结束"
    };
    public static float[] metaEventLengths = new float[7]
    {
        0, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0f
    };
    public static bool refreshPending;
    [SerializeField] InputField ExitCount;
    [SerializeField] InputField MusicEvent;
    [SerializeField] InputField GameMode;
    [SerializeField] InputField Delay;
    [SerializeField] InputField MusicStartPosition;
    [SerializeField] Toggle MidiTrack;
    [SerializeField] Button enterGame;
    [SerializeField] Button refresh;
    [SerializeField] Button save;
    [SerializeField] Button openTXT;
    [SerializeField] Toggle reverseRight;
    [SerializeField] Text selectInfo;
    [SerializeField] EventOperation eventOperationPanel;
    [SerializeField] MultiSelectPanel multiSelectPanel;
    [SerializeField] Button showAll;
    [Header("Prefabs")]
    public GameObject metaEvent;
    public GameObject check;
    public GameObject rightClickMenuPrefab;
    [HideInInspector] public List<KeyData> keyData;
    Dictionary<string, Dictionary<string, string>> sections;
    RectTransform[] nodes;
    RectTransform[] metaEventNodes;
    public RectTransform[] midiEventNodes;
    RectTransform contentL, contentR;
    ScrollRect left, right;
    public HashSet<EditorEvent> leftEvents = new HashSet<EditorEvent>();
    public HashSet<EditorEvent> rightEvents = new HashSet<EditorEvent>();
    HashSet<EditorEvent> selected = new HashSet<EditorEvent>();
    public HashSet<EditorEvent> hidden = new HashSet<EditorEvent>();
    GameObject mouseIdicatorLeft, mouseIdicatorRight;
    RightClickMenu rightClickMenu;
    private void Awake()
    {
        ins = this;
    }
    private void Start()
    {
        ExitCount.onValueChanged.AddListener(x => sections[SectionNames.General][GeneralTags.Exit] = x);
        MusicEvent.onValueChanged.AddListener(x => sections[SectionNames.General][GeneralTags.Music] = x);
        GameMode.onValueChanged.AddListener(x => sections[SectionNames.General][GeneralTags.GameMode] = x);
        Delay.onValueChanged.AddListener(x => sections[SectionNames.General][GeneralTags.Delay] = x);
        MusicStartPosition.onValueChanged.AddListener(x => sections[SectionNames.General][GeneralTags.MusicStartPosition] = x);
        MidiTrack.onValueChanged.AddListener(b => sections["General"][GeneralTags.MidiTrack] = b ? "yes" : "no");
        enterGame.onClick.AddListener(delegate
        {
            SelectLevel.ins.preselectedSongName = MidiTranslator.filename;
            SceneManager.LoadScene(1);
        });
        refresh.onClick.AddListener(Refresh);
        save.onClick.AddListener(SaveToFile);
        contentL = MusicalLevelEditor.ins.contents[1];
        contentR = MusicalLevelEditor.ins.contents[2];
        left = MusicalLevelEditor.ins.scrolls[1];
        right = MusicalLevelEditor.ins.scrolls[2];
        left.onValueChanged.AddListener(OnLevelPageLeftDragged);
        right.onValueChanged.AddListener(OnLevelPageRightDragged);
        reverseRight.isOn = false;
        reverseRight.onValueChanged.AddListener(ReverseRight);
        showAll.onClick.AddListener(ShowAllHiddenEvent);
        openTXT.onClick.AddListener(() =>
        {
            EditorUtility.RevealInFinder(Application.dataPath + "/MusicalLevels/Resources/" + Interpreter.platform + "/" + MidiTranslator.filename + "/00.txt");
        });
    }

    private void Update()
    {
        // debug
        if (mouseIdicatorLeft)
            MusicalLevelEditor.ins.mousePositionInScroll[1] = (mouseIdicatorLeft.transform as RectTransform).anchoredPosition = Utils.ScreenToCanvasPos(Input.mousePosition) + GetLeftScrollLocalCenter() - (left.transform as RectTransform).anchoredPosition;
        if (mouseIdicatorRight)
            MusicalLevelEditor.ins.mousePositionInScroll[2] = (mouseIdicatorRight.transform as RectTransform).anchoredPosition = Utils.ScreenToCanvasPos(Input.mousePosition) + GetRightScrollLocalCenter() - (right.transform as RectTransform).anchoredPosition;

        // control
        if (Input.GetMouseButtonDown(1)) // mouse right click
        {
            if (selected.Count > 0)
            {
                DeselectAll();
            }
            else
            {
                if (rightClickMenu) Destroy(rightClickMenu.gameObject);
                rightClickMenu = Instantiate(rightClickMenuPrefab, transform).GetComponent<RightClickMenu>();
                var pos = Utils.ScreenToCanvasPos(Input.mousePosition);
                rightClickMenu.rt.anchoredPosition = new Vector2(Mathf.Clamp(pos.x, -DefRes.x / 2, DefRes.x / 2 - rightClickMenu.rt.sizeDelta.x), Mathf.Clamp(pos.y, -DefRes.y / 2 + rightClickMenu.rt.sizeDelta.y, DefRes.y / 2));
            }
        }
    }
    public void Setup()
    {
        string path = Application.dataPath + "/MusicalLevels/Resources/" + Interpreter.platform + "/" + MidiTranslator.filename;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            File.WriteAllText(path + "/00.txt", "[" + SectionNames.General + "]\n" + GeneralTags.Exit + "=3\n");
            AssetDatabase.Refresh();
        }
        Interpreter.Open(MidiTranslator.filename, out keyData, out sections);
        leftEvents.Clear();
        rightEvents.Clear();
        string str;
        ExitCount.text = sections["General"]["Exit"];
        if (sections["General"].TryGetValue("Music", out str)) MusicEvent.text = str; else MusicEvent.text = MidiTranslator.filename;
        if (sections["General"].TryGetValue("GameMode", out str)) GameMode.text = str; else GameMode.text = "0";
        if (sections["General"].TryGetValue("Delay", out str)) Delay.text = str; else Delay.text = "3";
        if (sections["General"].TryGetValue("MusicStartPosition", out str)) MusicStartPosition.text = str; else MusicStartPosition.text = "0";
        if (sections["General"].TryGetValue("MidiTrack", out str)) MidiTrack.isOn = str == "yes"; else MidiTrack.isOn = false;
        nodes = new RectTransform[2];
        nodes[0] = Instantiate(MusicalLevelEditor.ins.node, contentL).GetComponent<RectTransform>();
        nodes[1] = Instantiate(MusicalLevelEditor.ins.node, contentR).GetComponent<RectTransform>();
        metaEventNodes = new RectTransform[2];
        metaEventNodes[0] = Instantiate(MusicalLevelEditor.ins.node, nodes[0]).GetComponent<RectTransform>();
        metaEventNodes[1] = Instantiate(MusicalLevelEditor.ins.node, nodes[1]).GetComponent<RectTransform>();
        metaEventNodes[0].name = "MetaEvents";
        metaEventNodes[1].name = "MetaEvents";
        midiEventNodes = new RectTransform[2];
        midiEventNodes[0] = Instantiate(MusicalLevelEditor.ins.node, nodes[0]).GetComponent<RectTransform>();
        midiEventNodes[1] = Instantiate(MusicalLevelEditor.ins.node, nodes[1]).GetComponent<RectTransform>();
        midiEventNodes[0].name = "MidiEvents";
        midiEventNodes[1].name = "MidiEvents";

        // find end pos
        GameObject line;
        RectTransform rt;
        ContentWidth = (MidiTranslator.endTime + 3) * lengthPerSec;
        ContentStart = -ContentWidth / 2 + 50;
        contentL.sizeDelta = contentR.sizeDelta = new Vector2(ContentWidth - 1900, ContentHeight);
        // 关卡起点
        exits = new ExitData[6];
        for (int page = 1; page <= 2; ++page)
        {
            line = Instantiate(MusicalLevelEditor.ins.barImage, nodes[page - 1]);
            line.name = "Exits";
            line.GetComponent<Image>().color = new Color32(99, 198, 255, 255);
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(4, ContentHeight);
            rt.anchoredPosition = new Vector2(ContentStart, 0);
            for (int i = 0; i < 3; ++i)
            {
                var e = exits[i + (page - 1) * 3] = new ExitData();
                e.id = i;
                e.panel = (PanelType)(page - 1);
                e.obj = Instantiate(Resources.Load<GameObject>("exit"), line.transform);
                rt = e.obj.transform as RectTransform;
                rt.sizeDelta = new Vector2(BlockSize.x, BlockSize.y);
                rt.anchoredPosition = new Vector2(0, ExitPos(i));
                e.y_top = rt.anchoredPosition.y + rt.sizeDelta.y / 2;
                e.y_bot = rt.anchoredPosition.y - rt.sizeDelta.y / 2;
                e.center = rt.anchoredPosition;
            }

            // 时间标尺
            var ruler = Instantiate(MusicalLevelEditor.ins.node, nodes[page - 1]);
            ruler.name = "TimeRuler";
            (ruler.transform as RectTransform).anchoredPosition = new Vector2(0, -ContentHeight / 2 + 20);
            line = Instantiate(MusicalLevelEditor.ins.barImage, ruler.transform);
            line.GetComponent<Image>().color = Color.black;
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ContentWidth, 2);
            for (int i = 0; i * lengthPerSec < ContentWidth; ++i)
            {
                var to = Instantiate(MusicalLevelEditor.ins.textObj2, ruler.transform);
                to.GetComponent<RectTransform>().anchoredPosition = new Vector2(ContentStart + i * lengthPerSec, -8);
                to.GetComponent<Text>().text = "^\n" + i;
            }
            // 时间标尺交互
            line = Instantiate(MusicalLevelEditor.ins.rulerInteractable, ruler.transform);
            line.GetComponent<RulerClickable>().page = (EditingPage)page;
            line.GetComponent<Image>().color = new Color(1, 0, 0, 0.5f);
            line.name = "Time Ruler Interactable";
            rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ContentWidth, 40);
            rt.anchoredPosition = new Vector2(0, -20);
        }
        // parse all events
        keyData.Sort((x, y) => x.startTime.CompareTo(y.startTime));
        foreach (var kd in keyData) ParseKeyData(kd);

        // Debug
        mouseIdicatorLeft = Instantiate(MusicalLevelEditor.ins.barImage, nodes[0]);
        mouseIdicatorLeft.name = "DEBUG";
        mouseIdicatorLeft.GetComponent<Image>().raycastTarget = false;
        (mouseIdicatorLeft.transform as RectTransform).sizeDelta = new Vector2(5, 5);
        mouseIdicatorRight = Instantiate(MusicalLevelEditor.ins.barImage, nodes[1]);
        mouseIdicatorRight.name = "DEBUG";
        mouseIdicatorRight.GetComponent<Image>().raycastTarget = false;
        (mouseIdicatorRight.transform as RectTransform).sizeDelta = new Vector2(5, 5);

        // Update View
        RefreshInfoPanel();
    }

    public void Refresh()
    {
        refreshPending = false;
        DeselectAll();
        if (nodes[0]) Destroy(nodes[0].gameObject);
        if (nodes[1]) Destroy(nodes[1].gameObject);
        keyData = null;
        sections = null;
        Setup();
    }
    public void SaveToFile()
    {
        Dictionary<string, List<KeyData>> files = new Dictionary<string, List<KeyData>>();
        foreach (var kd in keyData)
        {
            if (kd.deleted) continue;
            List<KeyData> file;
            if (files.TryGetValue(kd.filename, out file))
            {
                file.Add(kd);
            }
            else
            {
                var newFile = new List<KeyData>();
                newFile.Add(kd);
                files[kd.filename] = newFile;
            }
        }
        foreach (var f in files)
        {
            string savePath = SavePath + "/" + f.Key + ".txt";
            string text = "; This file is generate by level editor\n";
            if (f.Key == "00")
            {
                foreach (var section in sections)
                {
                    text += "[" + section.Key + "]\n";
                    foreach (var kv in section.Value) text += kv.Key + "=" + kv.Value + "\n";
                    text += "\n";
                }
            }

            f.Value.Sort((x, y) => x.startTime.CompareTo(y.startTime));
            foreach (var kd in f.Value)
            {
                text += "[" + kd.startTime + "]\n";
                foreach (var kv in kd.prop) text += kv.Key + "=" + kv.Value + "\n";
                text += "\n";
            }
            File.WriteAllText(savePath, text);
        }
        AssetDatabase.Refresh(); // 刷新Unity Editor的缓存
    }
    public float ExitPos(int exitIdx) => (1 - exitIdx) * PanelSize.y * 0.3f;
    void ParseKeyData(KeyData kd)
    {
        int exit = 0;
        PanelType panel = PanelType.Left;
        string str; string[] seg;
        if (kd.prop.TryGetValue("Exit", out str)) exit = int.Parse(str);
        if (kd.prop.TryGetValue("Panel", out str)) if (str == "Right") panel = PanelType.Right;

        string blockType = EventTypes.None;
        if (kd.prop.TryGetValue("Type", out str)) blockType = str;
        if (GeneralSettings.specialMode == 1) blockType = "FallingBlock";
        int idx;
        if (TryGetMetaEventIndex(blockType, out idx))
        {
            MetaEventShow(kd, metaEventColor[idx], chineseMetaEvent[idx], kd.startTime, metaEventLengths[idx]);
        }
        else
        {
            Color c = Utils.GetRandomColor();
            if (kd.prop.TryGetValue("Color", out str))
            {
                seg = str.Split(',');
                c = new Color32(byte.Parse(seg[0]), byte.Parse(seg[1]), byte.Parse(seg[2]), 255);
            }
            RhythmObject ro = Instantiate(Resources.Load<GameObject>(blockType), midiEventNodes[(int)panel]).GetComponent<RhythmObject>().Initialize_LevelEditor(exit, panel, c);
            ro.rt.anchoredPosition = new Vector2(ContentStart + kd.startTime * lengthPerSec, ExitPos(exit));
            ro.noUpdate = true;
            var e = ro.gameObject.AddComponent<EditorEvent>();
            e.eventType = LevelEventType.Note;
            e.kd = kd;
            if (panel == PanelType.Left) leftEvents.Add(e);
            else rightEvents.Add(e);
            switch (blockType)
            {
                case "FallingBlock":
                    break;
                case "LongFallingBlock":
                    LongFallingBlock lfb = (LongFallingBlock)ro;
                    lfb.length = int.Parse(kd.prop["Length"]);
                    break;
                case "HorizontalMove":
                    HorizontalMove hrm = (HorizontalMove)ro;
                    hrm.width = int.Parse(kd.prop["Width"]);
                    hrm.direction = kd.prop["Direction"] == "Up" ? Direction.Up : Direction.Down;
                    break;
            }
        }
    }
    public void MetaEventShow(KeyData kd, Color c, string txt, float st, float l)
    {
        EditorEvent[] e = new EditorEvent[2];
        for (int i = 0; i <= 1; ++i)
        {
            var line = Instantiate(metaEvent, metaEventNodes[i]);
            line.GetComponent<Image>().color = c;
            line.GetComponentInChildren<Text>().text = txt;
            var rt = line.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Mathf.Max(l * lengthPerSec, 4), ContentHeight);
            rt.anchoredPosition = new Vector2(ContentStart + st * lengthPerSec, 0);
            e[i] = line.AddComponent<EditorEvent>();
            e[i].eventType = LevelEventType.Meta;
            e[i].kd = kd;
        }
        e[0].mirror = e[1];
        e[1].mirror = e[0];
        leftEvents.Add(e[0]);
        rightEvents.Add(e[1]);
        eventOperationPanel.ResizeEvent(e[0]);
    }
    public static bool IsSelected(EditorEvent ee) => ins.selected.Contains(ee);
    public static void SelectEvent(EditorEvent e, bool adding)
    {
        if (!adding) DeselectAll();
        ins.selected.Add(e);
        ins.RefreshInfoPanel();
    }
    public static void DeselectEvent(EditorEvent e)
    {
        ins.selected.Remove(e);
        ins.RefreshInfoPanel();
    }
    public static void SelectRangeLeft(float startTime, float endTime)
    {
        DeselectAll();
        foreach (var ee in ins.leftEvents)
            if (ee.gameObject.activeInHierarchy && ee.kd.startTime >= startTime && ee.kd.startTime <= endTime)
            {
                ins.selected.Add(ee);
                ee.RefreshSelectedState();
            }
        ins.RefreshInfoPanel();
    }
    public static void SelectRangeRight(float startTime, float endTime)
    {
        DeselectAll();
        foreach (var ee in ins.rightEvents)
            if (ee.gameObject.activeInHierarchy && ee.kd.startTime >= startTime && ee.kd.startTime <= endTime)
            {
                ins.selected.Add(ee);
                ee.RefreshSelectedState();
            }
        ins.RefreshInfoPanel();
    }
    public static void DeselectAll()
    {
        if (ins.selected.Count == 0) return;
        List<EditorEvent> l = new List<EditorEvent>();
        foreach (var ee in ins.selected) l.Add(ee);
        ins.selected.Clear();
        foreach (var ee in l) ee.RefreshSelectedState();
        ins.RefreshInfoPanel();
    }
    public static void DeleteSelected()
    {
        foreach (var ee in ins.selected)
        {
            ee.kd.deleted = true;
            ins.leftEvents.Remove(ee);
            ins.rightEvents.Remove(ee);
            if (ee.mirror)
            {
                ins.leftEvents.Remove(ee.mirror);
                ins.rightEvents.Remove(ee.mirror);
                Destroy(ee.mirror.gameObject);
            }
            Destroy(ee.gameObject);
        }
        ins.selected.Clear();
        ins.RefreshInfoPanel();
    }
    void RefreshInfoPanel()
    {
        if (selected.Count == 0)
        {
            selectInfo.text = "已选择" + selected.Count + "个事件";
            (selectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, -505);
            eventOperationPanel.gameObject.SetActive(false);
            multiSelectPanel.gameObject.SetActive(false);
        }
        else if (selected.Count == 1)
        {
            selectInfo.text = "已选择" + selected.Count + "个事件";
            (selectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, 13);
            EditorEvent e = null;
            foreach (var ee in selected) e = ee;
            eventOperationPanel.gameObject.SetActive(true);
            eventOperationPanel.Show(e);
            multiSelectPanel.gameObject.SetActive(false);
        }
        else
        {
            selectInfo.text = "已选择" + selected.Count + "个事件";
            (selectInfo.transform.parent as RectTransform).anchoredPosition = new Vector2(785, -447);
            eventOperationPanel.gameObject.SetActive(false);
            multiSelectPanel.gameObject.SetActive(true);
            multiSelectPanel.Refresh();
        }
    }
    void OnLevelPageLeftDragged(Vector2 pos)
    {
        if (!reverseRight.isOn) right.horizontalNormalizedPosition = pos.x;
        else right.horizontalNormalizedPosition = 1 - pos.x;
    }
    void OnLevelPageRightDragged(Vector2 pos)
    {
        if (!reverseRight.isOn) left.horizontalNormalizedPosition = pos.x;
        else left.horizontalNormalizedPosition = 1 - pos.x;
    }
    void ReverseRight(bool b)
    {
        nodes[1].localScale = new Vector3(b ? -1 : 1, 1, 1);
    }
    Vector2 GetLeftScrollLocalCenter()
    {
        Vector2 center = left.normalizedPosition;
        Vector2 scrollSize = left.GetComponent<RectTransform>().sizeDelta;
        return new Vector2((-0.5f + center.x) * (ContentWidth - scrollSize.x), (-0.5f + center.y) * (ContentHeight - scrollSize.y));
    }
    Vector2 GetRightScrollLocalCenter()
    {
        Vector2 center = right.normalizedPosition;
        Vector2 scrollSize = right.GetComponent<RectTransform>().sizeDelta;
        return new Vector2((-0.5f + center.x) * (ContentWidth - scrollSize.x), (-0.5f + center.y) * (ContentHeight - scrollSize.y));
    }
    public static bool OneSelection => ins.selected.Count == 1;
    public static bool SelectionContainsOnlyFallingBlock()
    {
        foreach (var e in ins.selected)
        {
            string str;
            if (e.kd.prop.TryGetValue(EventTags.Type, out str))
            {
                if (str != EventTypes.FallingBlock)
                    return false;
            }
            else return false;
        }
        return true;
    }
    public static bool TryGetMetaEventIndex(string blockTypeName, out int idx)
    {
        for (int i = 0; i < EventTypes.Meta.Length; ++i)
        {
            if (blockTypeName == EventTypes.Meta[i])
            {
                idx = i;
                return true;
            }
        }
        idx = -1;
        return false;
    }

    void ShowAllHiddenEvent()
    {
        foreach (var ee in hidden)
        {
            ee.gameObject.SetActive(true);
            ee.mirror?.gameObject.SetActive(true);
        }
        eventOperationPanel.RefreshHiddenToggle();
        hidden.Clear();
    }
}
