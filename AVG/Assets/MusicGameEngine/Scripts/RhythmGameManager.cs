
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DefRes // 开发用分辨率
{
    public static int x { private set; get; } = 1920;
    public static int y { private set; get; } = 1080;
}
public class BlockSize
{
    public static int x = 90; // 原来是45，改为90增大判定区域
    public static int y = 90;
}

public struct GeneralSettings
{
    public static int mode; // 0=正常, 1=拖轨
    public static int exitCount;
    public static float delay;
    public static int specialMode = 0; // 0=正常, 1=把所有方块当作下落方块, 2=不下落任何方块
    public static float musicStartTime;
    public static float fallingTime = 3;
    public static bool midiTrack = false;
    public static void Reset()
    {
        mode = 0;
        exitCount = 0;
        delay = 0;
        specialMode = 0;
        musicStartTime = 0;
        fallingTime = 3;
        midiTrack = false;
    }
}

public struct Scoring
{
    public static int perfectCount;
    public static int goodCount;
    public static int missCount;
    public static void Reset()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
    }
}

public class ExitData
{
    public int id;
    public PanelType panel;
    public GameObject obj, idctor;
    public float y_top, y_bot;
    public RhythmObject current, last;
    public Vector2 center;
    bool needReleasing;
    bool isBeingTouchedPending;
    bool isBeingTouchedFinal;
    bool IsBeingTouchedBy(Vector3 pos)
    {
        bool leftJudging = panel == PanelType.Left && RhythmGameManager.leftBottomRect.Contains(pos);
        bool rightJudging = panel == PanelType.Right && RhythmGameManager.rightBottomRect.Contains(pos);
        return (leftJudging || rightJudging) && pos.y < y_top && pos.y > y_bot;
    }
    public bool IsBeingTouched()
    {
        return isBeingTouchedFinal;
    }
    public void SetX(float x)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
    }
    public static void CheckInput()
    {
        if (RhythmGameManager.exits == null) return;
        ExitData[] exits = RhythmGameManager.exits;
        HashSet<ExitData> touched = new HashSet<ExitData>();
        B3Input.GatherAllInputs();
        for (int i = 0; i < exits.Length; ++i)
        {
            exits[i].isBeingTouchedPending = false;
            if (exits[i].last != exits[i].current) exits[i].needReleasing = true;
            if (!touched.Contains(exits[i]))
            {
                for (int j = 0; j < B3Input.count; ++j)
                {
                    if (exits[i].IsBeingTouchedBy(B3Input.GetInput(j)))
                    {
                        exits[i].isBeingTouchedPending = true;
                        touched.Add(exits[i]);
                        break;
                    }
                }
            }

            if (exits[i].needReleasing)
            {
                if (exits[i].isBeingTouchedPending) exits[i].isBeingTouchedPending = false;
                else exits[i].needReleasing = false;
            }
            exits[i].isBeingTouchedFinal = exits[i].isBeingTouchedPending;
            exits[i].last = exits[i].current;
        }
    }
}

public class RhythmGameManager : MonoBehaviour
{
    [SerializeField] Transform parentNode;
    [SerializeField] GameObject gameContent;
    [SerializeField] GameObject selectSong;
    public static ExitData[] exits;
    [SerializeField] Text UIScore;
    [SerializeField] Button reload;
    public Button pauseButton;
    [SerializeField] Sprite[] pauseButtonSprites;
    [SerializeField] Text timeShown;
    [SerializeField] GameObject loading;
    public GameObject invisibleBlocker;
    public Transform imageNode;

    int score;
    public static Rect leftBottomRect, rightBottomRect;
    Vector2 buttonStart = new Vector2(-720, 388);
    Vector2 buttonSpaces = new Vector2(360, -90);
    List<ExitData[]> oldExits = new List<ExitData[]>();

    public static RhythmGameManager ins;

    public event Void_Float OnBinguiXFracUpdate;

    public Panel leftPanel;
    public Panel rightPanel;

    public bool autoMode;
    public Platform platform;

