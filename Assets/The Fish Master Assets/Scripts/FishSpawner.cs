using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    [SerializeField] private Fish fishPrefab;

    [SerializeField] private FishTypeSO[] fishTypes; // ← SO に変更

    private void Awake()
    {
        if (fishPrefab == null || fishTypes == null || fishTypes.Length == 0) return;

        // 事前に全体数を数えて、DOTween容量を中央集権的に確保（任意）
        int totalCount = 0;
        foreach (var t in fishTypes)
        {
            if (t == null) continue;
            totalCount += Mathf.Max(0, t.spawnCount);
        }
        // 余裕を持った容量（Fish.Awakeでやっているなら省略可／二重でも害は少）
        DG.Tweening.DOTween.SetTweensCapacity(totalCount * 2 + 200, totalCount / 4 + 50);

        // 種類ごとに spawnCount 匹生成
        foreach (var t in fishTypes)
        {
            if (t == null || t.spawnCount <= 0) continue;

            for (int i = 0; i < t.spawnCount; i++)
            {
                var fish = Instantiate(fishPrefab, transform); // 親をSpawnerに
                fish.Type = t;                                  // 種類を割り当て
                fish.name = $"{t.fishName}_{i:000}";            // デバッグしやすく
                fish.ResetFish();                               // 位置&Tween初期化
            }
        }
    }
}
