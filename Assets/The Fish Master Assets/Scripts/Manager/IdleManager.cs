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
    [SerializeField] private int baseLength = 10;   // �\���p�̏�����
    [SerializeField] private int lengthStep = 5;   // 1���x���ő������钷��



    [Header("Offline Settings")]
    [Tooltip("�I�t���C�����v�̒P�ʂ́u��������v")]
    [SerializeField] private int maxOfflineMinutes = 24 * 60; // ���: 24h


    [Header("UI Buttons")]
    [SerializeField] private Button BuyLengthsButton;
    [SerializeField] private Button BuyStrengthButton;
    [SerializeField] private Button BuyOfflineEarningsButton;

    // ���J�iUI�o�C���h�p�j
    [HideInInspector] public int lengthLevel;
    [HideInInspector] public int strengthLevel;
    [HideInInspector] public int offlineLevel;
    [HideInInspector] public int wallet;
     public int totalGain;

    [HideInInspector] public int lengthCost;
    [HideInInspector] public int strengthCost;
    [HideInInspector] public int offlineEarningsCost;

    // �R�X�g�e�[�u���i���x��0��index0 �ƑΉ��j
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
        strengthLevel = PlayerPrefs.GetInt(KEY_STRENGTH_LEVEL, 0); // ����3���x�[�X�ɂ������ꍇ�� +3 �����l���ő���
        offlineLevel = PlayerPrefs.GetInt(KEY_OFFLINE_LEVEL, 0);
        wallet = PlayerPrefs.GetInt(KEY_WALLET, 0);
        _appStartRealtime = Time.realtimeSinceStartup;
        RecalcCosts();
    }
    public int CurrentLength =>  baseLength + lengthLevel * lengthStep;
    public int CurrentStrength => 3 + strengthLevel;       // ���Ƃ���3�X�^�[�g�̑z����ێ�
    public int OfflinePerMinute => 3 + offlineLevel;       // ���Ƃ���3�X�^�[�g�̑z����ێ�


    private void Start()
    {
        // �� �N������i�R�[���h�X�^�[�g�j�ł��v�Z����
        // �N�������1�t���[�����1�񂾂��v�Z
        //   StartCoroutine(DoInitialOfflineNextFrame());
        ComputeOfflineAndShow();

        // �{�^���C�x���g�o�^
        if (BuyLengthsButton != null)
            BuyLengthsButton.onClick.AddListener(BuyLengthFunc);
        if (BuyStrengthButton != null)
            BuyStrengthButton.onClick.AddListener(BuyStrengthFunc);
        if (BuyOfflineEarningsButton != null)
            BuyOfflineEarningsButton.onClick.AddListener(BuyOfflineEarningsFunc);
    }
   
    private void OnDestroy()
    {
        // �{�^���C�x���g����
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
    
   
    // �� ���ʂ̕ۑ��֐��ɕ���
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
            // �N������i~2�b�j�͕��A�C�x���g�𖳎����ē�d���s��h��
            if (!_didInitialCompute && (Time.realtimeSinceStartup - _appStartRealtime) < 2f)
            {
                _didInitialCompute = true;
                return;
            }

            ComputeOfflineAndShow();
        }
    
    }
    // ������ ComputeOfflineAndShow ���ꕔ�ύX
    private void ComputeOfflineAndShow()
    {
        string iso = PlayerPrefs.GetString(KEY_LAST_UTC, string.Empty);
        if (string.IsNullOrEmpty(iso)) return;

        if (DateTime.TryParseExact(
                iso, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var lastUtc))
        {
            double elapsedSec = (DateTime.UtcNow - lastUtc).TotalSeconds;

            // �� 60�b�����́g���߂�h�B�ۑ�����ʑJ�ڂ����Ȃ��i����ɌJ��z���j
            if (elapsedSec < 60.0)
            {
                Debug.Log($"[Idle] <60s so skip. elapsed={elapsedSec:F1}s, keep lastUtc={lastUtc:o}");
                return;
            }

            int minutes = Mathf.Clamp(Mathf.FloorToInt((float)(elapsedSec / 60.0)), 0, maxOfflineMinutes);
            totalGain = minutes * OfflinePerMinute;

            Debug.Log($"[Idle] Offline minutes={minutes}, perMin={OfflinePerMinute}, totalGain={totalGain}");

            // �� �t�^�ł����g�Ƃ������h������ɍX�V
            SaveNowUtc();

            // �� ��>=1 �̂Ƃ�������V��ʂ��o��
            if (minutes >= 1 && totalGain > 0)
            {
                _needShowReturn = true;
                StartCoroutine(ShowReturnWhenReady());
            }
        }
    }

    // �� ScreenManager �̏����������E���X�N���v�g�� Start ������؂����g���Ɓh��1�񂾂� RETURN ��
    private IEnumerator ShowReturnWhenReady()
    {
        // �N������̕��A�C�x���g�⑼ Start() �����߂���
        yield return null; // 1�t���[��
        yield return new WaitForEndOfFrame(); // ���C�A�E�g������
        // ����ɕی��ŏ����҂i�K�v�Ȃ�j
        yield return new WaitForSecondsRealtime(0.05f);

        if (_needShowReturn && !_hasShownReturn && ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ChangeScreen(Screens.RETURN);
            _hasShownReturn = true;
            Debug.Log("[Idle] Switched to RETURN");
        }
    }


    // �� MAIN �ւ̑J�ڂ́ARETURN ���܂��o���Ă��Ȃ��Ԃ͖������ċ��������
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


    // Button �� onClick �ɓo�^���邽�߂ɕʖ��֐��ɂ��Ă܂�
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
    // Button �� onClick �ɓo�^���邽�߂ɕʖ��֐��ɂ��Ă܂�
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
