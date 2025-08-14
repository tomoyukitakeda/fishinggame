using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    [SerializeField] private Fish fishPrefab;

    [SerializeField] private FishTypeSO[] fishTypes; // �� SO �ɕύX

    private void Awake()
    {
        if (fishPrefab == null || fishTypes == null || fishTypes.Length == 0) return;

        // ���O�ɑS�̐��𐔂��āADOTween�e�ʂ𒆉��W���I�Ɋm�ہi�C�Ӂj
        int totalCount = 0;
        foreach (var t in fishTypes)
        {
            if (t == null) continue;
            totalCount += Mathf.Max(0, t.spawnCount);
        }
        // �]�T���������e�ʁiFish.Awake�ł���Ă���Ȃ�ȗ��^��d�ł��Q�͏��j
        DG.Tweening.DOTween.SetTweensCapacity(totalCount * 2 + 200, totalCount / 4 + 50);

        // ��ނ��Ƃ� spawnCount �C����
        foreach (var t in fishTypes)
        {
            if (t == null || t.spawnCount <= 0) continue;

            for (int i = 0; i < t.spawnCount; i++)
            {
                var fish = Instantiate(fishPrefab, transform); // �e��Spawner��
                fish.Type = t;                                  // ��ނ����蓖��
                fish.name = $"{t.fishName}_{i:000}";            // �f�o�b�O���₷��
                fish.ResetFish();                               // �ʒu&Tween������
            }
        }
    }
}
