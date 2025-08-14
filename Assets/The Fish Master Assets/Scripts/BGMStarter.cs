using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMStarter : MonoBehaviour
{
    [SerializeField] private AudioCueSO bgmCue;

    void Start()
    {
        // Clips�ɕ����ȓ����Ă���AudioCueSO���w��
        AudioManager.Instance.PlayBGMWithAutoChange(bgmCue);
    }
}
