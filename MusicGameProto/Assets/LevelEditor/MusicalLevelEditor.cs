using MidiParser;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public enum EditingPage
{
    Midi, Panel_Left, Panel_Right
}

public class MusicalLevelEditor : MonoBehaviour
{
    public static MusicalLevelEditor ins;
    [SerializeField] Text title;
    [SerializeField] GameObject explorerPage;
    [SerializeField] GameObject editingPage;
    public GameObject[] subPages;
    [SerializeField] Button[] pageTabs;
    public ScrollRect[] scrolls;
    public RectTransform[] contents;
    [Header("Prefabs")]
    public GameObject textObj;
    public GameObject textObj2;
    public GameObject barImage;
    public GameObject node;
    public GameObject timeSelectPad;
    public GameObject rulerInteractable;
    Vector2 buttonStart = new Vector2(-720, 388);
    Vector2 buttonSpaces = new Vector2(360, -90);
    EditingPage curPage = (EditingPage)(-1);
    public Vector2[] mousePositionInScroll { private set; get; }
    public SelectPad[] selectPads;

    private void Awake()
    {
        ins = this;
    }
    private void Start()
    {
        string dataPath = Application.dataPath;
        string midiFilesPath = dataPath.Substring(0, dataPath.Length - 6) + "Midi/";
        title.text = "Midi文件路径 " + midiFilesPath;
        var info = new DirectoryInfo(midiFilesPath);
        var files = info.GetFiles();
        int i = 0;
        foreach (var f in files)
        {
            string filename = f.Name.Substring(0, f.Name.Length - 4);
            var btn = Instantiate(Resources.Load<GameObject>("SongName"), explorerPage.transform);
            btn.name = "Button_LoadMidi";
            btn.GetComponent<RectTransform>().anchoredPosition = buttonStart + new Vector2(i % 5 * buttonSpaces.x, i / 5 * buttonSpaces.y);
            btn.GetComponentInChildren<Text>().text = filename;
            btn.GetComponentInChildren<Button>().onClick.AddListener(delegate {
                explorerPage.SetActive(false);
                editingPage.SetActive(true);
                MidiPage.ins.LoadMidi(filename);
                LevelPage.ins.Setup();
                SelectTab(0);
                for (int j = 0; j < 3; ++j) contents[j].anchoredPosition = new Vector2();
            });
            ++i;
        }
        for (int j = 0; j < pageTabs.Length; ++j)
        {
            EditingPage p = (EditingPage)j;
            pageTabs[j].onClick.AddListener(delegate { SelectTab(p); });
        }
        editingPage.SetActive(false);
        mousePositionInScroll = new Vector2[3];
    }

    void SelectTab(EditingPage tab)
    {
        if (tab == curPage) return;
        for (int i = 0; i < 2; ++i)
        {
            if ((int)tab == i)
            {
                pageTabs[i].GetComponentInChildren<Image>().color = new Color32(200, 200, 200, 255);
                subPages[i].SetActive(true);
            }
            else
            {
                pageTabs[i].GetComponentInChildren<Image>().color = new Color32(255, 255, 255, 255);
                subPages[i].SetActive(false);
            }
        }
        curPage = tab;
        if (LevelPage.refreshPending && curPage == EditingPage.Panel_Left)
        {
            LevelPage.ins.Refresh();
        }
    }
    public float TimeToPagePosX(EditingPage page, float t)
    {
        if (page == EditingPage.Midi) return MidiPage.ContentStart + t * MidiPage.lengthPerSec;
        else return LevelPage.ContentStart + t * LevelPage.lengthPerSec;
    }
    public float PagePosXToTime(EditingPage page, float x)
    {
        float ret;
        if (page == EditingPage.Midi) ret= (x - MidiPage.ContentStart) / MidiPage.lengthPerSec;
        else ret= (x - LevelPage.ContentStart) / LevelPage.lengthPerSec;
        return Mathf.Max(0, ret);
    }

    public void ScrollMoveLeft(EditingPage page)
    {
        float newPos = scrolls[(int)page].horizontalNormalizedPosition - 0.01f * Time.deltaTime;
        if (newPos > 0) scrolls[(int)page].horizontalNormalizedPosition = newPos;
        else scrolls[(int)page].horizontalNormalizedPosition = 0;
    }
    public void ScrollMoveRight(EditingPage page)
    {
        scrolls[(int)page].horizontalNormalizedPosition += 0.01f * Time.deltaTime;
    }
}
