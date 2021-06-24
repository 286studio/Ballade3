using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SectionNames
{
    public static string General = "General";
}

public static class GeneralTags
{
    public static string Exit = "Exit";
    public static string Music = "Music";
    public static string GameMode = "GameMode";
    public static string Delay = "Delay";
    public static string MusicStartPosition = "MusicStartPosition";
    public static string MidiTrack = "MidiTrack";
}

public static class EventTags
{
    // Midi
    public static string Exit = "Exit";
    public static string Panel = "Panel";
    public static string Type = "Type";
    public static string Color = "Color";
    public static string Note = "Note";
    public static string Delays = "Delays";
    public static string Position = "Position";
    public static string Lifetime = "Lifetime";
    public static string Length = "Length";
    public static string Width = "Width";
    public static string Direction = "Direction";
    public static string TimeLast = "TimeLast";
    public static string Rouxian = "Rouxian";
    public static string Roufa = "Roufa";
    public static string Rousu = "Rousu";
    public static string LimitTime = "LimitTime";
    public static string Cooldown = "Cooldown";
    public static string Text = "Text";
    public static string StartPosition = "StartPosition";
    public static string EndPosition = "EndPosition";
    public static string Rotation = "Rotation";
    public static string BeatInterval = "BeatInterval";
    public static string Voice = "Voice";
    public static string Num = "Num";
    public static string AnimColor = "AnimColor";
    public static string MaxMiss = "MaxMiss";
    public static string BeatLifetime = "BeatLifetime";
    public static string NoAnim = "NoAnim";
    // Meta
    public static string NewExitCount = "NewExitCount";
    public static string NewGameMode = "NewGameMode";
    public static string HideBottom = "HideBottom";
    public static string HideExits = "HideExits";
    public static string SpecialMode = "SpecialMode";
    public static string Enable3D = "Enable3D";
    public static string SetParam = "SetParam";
    public static string SFX = "SFX";
    public static string Image = "Image";
    public static string VideoVolumeFadeOut = "VideoVolumeFadeOut";
    public static string MusicFadeIn = "MusicFadeIn";
    public static string MusicFadeOut = "MusicFadeOut";
    public static string PreloadVideoClip = "PreloadVideoClip";
    public static string Ratio = "Ratio";
    public static string VideoStartTime = "VideoStartTime";
    public static string ShowPreloadVideo = "ShowPreloadVideo";
    public static string PlayVideoClip = "PlayVideoClip";
    public static string PanelAlpha = "PanelAlpha";

    static Dictionary<string, string> toChinese = new Dictionary<string, string>()
    {
        { Exit, "出口" }, { Panel, "谱面" }, { Type, "种类" }, { Color, "颜色" }, { Note, "包含音符值" }, { Delays, "各音符延迟" },
        { Length, "节数" }, { Direction, "朝向" }, { Width, "长度" }
    };
    public static string GetChinese(string tag)
    {
        string chn;
        if (toChinese.TryGetValue(tag, out chn)) return chn;
        else return tag;
    }
}

public static class EventTypes
{
    public static string None = "None";
    public static string FallingBlock = "FallingBlock";
    public static string Beat = "Beat";
    public static string LongFallingBlock = "LongFallingBlock";
    public static string HorizontalMove = "HorizontalMove";
    public static string Harp = "Harp";
    public static string VocalText = "VocalText";
    public static string Lyrics = "Lyrics";
    public static string ShowLeftPanel = "ShowLeftPanel";
    public static string HideLeftPanel = "HideLeftPanel";
    public static string ShowRightPanel = "ShowRightPanel";
    public static string HideRightPanel = "HideRightPanel";
    public static string HideBothPanels = "HideBothPanels";
    public static string GameOver = "GameOver";
    public static string[] Meta = new string[]
    {
        None, ShowLeftPanel, HideLeftPanel, ShowRightPanel, HideRightPanel, HideBothPanels, GameOver
    };
}
