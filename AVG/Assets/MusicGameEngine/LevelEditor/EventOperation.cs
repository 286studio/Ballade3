using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventOperation : MonoBehaviour
{
    [Header("Meta")]
    [SerializeField] GameObject[] pages;
    [SerializeField] Dropdown eventTypeDP;
    [SerializeField] InputField startTimeIF;
    [SerializeField] Dropdown addTagDP;
    [SerializeField] Button doneAddTagBTN;
    [SerializeField] GameObject conflictDialog;
    [Header("Midi")]
    [SerializeField] Dropdown panelDP;
    [SerializeField] Dropdown exitDP;
    [SerializeField] Button dissolveBTN;
    [SerializeField] Text midiDesc;
    [Header("Other")]
    [SerializeField] Toggle hiddenTG;
    [SerializeField] Button delete;
    EditorEvent currentEvent;
    Dictionary<string, int> eventTypeNameToIndex;
    float offset;
    float metaPageStart = 150;
    float metaPageStep = -30;
    string[] tagsToAdd = new string[11]
    {
        EventTags.Enable3D,
        EventTags.SetParam,
        EventTags.SFX,
        EventTags.Image,
        EventTags.VideoVolumeFadeOut,
        EventTags.MusicFadeIn,
        EventTags.MusicFadeOut,
        EventTags.PreloadVideoClip,
        EventTags.ShowPreloadVideo,
        EventTags.PlayVideoClip,
        EventTags.PanelAlpha
    };
    string[] tagsToChinese = new string[11]
    {
        "启用3D", //0
        "设置参数",//1
        "播放声效",//2
        "插入图片",//3
        "视频音效淡出",//4
        "音乐淡入",//5
        "音乐淡出",//6
        "预载视频",//7
        "显示预载视频",//8
        "播放视频",//9
        "设置谱面透明度"//10
    };
    int[] tagUIHeights = new int[11] { 1,2,1,1,1,1,1,3,1,3,1 };
    HashSet<string> hasDuration = new HashSet<string>()
    {
        EventTags.VideoVolumeFadeOut,
        EventTags.MusicFadeIn,
        EventTags.MusicFadeOut,
    };
    HashSet<string>[] conflicts = new HashSet<string>[]
    {
        new HashSet<string>(new string[] { EventTags.VideoVolumeFadeOut, EventTags.PreloadVideoClip, EventTags.ShowPreloadVideo, EventTags.PlayVideoClip }),
        new HashSet<string>(new string[] { EventTags.MusicFadeIn, EventTags.MusicFadeOut })
    };
    Dictionary<string, KeyValuePair<GameObject, int>> addedTags = new Dictionary<string, KeyValuePair<GameObject, int>>();
    HashSet<string> midiPropDontShow = new HashSet<string>()
    {
        EventTags.Exit, EventTags.Panel
    };
    private void Awake()
    {
        eventTypeDP.ClearOptions();
        eventTypeNameToIndex = new Dictionary<string, int>();
        for (int i = 0; i < LevelPage.chineseMetaEvent.Length; ++i)
        {
            eventTypeDP.options.Add(new Dropdown.OptionData(LevelPage.chineseMetaEvent[i]));
            eventTypeNameToIndex.Add(EventTypes.Meta[i], i);
        }
        addTagDP.ClearOptions();
        addTagDP.AddOptions(new List<string>(tagsToChinese));
    }
    private void Start()
    {
        eventTypeDP.onValueChanged.AddListener(OnEventTypeChanged);
        startTimeIF.onValueChanged.AddListener(OnStartTimeChanged);
        doneAddTagBTN.onClick.AddListener(AddTag);
        conflictDialog.GetComponentInChildren<Button>().onClick.AddListener(() => conflictDialog.SetActive(false));
        hiddenTG.onValueChanged.AddListener(OnHidden);
        delete.onClick.AddListener(LevelPage.DeleteSelected);
        dissolveBTN.onClick.AddListener(DissolveCurrentEvent);
    }

    private void Update()
    {
        // handle dragging
        if (Input.GetMouseButton(0) && LevelPage.OneSelection)
        {
            if (currentEvent.pointerDownStartTime > 0 && Time.time - currentEvent.pointerDownStartTime >= 0.25f)
            {
                currentEvent.dragging = true;
                currentEvent.pointerDownStartTime = -1;
                MusicalLevelEditor.ins.scrolls[1].horizontal = false;
                MusicalLevelEditor.ins.scrolls[2].horizontal = false;
                startTimeIF.interactable = false;
                offset = MusicalLevelEditor.ins.mousePositionInScroll[1].x - currentEvent.mirror.GetComponent<RectTransform>().anchoredPosition.x;
                print("Dragging starts");
            }
        }
        else currentEvent.pointerDownStartTime = -1;

        if (currentEvent && currentEvent.dragging)
        {
            if (Input.GetMouseButton(0))
            {
                float x = MusicalLevelEditor.ins.mousePositionInScroll[1].x - offset;
                float time = MusicalLevelEditor.ins.PagePosXToTime(EditingPage.Panel_Left, x);
                currentEvent.kd.startTime = time;
                startTimeIF.text = time.ToString();
                currentEvent.mirror.GetComponent<RectTransform>().anchoredPosition = currentEvent.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);
            }
            else
            {
                currentEvent.dragging = false;
                startTimeIF.interactable = true;
                MusicalLevelEditor.ins.scrolls[1].horizontal = true;
                MusicalLevelEditor.ins.scrolls[2].horizontal = true;
                print("Dragging ends");
            }
        }
    }
    public void Show(EditorEvent e)
    {
        currentEvent = e;
        hiddenTG.isOn = !e.gameObject.activeInHierarchy;
        for (int i = 0; i < pages.Length; ++i) pages[i].SetActive(i != (int)e.eventType);
        string str;
        if (e.eventType == LevelEventType.Meta)
        {
            if (e.kd.prop.TryGetValue("Type", out str)) eventTypeDP.value = eventTypeNameToIndex[str];
            else eventTypeDP.value = 0;
            startTimeIF.text = e.kd.startTime.ToString();
            foreach (var p in addedTags) Destroy(p.Value.Key);
            addedTags.Clear();
            for (int i = 0; i < tagsToAdd.Length; ++i) if (e.kd.prop.ContainsKey(tagsToAdd[i])) AddTag(i, true);
        }
        else
        {
            exitDP.onValueChanged.RemoveAllListeners();
            if(e.kd.prop.TryGetValue(EventTags.Exit, out str))
            {
                exitDP.value = int.Parse(str);
            }
            else
            {
                e.kd.prop[EventTags.Exit] = "0";
                exitDP.value = 0;
            }
            exitDP.onValueChanged.AddListener(OnExitChanged);
            panelDP.onValueChanged.RemoveAllListeners();
            if (e.kd.prop.TryGetValue(EventTags.Panel, out str))
            {
                panelDP.value = str == PanelType.Left.ToString() ? 0 : 1;
            }
            else
            {
                e.kd.prop[EventTags.Panel] = PanelType.Left.ToString();
                panelDP.value = 0;
            }
            panelDP.onValueChanged.AddListener(OnPanelChanged);
            if(e.kd.prop.TryGetValue(EventTags.Type, out str)) exitDP.interactable = str != EventTypes.HorizontalMove;
            bool dissolvable = false;
            if (e.kd.prop.TryGetValue(EventTags.Note, out str)) 
                if (str.Split(',').Length > 1)
                    dissolvable = true;
            dissolveBTN.interactable = dissolvable;
            dissolveBTN.GetComponentInChildren<Text>().text = dissolvable ? "分解" : "无法分解";

            string text = "开始时间: " + e.kd.startTime + "\n";
            foreach (var kv in e.kd.prop)
                if (!midiPropDontShow.Contains(kv.Key))
                    text += EventTags.GetChinese(kv.Key) + ": " + kv.Value + "\n";
            midiDesc.text = text;
        }
    }
    void OnEventTypeChanged(int idx)
    {
        currentEvent.kd.prop["Type"] = EventTypes.Meta[idx];
        currentEvent.mirror.GetComponentInChildren<Text>().text = currentEvent.GetComponentInChildren<Text>().text = LevelPage.chineseMetaEvent[idx];
    }
    void OnStartTimeChanged(string t)
    {
        float time = float.Parse(t);
        currentEvent.kd.startTime = time;
        currentEvent.mirror.GetComponent<RectTransform>().anchoredPosition = currentEvent.GetComponent<RectTransform>().anchoredPosition = new Vector2(MusicalLevelEditor.ins.TimeToPagePosX(EditingPage.Panel_Left, time), 0);
    }

    void AddTag() => AddTag(addTagDP.value);

    void AddTag(int idx, bool readData = false)
    {
        string tag = tagsToAdd[idx];
        // 无法添加
        string conflictTag;
        if (Conflicts(out conflictTag) || addedTags.ContainsKey(tag))
        {
            conflictDialog.SetActive(true);
            string msg = "无法添加语句\n\n";
            if (conflictTag.Length > 0)
            {
                int i;
                for (i = 0; i < tagsToAdd.Length; ++i) if (tagsToAdd[i] == conflictTag) break;
                currentEvent.kd.prop.Remove(tag);
                msg += "与现有语句\"" + tagsToChinese[i] + "\"冲突\n\n确定";
            }
            else
            {
                msg += "该语句已经存在\n\n确定";
            }
            conflictDialog.GetComponentInChildren<Text>().text = msg;
            return;
        }
        // 可以添加
        var ui = Instantiate(Resources.Load<GameObject>(tag), pages[0].transform);
        addedTags.Add(tag, new KeyValuePair<GameObject, int>(ui, tagUIHeights[idx]));
        var deleteButton = ui.GetComponentInChildren<Button>();
        switch (idx)
        {
            case 0: // 启用3D
            case 8: // 显示预载视频
                currentEvent.kd.prop[tag] = "true";
                deleteButton.onClick.AddListener(delegate
                {
                    currentEvent.kd.prop.Remove(tag);
                    addedTags.Remove(tag);
                    Destroy(ui);
                    ReorderLayout();
                });
                break;
            case 1: // 设置参数
                InputField[] ifs = ui.GetComponentsInChildren<InputField>();
                if (readData)
                {
                    string[] seg = currentEvent.kd.prop[tag].Split(',');
                    ifs[0].text = seg[0];
                    ifs[1].text = seg[1];
                }
                ifs[0].onValueChanged.AddListener((s) => currentEvent.kd.prop[tag] = s + "," + ifs[1].text);
                ifs[1].onValueChanged.AddListener((s) => currentEvent.kd.prop[tag] = ifs[0].text + "," + s);
                deleteButton.onClick.AddListener(delegate
                {
                    currentEvent.kd.prop.Remove(tag);
                    addedTags.Remove(tag);
                    Destroy(ui);
                    ReorderLayout();
                });
                break;
            case 2: // 播放声效
            case 3: // 插入图片
            case 4: // 视频音效淡出
            case 5: // 音效淡入
            case 6: // 音效淡出
                var @if = ui.GetComponentInChildren<InputField>();
                if (readData) @if.text = currentEvent.kd.prop[tag];
                @if.onValueChanged.AddListener((s) => { currentEvent.kd.prop[tag] = s; if (hasDuration.Contains(tag)) ResizeEvent(currentEvent); });
                deleteButton.onClick.AddListener(delegate
                {
                    currentEvent.kd.prop.Remove(tag);
                    addedTags.Remove(tag);
                    Destroy(ui);
                    ReorderLayout();
                    if (hasDuration.Contains(tag)) ResizeEvent(currentEvent);
                });
                break;
            case 7: // 预载视频
            case 9: // 播放视频
                ifs = ui.GetComponentsInChildren<InputField>();
                var ratio = ui.GetComponentInChildren<Dropdown>();
                if (readData)
                {
                    ifs[0].text = currentEvent.kd.prop[tag];
                    string str;
                    if (currentEvent.kd.prop.TryGetValue(EventTags.Ratio, out str))
                    {
                        for (int i = 0; i < ratio.options.Count; ++i)
                            if (str == ratio.options[i].text)
                            {
                                ratio.value = i;
                                break;
                            }
                    }
                    else currentEvent.kd.prop[EventTags.Ratio] = ratio.options[0].text;
                    if (currentEvent.kd.prop.TryGetValue(EventTags.VideoStartTime, out str))
                        ifs[1].text = str;
                    else ifs[1].text = "0";
                }
                ifs[0].onValueChanged.AddListener((s) => currentEvent.kd.prop[tag] = s);
                ratio.onValueChanged.AddListener((i) => currentEvent.kd.prop[EventTags.Ratio] = ratio.options[i].text);
                ifs[1].onValueChanged.AddListener((s) => currentEvent.kd.prop[EventTags.VideoStartTime] = s);
                deleteButton.onClick.AddListener(delegate
                {
                    currentEvent.kd.prop.Remove(tag);
                    currentEvent.kd.prop.Remove(EventTags.Ratio);
                    currentEvent.kd.prop.Remove(EventTags.VideoStartTime);
                    addedTags.Remove(tag);
                    Destroy(ui);
                    ReorderLayout();
                });
                break;
        }
        ReorderLayout();
    }
    void ReorderLayout()
    {
        int y = 0;
        foreach (var p in addedTags)
        {
            (p.Value.Key.transform as RectTransform).anchoredPosition = new Vector2(0, metaPageStart + y * metaPageStep);
            y += p.Value.Value;
        }
    }
    bool Conflicts(out string conflictTag)
    {
        foreach (var t in addedTags)
        {
            foreach (var p in conflicts)
            {
                if (p.Contains(t.Key) && p.Contains(tagsToAdd[addTagDP.value]))
                {
                    conflictTag = t.Key;
                    return true;
                }
            }
        }
        conflictTag = "";
        return false;
    }

    void OnHidden(bool hidden)
    {
        currentEvent.gameObject.SetActive(!hidden);
        currentEvent.mirror?.gameObject.SetActive(!hidden);
        if (hidden) LevelPage.ins.hidden.Add(currentEvent);
        else LevelPage.ins.hidden.Remove(currentEvent);
    }
    public void RefreshHiddenToggle()
    {
        if (currentEvent) hiddenTG.isOn = !currentEvent.gameObject.activeInHierarchy;
    }
    public void ResizeEvent(EditorEvent e)
    {
        int idx = eventTypeNameToIndex[e.kd.prop[EventTags.Type]];
        float t = LevelPage.metaEventLengths[idx];
        foreach (var p in hasDuration)
        {
            string str;
            if (e.kd.prop.TryGetValue(p, out str))
            {
                float value;
                if (float.TryParse(str, out value))
                    if (value > t) t = value;
            }
        }
        Vector2 newSize = new Vector2(Mathf.Max(4, t * LevelPage.lengthPerSec), LevelPage.ContentHeight);
        e.rt.sizeDelta = newSize;
        if (e.mirror) e.mirror.rt.sizeDelta = newSize;
    }

    private void OnExitChanged(int val)
    {
        currentEvent.kd.prop[EventTags.Exit] = val.ToString();
        if (currentEvent.kd.prop[EventTags.Type] == EventTypes.HorizontalMove)
        {
            Debug.LogError("不能随意改变竖滑的出口！");
        }
        else
        {
            var pos = currentEvent.rt.anchoredPosition;
            pos.y = LevelPage.ins.ExitPos(val);
            currentEvent.rt.anchoredPosition = pos;
        }
    }
    private void OnPanelChanged(int arg0)
    {
        currentEvent.kd.prop[EventTags.Panel] = ((PanelType)arg0).ToString();
        var pos = currentEvent.rt.anchoredPosition;
        currentEvent.rt.parent = LevelPage.ins.midiEventNodes[arg0];
        currentEvent.rt.anchoredPosition = pos;
        if (arg0 == 0)
        {
            LevelPage.ins.leftEvents.Add(currentEvent);
            LevelPage.ins.rightEvents.Remove(currentEvent);
        }
        else
        {
            LevelPage.ins.rightEvents.Add(currentEvent);
            LevelPage.ins.leftEvents.Remove(currentEvent);
        }
    }

    private void DissolveCurrentEvent()
    {
        throw new System.NotImplementedException();
    }
}
