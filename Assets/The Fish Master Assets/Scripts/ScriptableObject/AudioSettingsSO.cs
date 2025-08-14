using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Audio Settings", fileName = "AudioSettings")]
public class AudioSettingsSO : ScriptableObject
{
    [Header("AudioMixer�i�C�Ӂj")]
    public AudioMixer mixer; // �Ȃ��Ă�OK

    [Header("�����{�����[���i0..1�j")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Range(0f, 1f)] public float uiVolume = 0.9f;

    // Mixer��Exposed Parameter������ꍇ�̖��O�i�C�Ӂj
    public string masterParam = "MasterVol";
    public string musicParam = "MusicVol";
    public string sfxParam = "SFXVol";
    public string uiParam = "UIVol";
}
