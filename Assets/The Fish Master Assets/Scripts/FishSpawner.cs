using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FishSpawner : MonoBehaviour
{
    [SerializeField] private Fish fishPrefab;
    [SerializeField] private FishTypeSO[] fishTypes;
    [SerializeField] private float surfaceY = -10f; // ここで一元管理

    [Header("Spawn")]
    [SerializeField] private bool spawnFromBothSides = true;

    private float leftX, rightX;

    private void Awake()
    {
        if (!fishPrefab || fishTypes == null || fishTypes.Length == 0) return;

        // 画面端を一度だけ算出（Orthographic前提）
        var cam = Camera.main;
        var left = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f));
        var right = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f));
        leftX = left.x;
        rightX = right.x;

        // DOTween容量（任意）：総Tweens見積
        int totalCount = 0;
        foreach (var t in fishTypes) if (t) totalCount += Mathf.Max(0, t.spawnCount);
        DOTween.SetTweensCapacity(totalCount * 2 + 200, totalCount / 4 + 50);

        // 生成
        foreach (var t in fishTypes)
        {
            if (!t || t.spawnCount <= 0) continue;

            for (int i = 0; i < t.spawnCount; i++)
            {
                var fish = Instantiate(fishPrefab, transform);
                fish.Type = t;
                fish.name = $"{t.fishName}_{i:000}";

                // どちら側から出すか
                bool fromLeft = !spawnFromBothSides ? true : (Random.value < 0.5f);

                // 初期位置
                var pos = fish.transform.position;
                pos.x = fromLeft ? leftX : rightX;
                fish.transform.position = pos;

                // Fishへ画面端と水面Yを渡す（↓Fish側に追加したメソッド）
                fish.SetBounds(leftX, rightX, surfaceY);

                // 初期化（内部で向きflipXも決める）
                fish.ResetFish();
            }
        }
    }
}
