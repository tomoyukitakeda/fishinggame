using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.UI;

public class CaughtFishUIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CaughtFishInventoryMB inventory;
    [SerializeField] private Transform contentParent;
    [SerializeField] private CaughtFishItemUI itemPrefab;

    // �� ���̃��[�g���ƕ\��/��\���ɂ������iScrollRect��p�l���j
    [SerializeField] private GameObject root;
    // �����X�V�p�̃}�b�v
    private readonly Dictionary<FishTypeSO, CaughtFishItemUI> _uiMap = new();

    private void Awake()
    {
        // �����ōw�ǁi�p�l������\���ł��󂯎���j
        inventory.OnAddedOrUpdated += HandleAddedOrUpdated;
        inventory.OnCleared += HandleCleared;
    }
    private void Start()
    {
        RebuildAll();
        UpdateRootVisibility(); // �݌ɂɉ����Ď���ON/OFF
    }
    private void OnDestroy()
    {
        inventory.OnAddedOrUpdated -= HandleAddedOrUpdated;
        inventory.OnCleared -= HandleCleared;
    }


  
    private void RebuildAll()
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        _uiMap.Clear();

        foreach (var e in inventory.Entries)
        {
            var ui = Instantiate(itemPrefab, contentParent);
            ui.Set(e.type, e.count);
            _uiMap[e.type] = ui;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentParent);
        Canvas.ForceUpdateCanvases();
    }

    private void HandleAddedOrUpdated(FishTypeSO type, int count)
    {
        Debug.Log($"[UI] HandleAddedOrUpdated {type.name} -> {count}");
        if (_uiMap.TryGetValue(type, out var ui))
        {
            ui.UpdateCount(count);
        }
        else
        {
            var newUI = Instantiate(itemPrefab, contentParent);
            newUI.Set(type, count);
            _uiMap[type] = newUI;
        }
        UpdateRootVisibility();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentParent);
        Canvas.ForceUpdateCanvases();
    }

    private void HandleCleared()
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        _uiMap.Clear();
        UpdateRootVisibility();
    }
    // �� �މʂ��Ȃ����͎����Ŕ�\���ɂ���
    private void UpdateRootVisibility()
    {
        if (!root) return;
        bool hasAny = inventory.TotalCount > 0;   // �� �����Ŕ���
        Debug.Log("hasany" + hasAny);

        if (root.activeSelf != hasAny)            // �� �]�v�ȍĐݒ�������K�[�h
            root.SetActive(hasAny);
    }

  
}
