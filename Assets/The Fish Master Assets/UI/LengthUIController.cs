using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LengthUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider lengthSlider;     // 表示専用
    [SerializeField] private TMP_Text lengthLabel;    // "Depth: xx m"
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("Settings")]
    [SerializeField] private int stepDepth = 5;
    [SerializeField] private int minDepth = 20;       // 最小20m固定
    [SerializeField] private bool saveToPlayerPrefs = true;

    // （押しっぱなし設定を外から微調整したい場合に公開）
    [Header("Hold Settings (optional)")]
    [SerializeField] private float holdInitialDelay = 0.35f;
    [SerializeField] private float holdRepeatInterval = 0.08f;
    [SerializeField] private bool holdUseAcceleration = true;
    [SerializeField] private float holdIntervalMultiplier = 0.92f;
    [SerializeField] private float holdMinRepeatInterval = 0.03f;

    public int SelectedDepth { get; private set; }

    private int _maxDepth = 20;
    private bool _initialized = false;

    private const string KEY_SELECTED_DEPTH = "LengthUI.SelectedDepth";

    private void Awake()
    {
        if (!lengthLabel) lengthLabel = GetComponentInChildren<TMP_Text>(true);
        if (!lengthSlider) lengthSlider = GetComponentInChildren<Slider>(true);
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        SetupUI();
        WireHoldRepeat();   // ← 追加
        UpdateAll();
    }

    private void Start()
    {
        InitializeIfNeeded();
        SetupUI();

        UpdateAll();
    }

    private void InitializeIfNeeded()
    {
        RefreshMaxDepthFromIdle();
        if (_initialized) return;

        int defaultDepth = LoadSelectedDepthOrDefault(_maxDepth);
        SelectedDepth = Mathf.Clamp(defaultDepth, minDepth, _maxDepth);
        _initialized = true;
    }

    public void RefreshMaxDepthFromIdle()
    {
        _maxDepth = (IdleManager.instance != null)
            ? Mathf.Max(minDepth, IdleManager.instance.CurrentLength)
            : Mathf.Max(minDepth, 20);

        if (_initialized)
        {
            SelectedDepth = Mathf.Clamp(SelectedDepth, minDepth, _maxDepth);
        }
    }

    private void SetupUI()
    {
        if (lengthSlider)
        {
            lengthSlider.minValue = minDepth;
            lengthSlider.maxValue = _maxDepth;
            lengthSlider.wholeNumbers = true;
            lengthSlider.value = SelectedDepth;
            lengthSlider.interactable = false;

            var g = lengthSlider.GetComponentInChildren<Graphic>(true);
            if (g) g.raycastTarget = false;
        }

        if (plusButton)
        {
            plusButton.onClick.RemoveListener(OnPlus);
            plusButton.onClick.AddListener(OnPlus);
        }
        if (minusButton)
        {
            minusButton.onClick.RemoveListener(OnMinus);
            minusButton.onClick.AddListener(OnMinus);
        }
    }

    // ★ 押しっぱなしの紐づけ
    private void WireHoldRepeat()
    {
        SetupHold(plusButton, OnPlus);
        SetupHold(minusButton, OnMinus);
    }

    private void SetupHold(Button btn, System.Action stepAction)
    {
        if (!btn) return;

        var hold = btn.GetComponent<PressAndHoldButton>();
        if (!hold) hold = btn.gameObject.AddComponent<PressAndHoldButton>();

        // ★ ここで null ガード（Editor/Runtime 両対応の保険）
        if (hold.onRepeat == null) hold.onRepeat = new UnityEvent();

        hold.initialDelay = holdInitialDelay;
        hold.repeatInterval = holdRepeatInterval;
        hold.useAcceleration = holdUseAcceleration;
        hold.intervalMultiplierPerStep = holdIntervalMultiplier;
        hold.minRepeatInterval = holdMinRepeatInterval;

        // いったん既存のリスナーをクリアして重複防止
        hold.onRepeat.RemoveAllListeners();
        hold.onRepeat.AddListener(() => stepAction());
    }

    private void UpdateAll()
    {
        if (lengthSlider) lengthSlider.value = SelectedDepth;
        if (lengthLabel) lengthLabel.text = $"Depth: {SelectedDepth} m";
    }

    private void OnPlus() => SetDepth(SelectedDepth + stepDepth);
    private void OnMinus() => SetDepth(SelectedDepth - stepDepth);

    public void SetDepth(int meters)
    {
        int clamped = Mathf.Clamp(meters, minDepth, _maxDepth);
        if (clamped == SelectedDepth) return;

        SelectedDepth = clamped;
        UpdateAll();
        SaveSelectedDepthIfNeeded();
    }

    public void SetInteractable(bool interactable)
    {
        if (plusButton) plusButton.interactable = interactable;
        if (minusButton) minusButton.interactable = interactable;
        // スライダーは常に非インタラクティブ
    }

    private int LoadSelectedDepthOrDefault(int fallback)
    {
        if (!saveToPlayerPrefs) return fallback;
        return PlayerPrefs.HasKey(KEY_SELECTED_DEPTH)
            ? PlayerPrefs.GetInt(KEY_SELECTED_DEPTH)
            : fallback;
    }

    private void SaveSelectedDepthIfNeeded()
    {
        if (!saveToPlayerPrefs) return;
        PlayerPrefs.SetInt(KEY_SELECTED_DEPTH, SelectedDepth);
        PlayerPrefs.Save();
    }
}
