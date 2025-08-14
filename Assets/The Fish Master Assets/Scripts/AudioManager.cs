using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [Header("設定")]
    [SerializeField] private AudioSettingsSO settings;

    [Header("BGM用（クロスフェード）")]
    [SerializeField] private float defaultFadeSeconds = 0.5f;
    private AudioSource _bgmA, _bgmB;
    private bool _usingA = true;

    [Header("SFXプール")]
    [SerializeField] private int sfxPoolSize = 16;
    [SerializeField] private PooledAudioSource pooledPrefab; // 空オブジェクト+AudioSource付きのPrefab
    private readonly Queue<PooledAudioSource> _pool = new();
    private readonly HashSet<PooledAudioSource> _inUse = new();

    private AudioCueSO _currentBgmCue; // 今流してるAudioCueSOを記憶
    private bool _autoBgmChange = false;

    const string PREF_MASTER = "VOL_MASTER";
    const string PREF_MUSIC = "VOL_MUSIC";
    const string PREF_SFX = "VOL_SFX";
    const string PREF_UI = "VOL_UI";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGMダミー作成
        _bgmA = CreateChild("BGM_A").AddComponent<AudioSource>();
        _bgmB = CreateChild("BGM_B").AddComponent<AudioSource>();
        foreach (var s in new[] { _bgmA, _bgmB })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D
        }

        // SFXプール作成
        for (int i = 0; i < sfxPoolSize; i++)
            _pool.Enqueue(Instantiate(pooledPrefab, transform));

        LoadVolumes();
        ApplyMixerVolumes();
    }

    Transform CreateChild(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        return go.transform;
    }

    //==================== 再生API ====================

    public void PlayBGM(AudioCueSO cue, float fadeSeconds = -1f)
    {
        if (cue == null) return;
        var clip = cue.GetRandomClip();
        if (!clip) return;

        var fade = fadeSeconds < 0 ? defaultFadeSeconds : fadeSeconds;

        // フェード先/元を決定
        var srcFrom = _usingA ? _bgmA : _bgmB;
        var srcTo = _usingA ? _bgmB : _bgmA;
        _usingA = !_usingA;

        // 設定
        srcTo.clip = clip;
        srcTo.volume = 0f;
        srcTo.pitch = cue.GetRandomPitch();
        srcTo.loop = true;
        srcTo.Play();

        // クロスフェード
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(srcFrom, srcTo, fade));
    }

    public void StopBGM(float fadeSeconds = -1f)
    {
        var fade = fadeSeconds < 0 ? defaultFadeSeconds : fadeSeconds;
        var src = _usingA ? _bgmA : _bgmB;
        StartCoroutine(FadeOutAndStop(src, fade));
    }
    public PooledAudioSource PlaySFX(AudioCueSO cue, Vector3 position, bool returnHandle)
    {
        return PlayOneShotInternalWithHandle(cue, position, force2D: false);
    }

    public void PlaySFX(AudioCueSO cue, Vector3 position)
    {
        PlayOneShotInternal(cue, position, force2D: false);
    }
    public PooledAudioSource PlayUISFX(AudioCueSO cue, bool returnHandle)
    {
        return PlayOneShotInternalWithHandle(cue, Vector3.zero, force2D: true);
    }

    public void PlayUISFX(AudioCueSO cue)
    {
        PlayOneShotInternal(cue, Vector3.zero, force2D: true);
    }// ← 追加：途中停止（即止め）
    public void StopSFX(PooledAudioSource handle)
    {
        if (handle == null) return;
        var src = handle.Source;
        if (src && src.isPlaying) src.Stop();
        Return(handle); // プールに返却
    }


    // 将来Addressables対応する場合はここを差し替えればOK
    private void PlayOneShotInternal(AudioCueSO cue, Vector3 pos, bool force2D)
    {
        _ = PlayOneShotInternalWithHandle(cue, pos, force2D); // 破棄
    }

    //==================== 音量 ====================
    private PooledAudioSource PlayOneShotInternalWithHandle(AudioCueSO cue, Vector3 pos, bool force2D)
    {
        if (cue == null) return null;
        var clip = cue.GetRandomClip();
        if (!clip) return null;

        var src = Borrow();
        var t = src.transform;
        t.position = pos;

        var spatial = force2D ? 0f : cue.spatialBlend;

        src.Play(
            clip,
            cue.GetRandomVolume() * GetBusVolumeScalar(cue.bus),
            cue.GetRandomPitch(),
            spatial,
            cue.loop,
            cue.dopplerLevel,
            cue.spread,
            cue.minDistance,
            cue.maxDistance,
            Return // 再生完了時にプールへ返却
        );

        src.Source.spatialize = spatial > 0f;
        return src;
    }
    public void SetVolume(AudioBus bus, float value) // 0..1
    {
        value = Mathf.Clamp01(value);
        switch (bus)
        {
            case AudioBus.Master: PlayerPrefs.SetFloat(PREF_MASTER, value); break;
            case AudioBus.Music: PlayerPrefs.SetFloat(PREF_MUSIC, value); break;
            case AudioBus.SFX: PlayerPrefs.SetFloat(PREF_SFX, value); break;
            case AudioBus.UI: PlayerPrefs.SetFloat(PREF_UI, value); break;
        }
        PlayerPrefs.Save();
        ApplyMixerVolumes();
    }

    public float GetVolume(AudioBus bus)
    {
        return bus switch
        {
            AudioBus.Master => PlayerPrefs.GetFloat(PREF_MASTER, settings ? settings.masterVolume : 1f),
            AudioBus.Music => PlayerPrefs.GetFloat(PREF_MUSIC, settings ? settings.musicVolume : 0.8f),
            AudioBus.SFX => PlayerPrefs.GetFloat(PREF_SFX, settings ? settings.sfxVolume : 0.9f),
            AudioBus.UI => PlayerPrefs.GetFloat(PREF_UI, settings ? settings.uiVolume : 0.9f),
            _ => 1f
        };
    }

    private float GetBusVolumeScalar(AudioBus bus)
    {
        // Mixer未使用時でもバスごとの実音量を反映
        var master = GetVolume(AudioBus.Master);
        var busVol = GetVolume(bus);
        return master * busVol;
    }

    private void LoadVolumes()
    {
        // 初回はsettingsの値を既定値として読むだけ（保存はSetVolume呼ばれるまでしない）
        _ = GetVolume(AudioBus.Master);
        _ = GetVolume(AudioBus.Music);
        _ = GetVolume(AudioBus.SFX);
        _ = GetVolume(AudioBus.UI);
    }
    void Update()
    {
        if (_autoBgmChange && _currentBgmCue != null)
        {
            var activeSrc = _usingA ? _bgmA : _bgmB;
            if (!activeSrc.isPlaying)
            {
                PlayBGM(_currentBgmCue); // 次のランダム曲へ
            }
        }
    }

    // 自動切り替えONで再生
    public void PlayBGMWithAutoChange(AudioCueSO cue)
    {
        _currentBgmCue = cue;
        _autoBgmChange = true;
        PlayBGM(cue);
    }

    // 自動切り替えOFF
    public void StopAutoBGMChange()
    {
        _autoBgmChange = false;
    }
    private void ApplyMixerVolumes()
    {
        if (settings == null || settings.mixer == null) return;

        // dB = 20 * log10(linear)（0は-80dB扱い）
        void Set(string param, float linear)
        {
            if (string.IsNullOrEmpty(param)) return;
            float db = (linear <= 0.0001f) ? -80f : 20f * Mathf.Log10(linear);
            settings.mixer.SetFloat(param, db);
        }

        Set(settings.masterParam, GetVolume(AudioBus.Master));
        Set(settings.musicParam, GetVolume(AudioBus.Music));
        Set(settings.sfxParam, GetVolume(AudioBus.SFX));
        Set(settings.uiParam, GetVolume(AudioBus.UI));
    }

    //==================== プール管理 ====================

    private PooledAudioSource Borrow()
    {
        if (_pool.Count == 0)
            _pool.Enqueue(Instantiate(pooledPrefab, transform));

        var src = _pool.Dequeue();
        _inUse.Add(src);
        src.gameObject.SetActive(true);
        return src;
    }

    private void Return(PooledAudioSource src)
    {
        if (src == null) return;
        src.gameObject.SetActive(false);
        _inUse.Remove(src);
        _pool.Enqueue(src);
    }

    //==================== コルーチン ====================

    private System.Collections.IEnumerator FadeRoutine(AudioSource from, AudioSource to, float sec)
    {
        float t = 0f;
        float fromStart = from ? from.volume : 0f;
        float toTarget = GetBusVolumeScalar(AudioBus.Music); // BGMはMusicバスの音量
        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = sec <= 0 ? 1f : t / sec;
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to) to.volume = Mathf.Lerp(0f, toTarget, k);
            yield return null;
        }
        if (from) { from.Stop(); from.volume = 0f; }
        if (to) { to.volume = toTarget; }
    }

    private System.Collections.IEnumerator FadeOutAndStop(AudioSource src, float sec)
    {
        if (!src || !src.isPlaying) yield break;
        float start = src.volume;
        float t = 0f;
        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = sec <= 0 ? 1f : t / sec;
            src.volume = Mathf.Lerp(start, 0f, k);
            yield return null;
        }
        src.Stop();
        src.volume = 0f;
    }
    private AudioMixerGroup GetGroup(AudioBus bus)
    {
        return bus switch
        {
            AudioBus.Master => masterGroup,
            AudioBus.Music => musicGroup,
            AudioBus.SFX => sfxGroup,
            AudioBus.UI => uiGroup,
            _ => masterGroup
        };
    }
}
