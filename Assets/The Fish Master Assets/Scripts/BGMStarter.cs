using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMStarter : MonoBehaviour
{
    [SerializeField] private AudioCueSO bgmCue;

    void Start()
    {
        // Clips‚É•¡”‹È“ü‚Á‚Ä‚¢‚éAudioCueSO‚ğw’è
        AudioManager.Instance.PlayBGMWithAutoChange(bgmCue);
    }
}
