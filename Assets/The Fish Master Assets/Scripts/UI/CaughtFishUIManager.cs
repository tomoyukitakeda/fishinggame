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

    // ★ このルートごと表示/非表示にしたい（ScrollRectやパネル）
    [SerializeField] private GameObject root;
    // 高速更新用のマップ
    private readonly Dictionary<FishTypeSO, CaughtFishItemUI> _uiMap = new();

    private void Awake()
    {
        // ここで購読（パネルが非表示でも受け取れる）
        inventory.OnAddedOrUpdated += HandleAddedOrUpdated;
        inventory.OnCleared += HandleCleared;
    }
    private void Start()
    {
        RebuildAll();
        UpdateRootVisibility(); // 在庫に応じて自動ON/OFF
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
    // ★ 釣果がない時は自動で非表示にする
    private void UpdateRootVisibility()
    {
        if (!root) return;
        bool hasAny = inventory.TotalCount > 0;   // ★ 総数で判定
        Debug.Log("hasany" + hasAny);

        if (root.activeSelf != hasAny)            // ★ 余計な再設定を避けるガード
            root.SetActive(hasAny);
    }

  
}
