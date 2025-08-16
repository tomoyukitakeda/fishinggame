using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class IdleManager : MonoBehaviour
{
    // ---- Constants ----
    private const string KEY_LENGTH_LEVEL = "LengthLevel";
    private const string KEY_STRENGTH_LEVEL = "StrengthLevel";
    private const string KEY_OFFLINE_LEVEL = "OfflineLevel";
    private const string KEY_WALLET = "Wallet";
    private const string KEY_LAST_UTC = "LastUtcISO";

    private bool _didInitialCompute = false;
    private float _appStartRealtime;

    private bool _needShowReturn = false;
    private bool _hasShownReturn = false;

    [Header("Length Settings")]
    [SerializeField] private int baseLength = 10;   // 表示用の初期長
    [SerializeField] private int lengthStep = 5;   // 1レベルで増減する長さ



    [Header("Offline Settings")]
    [Tooltip("オフライン収益の単位は「分あたり」")]
    [SerializeField] private int maxOfflineMinutes = 24 * 60; // 上限: 24h


    [Header("UI Buttons")]
    [SerializeField] private Button BuyLengthsButton;
    [SerializeField] private Button BuyStrengthButton;
    [SerializeField] private Button BuyOfflineEarningsButton;

    // 公開（UIバインド用）
    [HideInInspector] public int lengthLevel;
    [HideInInspector] public int strengthLevel;
    [HideInInspector] public int offlineLevel;
    [HideInInspector] public int wallet;
     public int totalGain;

    [HideInInspector] public int lengthCost;
    [HideInInspector] public int strengthCost;
    [HideInInspector] public int offlineEarningsCost;

    // コストテーブル（レベル0→index0 と対応）
    [SerializeField]
    private int[] costs = new int[]
    {
        120,151,197,250,324,414,537,687,892,1145,
        1484,1911,2479,3196,4148,5359,6954,9000,11687,25000,
        50000,100000,200000
    };

    public static IdleManager instance;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        // Load
        lengthLevel = PlayerPrefs.GetInt(KEY_LENGTH_LEVEL, 0);
        strengthLevel = PlayerPrefs.GetInt(KEY_STRENGTH_LEVEL, 0); // 初期3をベースにしたい場合は +3 を実値側で足す
        offlineLevel = PlayerPrefs.GetInt(KEY_OFFLINE_LEVEL, 0);
        wallet = PlayerPrefs.GetInt(KEY_WALLET, 0);
        _appStartRealtime = Time.realtimeSinceStartup;
        RecalcCosts();
    }
    public int CurrentLength =>  baseLength + lengthLevel * lengthStep;
    public int CurrentStrength => 3 + strengthLevel;       // もともと3スタートの想定を維持
    public int OfflinePerMinute => 3 + offlineLevel;       // もともと3スタートの想定を維持


    private void Start()
    {
        // ★ 起動直後（コールドスタート）でも計算する
        // 起動直後の1フレーム後に1回だけ計算
        //   StartCoroutine(DoInitialOfflineNextFrame());
        ComputeOfflineAndShow();

        // ボタンイベント登録
        if (BuyLengthsButton != null)
            BuyLengthsButton.onClick.AddListener(BuyLengthFunc);
        if (BuyStrengthButton != null)
            BuyStrengthButton.onClick.AddListener(BuyStrengthFunc);
        if (BuyOfflineEarningsButton != null)
            BuyOfflineEarningsButton.onClick.AddListener(BuyOfflineEarningsFunc);
    }
   
    private void OnDestroy()
    {
        // ボタンイベント解除
        if (BuyLengthsButton != null)
            BuyLengthsButton.onClick.RemoveListener(BuyLengthFunc);
        if (BuyStrengthButton != null)
            BuyStrengthButton.onClick.RemoveListener(BuyStrengthFunc);
        if (BuyOfflineEarningsButton != null)
            BuyOfflineEarningsButton.onClick.RemoveListener(BuyOfflineEarningsFunc);
    }


    private void RecalcCosts()
    {
        lengthCost = CostAtLevel(lengthLevel);
        strengthCost = CostAtLevel(strengthLevel);
        offlineEarningsCost = CostAtLevel(offlineLevel);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveNowUtc();
    }
    private int CostAtLevel(int level)
    {
        int idx = Mathf.Clamp(level, 0, costs.Length - 1);
        return costs[idx];
    }
    
   
    // ★ 共通の保存関数に分離
    private void SaveNowUtc()
    {
        string nowIso = DateTime.UtcNow.ToString("O");

        PlayerPrefs.SetString(KEY_LAST_UTC,nowIso);
        PlayerPrefs.Save();
       
        Debug.Log($"[Idle] Saved UTC = {nowIso}");
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveNowUtc();
        }
        else
        {
            // 起動直後（~2秒）は復帰イベントを無視して二重実行を防ぐ
            if (!_didInitialCompute && (Time.realtimeSinceStartup - _appStartRealtime) < 2f)
            {
                _didInitialCompute = true;
                return;
            }

            ComputeOfflineAndShow();
        }
    
    }
    // 既存の ComputeOfflineAndShow を一部変更
    private void ComputeOfflineAndShow()
    {
        string iso = PlayerPrefs.GetString(KEY_LAST_UTC, string.Empty);
        if (string.IsNullOrEmpty(iso)) return;

        if (DateTime.TryParseExact(
                iso, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var lastUtc))
        {
            double elapsedSec = (DateTime.UtcNow - lastUtc).TotalSeconds;

            // ★ 60秒未満は“貯める”。保存も画面遷移もしない（次回に繰り越す）
            if (elapsedSec < 60.0)
            {
                Debug.Log($"[Idle] <60s so skip. elapsed={elapsedSec:F1}s, keep lastUtc={lastUtc:o}");
                return;
            }

            int minutes = Mathf.Clamp(Mathf.FloorToInt((float)(elapsedSec / 60.0)), 0, maxOfflineMinutes);
            totalGain = minutes * OfflinePerMinute;

            Debug.Log($"[Idle] Offline minutes={minutes}, perMin={OfflinePerMinute}, totalGain={totalGain}");

            // ★ 付与できた“ときだけ”基準を今に更新
            SaveNowUtc();

            // ★ 分>=1 のときだけ報酬画面を出す
            if (minutes >= 1 && totalGain > 0)
            {
                _needShowReturn = true;
                StartCoroutine(ShowReturnWhenReady());
            }
        }
    }

    // ★ ScreenManager の初期化完了・他スクリプトの Start が走り切った“あと”に1回だけ RETURN へ
    private IEnumerator ShowReturnWhenReady()
    {
        // 起動直後の復帰イベントや他 Start() をやり過ごす
        yield return null; // 1フレーム
        yield return new WaitForEndOfFrame(); // レイアウト完了後
        // さらに保険で少し待つ（必要なら）
        yield return new WaitForSecondsRealtime(0.05f);

        if (_needShowReturn && !_hasShownReturn && ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ChangeScreen(Screens.RETURN);
            _hasShownReturn = true;
            Debug.Log("[Idle] Switched to RETURN");
        }
    }


    // ★ MAIN への遷移は、RETURN をまだ出していない間は無視して競合を回避
    private void SafeGo(Screens s)
    {
        if (ScreenManager.Instance == null) return;

        if (_needShowReturn && !_hasShownReturn && s == Screens.MAIN)
        {
            Debug.Log("[Idle] Blocked MAIN because RETURN is pending");
            return;
        }
        ScreenManager.Instance.ChangeScreen(s);
    }

    // ---- Purchases ----


    // Button の onClick に登録するために別名関数にしてます
    private void BuyLengthFunc() => BuyLength();
    public bool BuyLength()
    {
        if (!TrySpend(lengthCost)) return false;
        lengthLevel++;
        SaveProgress();
        RecalcCosts();
        SafeGo(Screens.MAIN);
        return true;
    }
    // Button の onClick に登録するために別名関数にしてます
    private void BuyStrengthFunc() => BuyStrength();


    public bool BuyStrength()
    {
        if (!TrySpend(strengthCost)) return false;
        strengthLevel++;
        SaveProgress();
        RecalcCosts();
        SafeGo(Screens.MAIN);
        return true;
    }
    private void BuyOfflineEarningsFunc() => BuyOfflineEarnings();
    public bool BuyOfflineEarnings()
    {
        if (!TrySpend(offlineEarningsCost)) return false;
        offlineLevel++;
        SaveProgress();
        RecalcCosts();
        SafeGo(Screens.MAIN);
        return true;
    }

    private bool TrySpend(int cost)
    {
        if (wallet < cost) return false;
        wallet -= cost;
        return true;
    }
    // ---- Collect ----
    public void CollectMoney()
    {

        wallet += totalGain;
        totalGain = 0;
        SaveProgress();
        SafeGo(Screens.MAIN);
    }

    public void CollectDoubleMoney()
    {
        wallet += totalGain * 2;
        totalGain = 0;
        SaveProgress();
        SafeGo(Screens.MAIN);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(KEY_LENGTH_LEVEL, lengthLevel);
        PlayerPrefs.SetInt(KEY_STRENGTH_LEVEL, strengthLevel);
        PlayerPrefs.SetInt(KEY_OFFLINE_LEVEL, offlineLevel);
        PlayerPrefs.SetInt(KEY_WALLET, wallet);
        PlayerPrefs.Save();
    }

  
}
