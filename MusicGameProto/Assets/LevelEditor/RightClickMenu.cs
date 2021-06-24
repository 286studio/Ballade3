using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RightClickMenu : MonoBehaviour
{
    [SerializeField] Dropdown eventDP;
    [SerializeField] InputField timeIF;
    [SerializeField] Button doneBTN;
    [SerializeField] Button closeBTN;
    public RectTransform rt;
    private void OnValidate()
    {
        rt = GetComponent<RectTransform>();
    }
    void Start()
    {
        eventDP.ClearOptions();
        for (int i = 0; i < LevelPage.chineseMetaEvent.Length; ++i) eventDP.options.Add(new Dropdown.OptionData(LevelPage.chineseMetaEvent[i]));
        timeIF.text = MusicalLevelEditor.ins.PagePosXToTime(EditingPage.Panel_Left, MusicalLevelEditor.ins.mousePositionInScroll[1].x).ToString();
        doneBTN.onClick.AddListener(Done);
        closeBTN.onClick.AddListener(delegate { Destroy(gameObject); });
    }
    void Done()
    {
        float timeval = float.Parse(timeIF.text);
        KeyData newkd = new KeyData(timeval);
        newkd.prop[EventTags.Type] = EventTypes.Meta[eventDP.value];
        LevelPage.ins.keyData.Add(newkd);
        LevelPage.ins.MetaEventShow(newkd, LevelPage.metaEventColor[eventDP.value], LevelPage.chineseMetaEvent[eventDP.value], timeval, LevelPage.metaEventLengths[eventDP.value]);
        Destroy(gameObject);
    }
}
