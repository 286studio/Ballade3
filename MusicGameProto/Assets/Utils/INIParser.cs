using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class INIParser
{
    string filename;
    Dictionary<string, Dictionary<string, string>> Content;
    string curSection;

    public INIParser(string _filename, bool isResource = true)
    {
        LoadFile(_filename, isResource);
    }
    public void LoadFile(string _filename, bool isResource = true)
    {
        filename = _filename;
        Content = new Dictionary<string, Dictionary<string, string>>();
        string[] lines;
        if (isResource)
        {
            var file = Resources.Load<TextAsset>(filename);
            lines = file.text.Split("\r\n".ToCharArray());
        }
        else
        {
            lines = File.ReadAllLines(_filename);
        }
        string curSec = "NoSection";
        foreach (var line in lines)
        {
            // remove comment
            string nocom = line.Split(';')[0].Trim();
            if (nocom.Length > 0)
            {
                if (nocom[0] == '[') // section begins
                {
                    curSec = nocom.Substring(1, line.Length - 2);
                    Content.Add(curSec, new Dictionary<string, string>());
                }
                else
                {
                    var pair = line.Split('=');
                    Content[curSec].Add(pair[0], pair[1]);
                }
            }
        }
    }

    public void SetSection(string section)
    {
        curSection = section;
    }

    public bool TryGetValue(string section, string key, out string value)
    {
        SetSection(section);
        return TryGetValue(key, out value);
    }
    public bool TryGetValue(string key, out string value)
    {
        Dictionary<string, string> pair;
        if (Content.TryGetValue(curSection, out pair))
        {
            if (pair.TryGetValue(key, out value))
            {
                return true;
            }
        }
        value = string.Empty;
        return false;
    }
    public void Close()
    {
        filename = "";
        Content.Clear();
    }
}