    private void Awake()
    {
        ins = this;
        exits = null;
        reload.onClick.AddListener(delegate
        {
            Timeline.Stop();
            SceneManager.LoadScene(0);
        });
        Panel.Left = leftPanel;
        Panel.Left.panelType = PanelType.Left;
        Panel.Right = rightPanel;
        Panel.Right.panelType = PanelType.Right;
        Panel.state = 0;
    }
    void Start()
    {
        score = 0;
        UIScore.text = "Score: 0";

        // 左边底部判定区
        RectTransform brt = Panel.Left.bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(BlockSize.x, PanelSize.y);
        leftBottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2 - new Vector2(BlockSize.x / 2f, 0), brt.sizeDelta + new Vector2(BlockSize.x, 0));
        // 右边底部判定区
        brt = Panel.Right.bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(BlockSize.x, PanelSize.y);
        rightBottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2 - new Vector2(BlockSize.x / 2f, 0), brt.sizeDelta + new Vector2(BlockSize.x, 0));

        pauseButton.onClick.AddListener(OnPauseButtonPressed);
        pauseButton.gameObject.SetActive(false);

        if (!SelectLevel.ins)
        {
            var info = new DirectoryInfo("Assets/MusicGameEngine/MusicalLevels/Resources/" + platform);
            var dirs = info.GetDirectories();
            int i = 0;
            foreach (var d in dirs)
            {
                string songName = d.Name;
                var btn = Instantiate(Resources.Load<GameObject>("SongName"), selectSong.transform);
                btn.GetComponent<RectTransform>().anchoredPosition = buttonStart + new Vector2(i % 5 * buttonSpaces.x, i / 5 * buttonSpaces.y);
                btn.GetComponentInChildren<Text>().text = songName;
                btn.GetComponent<Button>().onClick.AddListener(delegate
                {
                    Timeline.StartMusicScript(songName);
                    GenerateExits();
                    Destroy(selectSong);
                });
                ++i;
            }
        }
        else
        {
            Timeline.StartMusicScript(SelectLevel.ins.preselectedSongName, SelectLevel.ins.fallingTime);
            Destroy(SelectLevel.ins.gameObject);
            GenerateExits();
            Destroy(selectSong);
        }
    }

    bool pause;
    private void Update()
    {
        // timeShown.text = ((int)((Time.time - Timeline.ins.startTime) * 1000)).ToString();
        if (Input.GetKeyDown(KeyCode.Escape)) OnPauseButtonPressed();

        ExitData.CheckInput();
    }

    private void FixedUpdate()
    {
        oldExits.RemoveAll(RemoveOldExits);
    }

    public void OnPauseButtonPressed()
    {
        pause = !pause;
        pauseButton.image.sprite = pauseButtonSprites[pause ? 1 : 0];
        Timeline.Pause(pause);
    }

    bool RemoveOldExits(ExitData[] es)
    {
        bool shouldBeRemoved = true;
        foreach (ExitData ed in es)
        {
            if (ed.current)
            {
                shouldBeRemoved = false;
                break;
            }
        }
        if (shouldBeRemoved)
        {
            print("Removing");
            StopAllCoroutines();
            foreach (ExitData ed in es)
            {
                // Instantiate(Resources.Load<GameObject>("explosion"), ins.parentNode).transform.position = ed.obj.transform.position;
                Destroy(ed.obj);
                Destroy(ed.idctor);
            }
        }
        return shouldBeRemoved;
    }

    public void RestartGame(string newSongName = "", int startScore = 0)
    {
        score = startScore;
        UIScore.text = "Score: " + score;

        // 左边底部判定区
        RectTransform brt = Panel.Left.bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(BlockSize.x, PanelSize.y);
        leftBottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2 - new Vector2(BlockSize.x / 2f, 0), brt.sizeDelta + new Vector2(BlockSize.x, 0));
        // 右边底部判定区
        brt = Panel.Right.bottom.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(BlockSize.x, PanelSize.y);
        rightBottomRect = new Rect(brt.anchoredPosition - brt.sizeDelta / 2 - new Vector2(BlockSize.x / 2f, 0), brt.sizeDelta + new Vector2(BlockSize.x, 0));

        Panel.state = 0;

        Timeline.Stop();
        Timeline.StartMusicScript(newSongName);
        GenerateExits();
    }

    public void StopGame()
    {
        exits = null;
        Timeline.Stop();
    }

    public static float GetBottom(PanelType panel)
    {
        // 判定区的x位置
        // 0=left,1=right
        return panel == PanelType.Left ? GetLeftBottom() : GetRightBottom();
    }

    public static float GetLeftBottom()
    {
        return -Panel.bottomPos;
    }

    public static float GetRightBottom()
    {
        return Panel.bottomPos;
    }

    public static void UpdateScore(int s)
    {
        ins.score += s;
        ins.UIScore.text = "Score: " + ins.score;
    }

    public static RhythmObject CreateBlock(int exit, PanelType panel, string blockName, Color? c = null, int perfectScore = 20, int goodScore = 10, int badScore = 0, float debugTime = -1)
    {
        string prefabName = blockName;
        if (GeneralSettings.mode > 0) prefabName += "_" + GeneralSettings.mode;
        var ret = Instantiate(Resources.Load<GameObject>(prefabName), ins.parentNode).GetComponent<RhythmObject>().Initialize(exit, panel, c == null ? Color.white : c.GetValueOrDefault(), perfectScore, goodScore, badScore);
        if (debugTime > 0)
        {
            var text = ret.GetComponentInChildren<Text>();
            if (text)
            {
                text.color = Color.black;
                text.text = debugTime.ToString();
            }
        }
        return ret;
    }

    public static Beat CreateBeat(int exit, PanelType panel, Color? c = null, int perfectScore = 20, int goodScore = 10, int badScore = 0)
    {
        var ret = Instantiate(Resources.Load<GameObject>("Beat"), ins.parentNode).GetComponent<Beat>().Initialize(exit, panel, c == null ? Color.white : c.GetValueOrDefault(), perfectScore, goodScore, badScore);
        ret.fallingTime = 1f;
        ((Beat)ret).lifetime = 1f;
        var pos = (ret.transform as RectTransform).anchoredPosition;
        pos.y = Harp.GetHeight();
        (ret.transform as RectTransform).anchoredPosition = pos;
        return (Beat)ret;
    }

    public static void GenerateExits()
    {
        // handling old exits
        if (exits != null)
        {
            //for (int i = 0; i < exits.Length; ++i) ins.StartCoroutine(ins.ExitFlashing(exits[i]));
            ins.oldExits.Add(exits);
        }

        int num = GeneralSettings.exitCount;
        float step = PanelSize.y / num * 0.9f;
        exits = new ExitData[num * 2];
        for (int h = 0; h < 2; ++h)
        {
            float top = h == 0 ? Panel.longExitPos : Panel.exitPos;
            for (int i = 0; i < num; ++i)
            {
                var e = exits[i + h * num] = new ExitData();
                e.id = i;
                e.panel = (PanelType)h;
                e.obj = Instantiate(Resources.Load<GameObject>("exit"), ins.parentNode);
                e.idctor = Instantiate(Resources.Load<GameObject>("Indicator"), ins.parentNode);
                RectTransform rt = e.obj.transform as RectTransform;
                e.obj.SetActive(false);
                e.idctor.SetActive(false);
                rt.sizeDelta = new Vector2(BlockSize.x, BlockSize.y);
                float y = step * -(i + 0.5f) + PanelSize.y / 2f * 0.9f + PanelPos.y;
                rt.anchoredPosition = new Vector2(top, y);
                (e.idctor.transform as RectTransform).anchoredPosition = new Vector2(GetBottom((PanelType)h), y);
                e.y_top = rt.anchoredPosition.y + rt.sizeDelta.y / 2;
                e.y_bot = rt.anchoredPosition.y - rt.sizeDelta.y / 2;
                e.center = new Vector2(GetBottom((PanelType)h), rt.anchoredPosition.y);
            }
        }
    }

    public void LoadMainMenu()
    {
        StartCoroutine(WaitForLoadMainMenuFinished());
    }

    IEnumerator WaitForLoadMainMenuFinished()
    {
        Time.timeScale = 1;
        loading.SetActive(true);
        AsyncOperation async = SceneManager.LoadSceneAsync(0);
        do
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }
        while (async == null);
        async.allowSceneActivation = false;
        yield return new WaitForSecondsRealtime(1f);
        loading.SetActive(false);
        async.allowSceneActivation = true;
    }

    public static void HideContent(bool hide = true)
    {
        ins.gameContent.SetActive(!hide);
    }

    public static void HideExit(bool hide = true)
    {
        foreach (ExitData ed in exits) ed.obj.SetActive(!hide);
    }

    public static void HideExit(HashSet<int> exitsToHide)
    {
        for (int i = 0; i < exits.Length; ++i) if (exitsToHide.Contains(i)) exits[i].obj.SetActive(false);
    }

    public static void HideBottomBar(bool hide = true)
    {
        Panel.Left.bottom.gameObject.SetActive(!hide);
        Panel.Right.bottom.gameObject.SetActive(!hide);
    }
}
