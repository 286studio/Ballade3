using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RulerClickable : MonoBehaviour, IPointerDownHandler // 继承这个被点击Interface
{
    public EditingPage page;
    SelectPad selectPad;
    bool dragging;
    float x1, x2;
    public void OnPointerDown(PointerEventData eventData)
    {
        // 被点击不松开
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!dragging) DragStart();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        selectPad = MusicalLevelEditor.ins.selectPads[(int)page];
    }

    // Update is called once per frame
    void Update()
    {
        if (dragging)
        {
            if (Input.GetMouseButton(0))
            {
                x2 = MusicalLevelEditor.ins.mousePositionInScroll[(int)page].x;
                selectPad.SetRange(MusicalLevelEditor.ins.PagePosXToTime(page, x1), MusicalLevelEditor.ins.PagePosXToTime(page, x2));
                float mousePos = Utils.ScreenToCanvasPos(Input.mousePosition).x;
                float scrollSize = (MusicalLevelEditor.ins.scrolls[(int)page].transform as RectTransform).sizeDelta.x;
                if (mousePos > scrollSize / 2) MusicalLevelEditor.ins.ScrollMoveRight(page);
                if (mousePos < -scrollSize / 2) MusicalLevelEditor.ins.ScrollMoveLeft(page);
            }
            else DragEnd();
        }
    }

    void DragStart()
    {
        dragging = true;
        MusicalLevelEditor.ins.scrolls[(int)page].horizontal = false;
        if (page == EditingPage.Midi) MusicalLevelEditor.ins.scrolls[(int)page].vertical = false;
        selectPad.Interactable = false;
        x1 = MusicalLevelEditor.ins.mousePositionInScroll[(int)page].x;
    }
    void DragEnd()
    {
        dragging = false;
        MusicalLevelEditor.ins.scrolls[(int)page].horizontal = true;
        if (page == EditingPage.Midi) MusicalLevelEditor.ins.scrolls[(int)page].vertical = true;
        selectPad.Interactable = true;
        selectPad.OnSelectButtonClicked();
    }
}
