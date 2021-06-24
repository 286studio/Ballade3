using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MidiOperation : MonoBehaviour
{
    [SerializeField] Dropdown selection;
    [SerializeField] GameObject[] pages;
    [SerializeField] Button start;
    [SerializeField] Dropdown panelDP;
    [Header("FallingBlock")]
    [SerializeField] Dropdown orderByDP; // 0=按音高 1=按出口顺序
    [SerializeField] Dropdown ascendingDP; // 0=升序 1=降序
    [Header("VerticalSwipe")]
    [SerializeField] Dropdown headExitDP;
    [SerializeField] Dropdown directionDP;
    [SerializeField] Dropdown widthDP;
    [Header("LongPress")]
    [SerializeField] Dropdown exitDP;
    [SerializeField] Dropdown lengthDP;
    void Start()
    {
        SelectPage(selection.value);
        selection.onValueChanged.AddListener(SelectPage);
        start.onClick.AddListener(StartConvertion);

        // 关于竖滑转化的一些限制
        headExitDP.onValueChanged.AddListener(OnVerticalSwipeExitChanged);
        headExitDP.value = 1;
    }
    private void OnEnable()
    {
        start.interactable = true;
        start.GetComponentInChildren<Text>().text = "开始转换";
    }
    void SelectPage(int idx)
    {
        for (int i = 0; i < pages.Length; ++i) pages[i].SetActive(i == idx);
    }
    void OnVerticalSwipeExitChanged(int val)
    {
        if (val == 0)
        {
            // 出口选为上时, 朝向只能选往下
            directionDP.value = 1;
            directionDP.interactable = false;
            widthDP.interactable = true;
        }
        else if (val == 1)
        {
            // 出口选为中时, 宽度只能选2
            directionDP.interactable = true;
            widthDP.value = 0;
            widthDP.interactable = false;
        }
        else if (val == 2)
        {
            // 出口选为下时, 朝向只能选往上
            directionDP.value = 0;
            directionDP.interactable = false;
            widthDP.interactable = true;
        }
    }
    void StartConvertion()
    {
        List<Note> list = new List<Note>();
        foreach (var nb in MidiPage.ins.selected) list.Add(nb.note);
        list.Sort((x, y) => x.startTimeInSec.CompareTo(y.startTimeInSec));
        if (selection.value == 0)
        {
            ConvertToFallingBlock(list);
        }
        else if (selection.value == 1)
        {
            MergeIntoVerticalSwipe(list);
        }
        else if (selection.value == 2)
        {
            MergeIntoLongPress(list);
        }
        LevelPage.ins.SaveToFile();
        LevelPage.refreshPending = true;
        start.interactable = false;
        start.GetComponentInChildren<Text>().text = MidiPage.ins.selected.Count + "个音符已转换";
    }
    void ConvertToFallingBlock(List<Note> input)
    {
        int curExit = 0;
        int noteMin = int.MaxValue, noteMax = int.MinValue;
        if (orderByDP.value == 0) // 按音高
        {
            foreach (var n in input)
            {
                if (n.note > noteMax) noteMax = n.note;
                if (n.note < noteMin) noteMin = n.note;
            }
        }
        for (int i = 0; i < input.Count; ++i)
        {
            KeyData kd = new KeyData(input[i].startTimeInSec);
            kd.prop[EventTags.Type] = EventTypes.FallingBlock;
            kd.prop[EventTags.Panel] = ((PanelType)panelDP.value).ToString();
            // calculate exit
            if (ascendingDP.value == 0) //升序
            {
                switch (orderByDP.value)
                {
                    case 0:
                        curExit = Mathf.RoundToInt((float)(noteMax - input[i].note) / (noteMax - noteMin) * 3f - 0.5f);
                        break;
                    case 1:
                        ++curExit;
                        if (curExit > 2) curExit = 0;
                        break;
                    default:
                        Debug.LogError("MidiOperationConvertion: Can't reach!");
                        break;
                }
            }
            else // 降序
            {
                switch (orderByDP.value)
                {
                    case 0:
                        curExit = Mathf.RoundToInt((float)(input[i].note - noteMin) / (noteMax - noteMin) * 3f - 0.5f);
                        break;
                    case 1:
                        --curExit;
                        if (curExit < 0) curExit = 2;
                        break;
                    default:
                        Debug.LogError("MidiOperationConvertion: Can't reach!");
                        break;
                }
            }
            kd.prop[EventTags.Exit] = curExit.ToString();
            kd.prop[EventTags.Note] = input[i].note.ToString();
            LevelPage.ins.keyData.Add(kd);
        }
    }
    void MergeIntoVerticalSwipe(List<Note> input)
    {
        List<int> noteVals = new List<int>() { input[0].note };
        List<float> delays = new List<float>();
        for (int i = 1; i < input.Count; ++i)
        {
            noteVals.Add(input[i].note);
            delays.Add(input[i].startTimeInSec - input[i - 1].startTimeInSec);
        }
        KeyData kd = new KeyData(input[0].startTimeInSec);
        kd.prop[EventTags.Type] = EventTypes.HorizontalMove;
        kd.prop[EventTags.Panel] = ((PanelType)panelDP.value).ToString();
        kd.prop[EventTags.Exit] = headExitDP.value.ToString();
        kd.prop[EventTags.Direction] = ((Direction)directionDP.value).ToString();
        kd.prop[EventTags.Width] = (widthDP.value + 2).ToString();
        string temp = "";
        for (int i = 0; i < noteVals.Count; ++i) temp += noteVals[i] + (i == noteVals.Count - 1 ? "\n" : ",");
        kd.prop[EventTags.Note] = temp;
        temp = "";
        for (int i = 0; i < delays.Count; ++i) temp += delays[i] + (i == delays.Count - 1 ? "\n" : ",");
        kd.prop[EventTags.Delays] = temp;
        LevelPage.ins.keyData.Add(kd);
    }
    void MergeIntoLongPress(List<Note> input)
    {
        List<int> noteVals = new List<int>() { input[0].note };
        List<float> delays = new List<float>();
        for (int i = 1; i < input.Count; ++i)
        {
            noteVals.Add(input[i].note);
            delays.Add(input[i].startTimeInSec - input[i - 1].startTimeInSec);
        }
        KeyData kd = new KeyData(input[0].startTimeInSec);
        kd.prop[EventTags.Type] = EventTypes.LongFallingBlock;
        kd.prop[EventTags.Panel] = ((PanelType)panelDP.value).ToString();
        kd.prop[EventTags.Exit] = exitDP.value.ToString();
        kd.prop[EventTags.Length] = (lengthDP.value + 1).ToString();
        string temp = "";
        for (int i = 0; i < noteVals.Count; ++i) temp += noteVals[i] + (i == noteVals.Count - 1 ? "\n" : ",");
        kd.prop[EventTags.Note] = temp;
        temp = "";
        for (int i = 0; i < delays.Count; ++i) temp += delays[i] + (i == delays.Count - 1 ? "\n" : ",");
        kd.prop[EventTags.Delays] = temp;
        LevelPage.ins.keyData.Add(kd);
    }
}
