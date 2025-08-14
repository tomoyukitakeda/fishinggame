using UnityEngine;
using UnityEngine.Audio;

public enum AudioBus { Master, Music, SFX, UI }

[CreateAssetMenu(menuName = "Audio/Audio Cue", fileName = "AudioCue")]
public class AudioCueSO : ScriptableObject
{
    [Header("Clips�i�������烉���_���I���j")]
    public AudioClip[] clips;

    [Header("�o�͐�o�X")]
    public AudioBus bus = AudioBus.SFX;

    [Header("���ʁE�s�b�`�i�����_���͈́j")]
    [Range(0f, 1f)] public float volumeMin = 0.9f;
    [Range(0f, 1f)] public float volumeMax = 1.0f;
    [Range(-3f, 3f)] public float pitchMin = 0.98f;
    [Range(-3f, 3f)] public float pitchMax = 1.02f;

    [Header("�Đ��ݒ�")]
    public bool loop = false;           // BGM�Ȃ�
    [Range(0f, 1f)] public float spatialBlend = 0f; // 0=2D, 1=3D
    [Range(0f, 5f)] public float dopplerLevel = 0f;
    public float spread = 0f;
    public float minDistance = 1f;
    public float maxDistance = 25f;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public float GetRandomVolume() => Random.Range(volumeMin, volumeMax);
    public float GetRandomPitch() => Random.Range(pitchMin, pitchMax);
}
