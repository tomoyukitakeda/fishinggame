using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PooledAudioSource : MonoBehaviour
{
    private AudioSource _src;
    private System.Action<PooledAudioSource> _onFinished;

    void Awake() => _src = GetComponent<AudioSource>();

    public void Play(AudioClip clip, float vol, float pitch, float spatialBlend, bool loop,
                     float doppler, float spread, float minDist, float maxDist,
                     System.Action<PooledAudioSource> onFinished)
    {
        _onFinished = onFinished;

        _src.clip = clip;
        _src.volume = vol;
        _src.pitch = pitch;
        _src.spatialBlend = spatialBlend;
        _src.loop = loop;
        _src.dopplerLevel = doppler;
        _src.spread = spread;
        _src.minDistance = minDist;
        _src.maxDistance = maxDist;

        _src.Play();

        if (!loop)
            StartCoroutine(ReturnWhenDone());
    }

    public void StopNow()
    {
        _src.Stop();
        _onFinished?.Invoke(this);
    }

    private IEnumerator ReturnWhenDone()
    {
        // Ä¶I—¹‚ð‘Ò‚Á‚Ä•Ô‹p
        while (_src != null && _src.isPlaying) yield return null;
        _onFinished?.Invoke(this);
    }

    public AudioSource Source => _src;
}
