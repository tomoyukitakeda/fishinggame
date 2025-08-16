using System;
using System.Collections.Generic;
using UnityEngine;

public class CaughtFishInventoryMB : MonoBehaviour
{
    public class Entry { public FishTypeSO type; public int count; }

    // UI更新用イベント（差分通知）
    public event Action<FishTypeSO, int> OnAddedOrUpdated;
    public event Action OnCleared;

    // ランタイム管理
    private readonly Dictionary<FishTypeSO, int> _counts = new();
    private readonly List<Entry> _entriesCache = new();

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (var kv in _counts) total += kv.Value;
            return total;
        }
    }

    public IReadOnlyList<Entry> Entries
    {
        get
        {
            _entriesCache.Clear();
            foreach (var kv in _counts) _entriesCache.Add(new Entry { type = kv.Key, count = kv.Value });
            return _entriesCache;
        }
    }

    public void Add(FishTypeSO type, int amount = 1)
    {
        if (!type || amount <= 0) return;
        _counts.TryGetValue(type, out var c);
        c += amount;
        _counts[type] = c;
        OnAddedOrUpdated?.Invoke(type, c);
    }

    public void ClearAll()
    {
        _counts.Clear();
        OnCleared?.Invoke();
    }
}
