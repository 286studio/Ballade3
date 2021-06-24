using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum LevelEventType
{
    Note, Meta
}
public class EditorEvent : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] Button btn;
    public LevelEventType eventType;
    public KeyData kd;
    [HideInInspector] public EditorEvent mirror;
    [HideInInspector] public bool dragging;
    public float pointerDownStartTime = -1;
    Image check;
    public RectTransform rt;
    private void OnValidate()
    {
        rt = GetComponent<RectTransform>();
    }
    private void Start()
    {
        check = Instantiate(LevelPage.ins.check, transform).GetComponent<Image>();
        check.enabled = false;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Pressed();
        }
    }
    void Pressed()
    {
        pointerDownStartTime = Time.time;
        if (Utils.ControlKeyHeldDown()) LevelPage.SelectEvent(this, true);
        else if (Utils.AltKeyHeldDown()) LevelPage.DeselectEvent(this);
        else LevelPage.SelectEvent(this, false);
        RefreshSelectedState();
    }
    public void DragEnd()
    {
        dragging = false;
    }
    public void RefreshSelectedState()
    {
        if (eventType == LevelEventType.Meta)
            mirror.check.enabled = check.enabled = LevelPage.IsSelected(this) || LevelPage.IsSelected(mirror);
        else check.enabled = LevelPage.IsSelected(this);
    }
}
