using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackToggle : MonoBehaviour
{
    [HideInInspector] public int trackNum;
    public void OnValueChanged(bool val)
    {
        MidiPage.trackOfbars[trackNum].SetActive(val);
    }
}
