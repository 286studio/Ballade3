using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Michsky.UI.Shift;
using UnityEngine.SceneManagement;

public class SelectLevel : MonoBehaviour
{
    [SerializeField] GameObject node;
    [SerializeField] GameObject chapterPrefab;
    [SerializeField] Sprite[] tilepages;
    [SerializeField] string[] titles;
    [SerializeField] [TextArea] string[] desc;
    [SerializeField] string[] songNames;
    GameObject preenterDialog;
    [HideInInspector] public string preselectedSongName;
    [HideInInspector] public float fallingTime = 3;

    public static SelectLevel ins;

    private void Awake()
    {
        if (ins) Destroy(gameObject); else ins = this;
        int i = 0;
        foreach (string t in titles)
        {
            var btn = Instantiate(chapterPrefab, node.transform).GetComponent<ChapterButton>();
            btn.buttonTitle = t;
            btn.statusItem = ChapterButton.StatusItem.NONE;
            btn.backgroundImage = tilepages[i];
            btn.buttonDescription = desc[i];
            int j = i;
            btn.GetComponent<Button>().onClick.AddListener(delegate
            {
                preselectedSongName = songNames[j];
                preenterDialog = Instantiate(Resources.Load<GameObject>("PreEnter"), GameObject.Find("Canvas").transform);
                //SceneManager.LoadScene(1);
            });
            ++i;
        }
        DontDestroyOnLoad(this);
    }
}
