using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public class Interpreter
{
    public static Platform platform;
    public static void Open(string filename, out List<KeyData> keyData, out Dictionary<string, Dictionary<string, string>> sections)
    {
        keyData = new List<KeyData>();
        sections = new Dictionary<string, Dictionary<string, string>>();
        TextAsset[] tas = Resources.LoadAll<TextAsset>(platform + "/" + filename);
        for (int i = 0; i < tas.Length; ++i)
        {
            bool isSection = true;
            string curSec = "";
            string[] lines = Regex.Split(tas[i].text, "\n|\r|\r\n");
            Dictionary<string, float> vars = new Dictionary<string, float>();
            foreach (var line in lines)
            {
                // remove comment
                string nocom = line.Split(';')[0].Trim();
                if (nocom.Length > 0)
                {
                    if (nocom[0] == '[') // section begins
                    {
                        bool needAddition = false;
                        bool needStoring = false;
                        string varName = "";
                        string curTimeString = curSec = nocom.Substring(1, line.Length - 2);
                        string[] seg = curSec.Split('=');
                        if (seg.Length > 1)
                        {
                            varName = seg[0];
                            curTimeString = seg[1];
                            needStoring = true;
                        }
                        seg = curSec.Split('+');
                        if (seg.Length > 1)
                        {
                            varName = seg[0];
                            curTimeString = seg[1];
                            needAddition = true;
                        }
                        float curTime;
                        if (float.TryParse(curTimeString, out curTime))
                        {
                            if (needStoring)
                            {
                                Debug.Log("Detect Variable " + varName + "=" + curTimeString);
                                vars.Add(varName, curTime);
                            }
                            KeyData kd = new KeyData(curTime + (needAddition ? vars[varName] : 0), tas[i].name);
                            keyData.Add(kd);
                            isSection = false;
                        }
                        else
                        {
                            sections.Add(curSec, new Dictionary<string, string>());
                            isSection = true;
                        }
                    }
                    else
                    {
                        var pair = line.Split('=');
                        (isSection ? sections[curSec] : keyData[keyData.Count - 1].prop).Add(pair[0], pair[1]);
                    }
                }
            }
        }
    }

    public static void Save(string filename, in List<KeyData> keyData)
    {

    }
}
