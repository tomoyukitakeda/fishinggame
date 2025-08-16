using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FishPediaEntry
{
    public string id;          // FishTypeSO.PersistentId
  
    public int caughtCount;    // ���ۂɒނ�����
   
    public bool unlocked;      // 1��ł�������/�ނ��� ��true
    public string firstCaughtIsoUtc; // ����ߊl����
}

[Serializable]
public class FishPediaSave
{
    public List<FishPediaEntry> entries = new();
    public int version = 1;
}

public class FishPediaManager : MonoBehaviour
{
    [Header("�}�ӑΏہi���я��ɂ��g����j")]
    [SerializeField] private List<FishTypeSO> allFishTypes = new();

    private readonly Dictionary<string, FishPediaEntry> _map = new();
    public event Action<FishTypeSO, FishPediaEntry> OnEntryUpdated;

    private const string SaveKey = "FishPediaSave_v1";

    private void Awake()
    {
        Load();
        // allFishTypes�ɂ��邪�Z�[�u�ɂȂ���ނ̌�����
        foreach (var t in allFishTypes)
        {
            var id = t.PersistentId;
            if (string.IsNullOrEmpty(id)) continue;
            if (!_map.ContainsKey(id))
                _map[id] = new FishPediaEntry { id = id, caughtCount = 0, unlocked = false };
        }
    }

    public IReadOnlyList<FishTypeSO> AllTypes => allFishTypes;

    public FishPediaEntry GetEntry(FishTypeSO type)
    {
        if (type == null || string.IsNullOrEmpty(type.PersistentId)) return null;
        _map.TryGetValue(type.PersistentId, out var e);
        return e;
    }

   

    public void ReportCaught(FishTypeSO type, float size)
    {
        if (type == null) return;
        var e = Ensure(type);
        e.caughtCount++;
        e.unlocked = true;
    
        if (string.IsNullOrEmpty(e.firstCaughtIsoUtc))
            e.firstCaughtIsoUtc = DateTime.UtcNow.ToString("o");
        OnEntryUpdated?.Invoke(type, e);
        Save();
    }

    private FishPediaEntry Ensure(FishTypeSO type)
    {
        var id = type.PersistentId;
        if (!_map.TryGetValue(id, out var e))
        {
            e = new FishPediaEntry { id = id };
            _map[id] = e;
        }
        return e;
    }

    private void Save()
    {
        var data = new FishPediaSave { entries = new List<FishPediaEntry>(_map.Values) };

#if ES3 // �� Easy Save ������΂�����iScripting Define Symbols �� ES3 ��ǉ�����Ɛؑ։j
        ES3.Save(SaveKey, JsonUtility.ToJson(data));
#else
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
#endif
    }

    private void Load()
    {
#if ES3
        if (ES3.KeyExists(SaveKey))
        {
            var json = ES3.Load<string>(SaveKey);
            ApplyJson(json);
        }
#else
        if (PlayerPrefs.HasKey(SaveKey))
        {
            var json = PlayerPrefs.GetString(SaveKey);
            ApplyJson(json);
        }
#endif
    }

    private void ApplyJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var data = JsonUtility.FromJson<FishPediaSave>(json);
        _map.Clear();
        if (data?.entries != null)
            foreach (var e in data.entries)
                _map[e.id] = e;
    }
}
