using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public Sprite[] UpNotes;
    public Sprite[] DownNotes;
    public static ResourceManager ins;
    private void Awake()
    {
        ins = this;
    }
}
