using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiSelectPanel : MonoBehaviour
{
    [SerializeField] Button mergeBTN;
    [SerializeField] Button deleteAllBTN;
    private void Start()
    {
        mergeBTN.onClick.AddListener(OnMergeButtonClicked);
        deleteAllBTN.onClick.AddListener(LevelPage.DeleteSelected);
    }

    private void OnMergeButtonClicked()
    {
        Instantiate(LevelPage.ins.mergePagePrefab, MusicalLevelEditor.ins.transform).GetComponent<MergePage>();
    }
    public void Refresh()
    {
        mergeBTN.interactable = LevelPage.SelectionContainsOnlyFallingBlock();
        mergeBTN.GetComponentInChildren<Text>().text = mergeBTN.interactable ? "合并..." : "无法合并";
    }
}
