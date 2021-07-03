using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectPad : MonoBehaviour
{
    [SerializeField] InputField startTimeIF;
    [SerializeField] InputField endTimeIF;
    [SerializeField] Button selectButton;
    [SerializeField] Button selectAll;
    [SerializeField] Button unselectAll;
    public EditingPage page;
    Image selectionBox;
    bool canClick;
    float startTime, endTime;
    void Start()
    {
        startTimeIF.onValueChanged.AddListener(OnValueChanged);
        endTimeIF.onValueChanged.AddListener(OnValueChanged);
        selectButton.onClick.AddListener(OnSelectButtonClicked);
        selectAll.onClick.AddListener(OnSelectAll);
        unselectAll.onClick.AddListener(OnUnselectAll);
    }

    private void OnSelectAll()
    {
        SetRange(0, MidiTranslator.endTime);
    }
    private void OnUnselectAll()
    {
        SetRange(-1, -1);
        if (selectionBox) Destroy(selectionBox.gameObject);
        canClick = false;
    }
    void OnValueChanged(string val)
    {
        if (!float.TryParse(startTimeIF.text, out startTime) || !float.TryParse(endTimeIF.text, out endTime) || startTime >= endTime || startTime < 0 || endTime < 0)
        {
            if (selectionBox) Destroy(selectionBox.gameObject);
            canClick = false;
            return;
        }
        canClick = true;
        if (!selectionBox)
        {
            selectionBox = Instantiate(MusicalLevelEditor.ins.barImage, MusicalLevelEditor.ins.contents[(int)page]).GetComponent<Image>();
            selectionBox.color = new Color32(73, 174, 190, 60);
            selectionBox.name = "SelectBox";
            selectionBox.raycastTarget = false;
        }
        selectionBox.rectTransform.anchoredPosition = new Vector2(MusicalLevelEditor.ins.TimeToPagePosX(page, (startTime + endTime) / 2), 0);
        float lengthPerSec = page == EditingPage.Midi ? MidiPage.lengthPerSec : LevelPage.lengthPerSec;
        float contentHeight = page == EditingPage.Midi ? MidiPage.ContentHeight : LevelPage.ContentHeight;
        selectionBox.rectTransform.sizeDelta = new Vector2(lengthPerSec * (endTime - startTime), contentHeight);
    }
    public void OnSelectButtonClicked()
    {
        if (!canClick) return;
        switch (page)
        {
            case EditingPage.Midi:
                MidiPage.SelectRange(startTime, endTime);
                break;
            case EditingPage.Panel_Left:
                LevelPage.SelectRangeLeft(startTime, endTime);
                break;
            case EditingPage.Panel_Right:
                LevelPage.SelectRangeRight(startTime, endTime);
                break;
        }
    }

    public void SetRange(float t1, float t2)
    {
        if (t1 > t2)
        {
            startTimeIF.text = t2.ToString();
            endTimeIF.text = t1.ToString();
        }
        else if (t1 < t2)
        {
            startTimeIF.text = t1.ToString();
            endTimeIF.text = t2.ToString();
        }
        else
        {
            startTimeIF.text = endTimeIF.text = "0";
        }
    }
    public bool Interactable
    {
        get => selectButton.interactable;
        set
        {
            selectButton.interactable = startTimeIF.interactable = endTimeIF.interactable = value;
        }
    }
}
