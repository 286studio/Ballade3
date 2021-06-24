using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] Text perfect, good, miss;
    [SerializeField] GameObject[] anims;
    [SerializeField] Text perfectCount, goodCount, missCount;
    [SerializeField] Text perfectScore, goodScore, missScore;
    [SerializeField] Text totalScore;
    float startTime = 0;
    Vector2 startScale = Vector2.one * 3;
    int state = 0;
    Text[] titles;
    Text[] countTexts;
    Text[] scoreTexts;
    int[] counts;
    int[] curCounts;
    int[] scores;
    int[] curScores;

    AsyncOperation async;
    FMOD.Studio.EventInstance bgm;

    // Start is called before the first frame update
    void Start()
    {
        Timeline.Stop();
        startTime = Time.time;
        titles = new Text[3] { perfect, good, miss };
        counts = new int[3] { Scoring.perfectCount, Scoring.goodCount, Scoring.missCount };
        curCounts = new int[3];
        scores = new int[3] { 20, 10, 0 };
        curScores = new int[3];
        countTexts = new Text[3] { perfectCount, goodCount, missCount };
        scoreTexts = new Text[3] { perfectScore, goodScore, missScore };
        for (int i = 0; i < 3; ++i)
        {
            anims[i].GetComponent<RectTransform>().localScale = startScale;
            anims[i].GetComponent<Text>().enabled = true;
            Graphic g = anims[i].GetComponent<Graphic>();
            Color c = g.color;
            c.a = 0.5f;
            g.color = c;
            g.enabled = false;
            titles[i].GetComponent<Text>().enabled = false;
        }
        bgm = FMODUnity.RuntimeManager.CreateInstance("event:/TBC");
        bgm.start();
        StartCoroutine(PreloadMainMenu());
    }

    // Update is called once per frame
    void Update()
    {
        if (state == 0)
        {
            float time = Time.time - startTime;
            for (int i = 0; i < 3; ++i)
            {
                if (anims[i])
                {
                    float frac = Mathf.Clamp((time - 0.25f * i) / 0.5f, 0, 1);
                    anims[i].GetComponent<Text>().enabled = frac > 0;
                    anims[i].GetComponent<RectTransform>().localScale = Vector2.Lerp(startScale, Vector2.one, frac);
                    curCounts[i] = Mathf.RoundToInt(frac * counts[i]);
                    countTexts[i].text = curCounts[i].ToString();
                    curScores[i] = curCounts[i] * scores[i];
                    scoreTexts[i].text = curScores[i].ToString();
                    if (frac == 1)
                    {
                        Destroy(anims[i]);
                        titles[i].GetComponent<Text>().enabled = true;
                        if (i == 2) state = 1;
                    }
                }
            }
            int total = 0; foreach (int i in curScores) total += i;
            totalScore.text = total.ToString();
        }
        else if (state == 1)
        {

        }
    }

    IEnumerator PreloadMainMenu()
    {
        async = SceneManager.LoadSceneAsync(0);
        while (async == null)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }
        async.allowSceneActivation = false;
    }

    public void ContinueButtonPressed()
    {
        if (async != null)
        {
            bgm.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            async.allowSceneActivation = true;
        }
    }
}
