using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Audio Settings", fileName = "AudioSettings")]
public class AudioSettingsSO : ScriptableObject
{
    [Header("AudioMixer（任意）")]
    public AudioMixer mixer; // なくてもOK

    [Header("初期ボリューム（0..1）")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Range(0f, 1f)] public float uiVolume = 0.9f;

    // MixerにExposed Parameterがある場合の名前（任意）
    public string masterParam = "MasterVol";
    public string musicParam = "MusicVol";
    public string sfxParam = "SFXVol";
    public string uiParam = "UIVol";
}
