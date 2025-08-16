using System.Collections.Generic;
using UnityEngine;

public class FishPediaUI : MonoBehaviour
{
    [SerializeField] private FishPediaManager manager;
    [SerializeField] private Transform contentParent;
    [SerializeField] private FishPediaItemUI itemPrefab;

    [Header("Detail")]
    [SerializeField] private FishPediaDetailUI detailUI; // Åö í«â¡

    private readonly Dictionary<string, FishPediaItemUI> _items = new();

    private void Start()
    {
        RebuildAll();
        manager.OnEntryUpdated += HandleUpdated;
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnEntryUpdated -= HandleUpdated;
    }

    private void RebuildAll()
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);
        _items.Clear();

        foreach (var type in manager.AllTypes)
        {
            var item = Instantiate(itemPrefab, contentParent);
            // Åö è⁄ç◊ÇäJÇ≠ÉnÉìÉhÉâÇíçì¸
            item.Init((t, e) =>
            {
                if (detailUI != null) detailUI.Show(t, e);
            });

            var e = manager.GetEntry(type);
            item.Bind(type, e);
            _items[type.PersistentId] = item;
        }
    }

    private void HandleUpdated(FishTypeSO type, FishPediaEntry entry)
    {
        if (_items.TryGetValue(type.PersistentId, out var ui))
            ui.Bind(type, entry);
    }
}
