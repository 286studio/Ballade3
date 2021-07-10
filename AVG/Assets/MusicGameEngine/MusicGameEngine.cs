using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 音游引擎以外的组件需要与音游引擎交互时，只调用这个类里的函数
public static class MusicGameEngine
{
    public static string scriptName_ClearLevel;
    public static string label_ClearLevel;
    public static string scriptName_FailLevel;
    public static string label_FailLevel;
    public static int scorePercentageConsideredFail;
    public static bool loadedFromAVG;
    public static void LoadLevel(string songName)
    {
        loadedFromAVG = true;
        GameObject sl = new GameObject();
        sl.AddComponent<SelectLevel>();
        SelectLevel.ins.preselectedSongName = songName;
        SceneManager.LoadScene(1, LoadSceneMode.Additive);
    }
}
