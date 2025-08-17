using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // ★追加：Button用

public class Hock : MonoBehaviour
{
    // 追加：入力デバウンス & 多重開始ブロック
    private bool _inProgress = false;
    private float _blockInputUntil = 0f;
    [SerializeField] private float startDebounceSeconds = 0.25f;
    [SerializeField] private float normalReturnTime = 1.2f; // 通常帰還時間
    [SerializeField] private float hazardReturnTime = 0.5f; // サメ・障害物帰還時間
    [SerializeField] private float hookStopY = -10f;          // フックを止めたい高さ
    [SerializeField] private float surfaceThresholdY = -11f;  // ほぼ水面とみなすY
    public Transform hookedTransfrom;
    [Header("Managers")]
    [SerializeField] private FishPediaManager fishPedia; 

    private Camera mainCamera;
    private Collider2D coll;

    [Header("References")]
    [SerializeField] private LengthUIController lengthUI;
    [SerializeField] private CaughtFishUIManager caughtUI;
    [SerializeField] private CaughtFishInventoryMB caughtInventory;

    [Header("UI")]
    [SerializeField] private Button startFishingButton;

    private int length;
    private int strength;
    private int fishCount;

    private bool canMove =false;
    private bool canFishing =true;

    public List<Fish> hookedFishes;

    private Tweener cameraTween;

    private int  GetFishCoin;

    private bool snagged = false;

    // 連打・多重終了防止
    private bool _finishingNow = false;
    private bool _catchWindow = false; // 釣り中の接触受付フラグ

    // --- SFX（インスペクタで AudioCueSO を割り当て） ---
    [SerializeField] private AudioCueSO sfxCast;   // 釣り開始（投げる）
    [SerializeField] private AudioCueSO sfxHook;   // 魚がかかった
    [SerializeField] private AudioCueSO sfxReel;   // リールを上げ始め
    [SerializeField] private AudioCueSO sfxCash;   // 回収/精算
    [SerializeField] private AudioCueSO sfxMiss;   // 収穫ゼロのとき（任意）
                                                   // ★任意追加：サメ／根掛かり用SFX（あれば）
    [SerializeField] private AudioCueSO sfxSharkOrSnag;

    // 連打・多重再生の抑制用
    private float _lastHookSfxTime;
    private const float HookSfxInterval = 0.08f;

    private PooledAudioSource _reelHandle; // ← 追加：再生中のハンドル

    private void Awake()
    {
        mainCamera = Camera.main;
        coll = GetComponent<Collider2D>();
      
        coll.enabled = false;
        hookedFishes = new List<Fish>();
        // ★開始ボタンのリスナー設定
        if (startFishingButton != null)
        {
            startFishingButton.onClick.RemoveAllListeners();
            startFishingButton.onClick.AddListener(OnClickStartButton);
        }

    }

    private void OnDestroy()
    {
        if (startFishingButton != null)
        {
            startFishingButton.onClick.RemoveListener(OnClickStartButton);
        }
    }

    private void Start()
    {
        // 起動直後の連打対策：少しの間だけ無視
        _blockInputUntil = Time.unscaledTime + startDebounceSeconds;
        // 念のため初期位置・親を整える
        transform.SetParent(null);
        transform.position = Vector2.down * 6f;

        UpdateStartButtonState(); // ★追加：初期のボタン状態反映
        Invoke(nameof(UpdateStartButtonState), startDebounceSeconds + 0.02f); // ← デバウンス後に再評価
    }


    void Update()
    {

        UpdateStartButtonState(); // ← 追加：これでデバウンス解けた瞬間にtrueになる
        if (canMove && Input.GetMouseButton(0))
        {

            Vector3 vector = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 postion = transform.position;
            postion.x = vector.x;
            transform.position = postion;
        }
        
    }

    private void FinishNow(bool forceMiss = false)
    {
        if (_finishingNow) return;
        _finishingNow = true;

        // これ以降のキャッチ禁止
        coll.enabled = false;
        _catchWindow = false;

        // 進行中のカメラTweenは「待たない」ので中断
        cameraTween?.Kill(false);

        // （任意）見た目用にカメラは0へ戻すが、END遷移は待たない
        mainCamera.transform.DOMoveY(0, 0.6f, false)
            .SetUpdate(false) // 時間スケールに従う（好みで）
            .OnUpdate(() =>
            {
                if (mainCamera.transform.position.y >= -11)
                {
                    transform.SetParent(null);
                    transform.position = new Vector2(transform.position.x, -6);
                }
            });

        // 釣果精算（StopFinshing の OnComplete 相当）
        int num = 0;
        if (!forceMiss)
        {
            num = SettleHookedFishes();
        }
        else
        {
            caughtInventory.ClearAll();
            ResetFish();
        }

        GetFishCoin = forceMiss ? 0 : num;
        ScreenManager.Instance.GetCoin = GetFishCoin;
        IdleManager.instance.wallet += GetFishCoin;

        // SFX
        if (GetFishCoin > 0)
        {
            if (sfxCash) AudioManager.Instance.PlayUISFX(sfxCash);
        }
        else
        {
            if (sfxMiss) AudioManager.Instance.PlayUISFX(sfxMiss);
        }

        // ENDへ即移行
        ScreenManager.Instance.ChangeScreen(Screens.END);

        // 後処理
        hookedFishes.Clear();
        if (lengthUI) lengthUI.SetInteractable(true);

        _blockInputUntil = Time.unscaledTime + 0.15f;
        canFishing = true;
        _inProgress = false;
        GetFishCoin = 0;
        UpdateStartButtonState();

        // 完了
        _finishingNow = false;
    }

    private void OnClickStartButton()
    {
        // 押してすぐは連打防止、かつ釣れる状態のみ
        if (!CanStartFishingNow()) return;
        StartFishing();
    }

    private bool CanStartFishingNow()
    {
        if (Time.unscaledTime < _blockInputUntil) return false;
        if (_inProgress) return false;
        if (!canFishing) return false;

        // 長さUIが操作不可の間は開始不可（任意）
        // if (lengthUI != null && !lengthUI.IsInteractable) return false;

        return true;
    }
    // ★追加：ボタンのinteractableを更新
    private void UpdateStartButtonState()
    {
        if (startFishingButton == null) return;

        bool screenAllows = true;
        if (ScreenManager.Instance != null)
        {
            // MAIN or GAME の時だけ true
            var sm = ScreenManager.Instance;
            // curretScreen は private なので、ScreenManager側の SetFishingButtonInteractable だけでも充分。
            // ここでは「END/RETURNの時はfalse」に上書きする簡易版:
            screenAllows = (sm != null) && (sm.gameObject.activeInHierarchy) &&
                           (sm.gameScreen.activeSelf || sm.mainScreen.activeSelf);
        }

        startFishingButton.interactable = screenAllows && CanStartFishingNow();
    }
    private void OnDisable()
    {
        if (coll) coll.enabled = false; // ★シーン切替などで安全OFF
        _catchWindow = false;
    }
    public void StartFishing()
    {

        // 図鑑が開いているときは釣り不可
        if (EncyclopediaUIManager.Instance != null && EncyclopediaUIManager.Instance.IsOpen)
        {
            Debug.Log("図鑑が開いているので釣りできません");
            return;
        }

        // 通常の釣り処理
        Debug.Log("釣り開始！");

        // 連打・多重防止（起動直後のデバウンス + 進行中ブロック + 既存のcanFishing）
        if (Time.unscaledTime < _blockInputUntil) { UpdateStartButtonState(); return; }
        if (_inProgress || !canFishing) { UpdateStartButtonState(); return; }

        coll.enabled = false;      // ★追加：安全にOFF
        _catchWindow = false;
        _inProgress = true;          // ← 最上段で立てる
        canFishing = false;

        UpdateStartButtonState();    // ★追加：ボタンOFF反映
        ResetFish();

        // 表示が消えない保険
        gameObject.SetActive(true);

        // ★音：投げる
        if (sfxCast) AudioManager.Instance.PlaySFX(sfxCast, transform.position);

        // ★開始時にリセット
        snagged = false;
        canFishing = false;
        GetFishCoin = 0;



        // UI で選んだ値を負にして設定（負の方向へ進む）
        int maxReach = Mathf.Max(1, IdleManager.instance.CurrentLength);
        int uiDepth = Mathf.Clamp(lengthUI.SelectedDepth, 1, maxReach);

        Debug.Log(lengthUI.SelectedDepth+"セレクトディープ");

        length = -uiDepth;


        strength = IdleManager.instance.CurrentStrength;
        fishCount = 0;
        float time = (-length) * 0.1f;

        lengthUI.SetInteractable(false);

        cameraTween = mainCamera.transform.DOMoveY(length, 1 + time * 0.25f, false)
            .OnUpdate(delegate
              {
                  if (mainCamera.transform.position.y <= surfaceThresholdY)
                  {
                      transform.SetParent(mainCamera.transform);
                  }
              })
           .OnComplete(() =>
           {
               // 底に到達：ここでキャッチ開始
               coll.enabled = true;       // ★ここでON
               _catchWindow = true;       // ★受け付け開始

               // すぐ上昇開始（底で待たせたいなら DOVirtual.DelayedCall を挟む）
               cameraTween = mainCamera.transform.DOMoveY(0, time * 5f, false)
        .OnUpdate(() =>
        {
            float cy = mainCamera.transform.position.y;

            // フック固定処理は既存のまま
            if (transform.parent == mainCamera.transform && cy >= hookStopY)
            {
                transform.SetParent(null);
                var p = transform.position;
                p.y = hookStopY;
                transform.position = p;
            }

            // ★追加：水面しきい値に達したら即フィニッシュ（待たない）
            if (cy >= surfaceThresholdY && !_finishingNow)
            {
                FinishNow(forceMiss: false); // サメ等で強制ミスなら true にする
            }
        })
        .OnComplete(() =>
        {
            // 通常はここまで待たずにFinishNowで終わる想定
            StopFinshing();
        });

           });

        ScreenManager.Instance.ChangeScreen(Screens.GAME);
        coll.enabled = false;
        canMove = true;
        hookedFishes.Clear();   
    }

    void StopFinshing(bool forceMiss = false)
    {
        coll.enabled = false;   // ★すぐOFF
        _catchWindow = false;

        // ★音：リール開始（上げる動きに入った瞬間）
        // リール音：上げ始めで鳴らす（まだ鳴ってなければ）
        if (!forceMiss && sfxReel)
        {
            _reelHandle = AudioManager.Instance.PlaySFX(sfxReel, transform.position, true);
        }
        cameraTween.Kill(false);
        cameraTween = mainCamera.transform.DOMoveY(0, 2, false).OnUpdate(delegate
        {
            if (mainCamera.transform.position.y >= -11)
            {
                transform.SetParent(null);
                transform.position = new Vector2(transform.position.x, -6);


                // ★ホック停止で音を止める（即停止）
                if (_reelHandle != null)
                {
                    AudioManager.Instance.StopSFX(_reelHandle);
                    _reelHandle = null;
                }
            }
        }).OnComplete(delegate
        {
            transform.position = Vector2.down * 6;
         
            int num = 0;

            // 強制ミスなら収穫ゼロ。通常は合計
            if (!forceMiss)
            {
                num = SettleHookedFishes();
            }
            else
            {
                // 失敗時は釣果をクリア（UIはClearで非表示になる）
                caughtInventory.ClearAll();
                ResetFish();
            }

            GetFishCoin = forceMiss ? 0 : num;


            
            ScreenManager.Instance.GetCoin = GetFishCoin;
            IdleManager.instance.wallet += GetFishCoin;

           

            // ★音：精算（コイン or ミス）
            if (GetFishCoin > 0)
            {
                if (sfxCash) AudioManager.Instance.PlayUISFX(sfxCash);
            }
            else
            {
                if (sfxMiss) AudioManager.Instance.PlayUISFX(sfxMiss);
            }



            ScreenManager.Instance.ChangeScreen(Screens.END);
          
            hookedFishes.Clear();
            if (lengthUI) lengthUI.SetInteractable(true);

            // 次回入力のデバウンス（画面遷移直後の誤タップ対策）
            _blockInputUntil = Time.unscaledTime + 0.15f;
            // ★ここでインベントリをクリア
         

            // 状態解除
            canFishing = true;
            _inProgress = false;
            GetFishCoin = 0;
            UpdateStartButtonState(); // ★追加：釣り可能になったのでボタンON
        });
    }


    private int  SettleHookedFishes() // 例：精算関数
    {
            int num = 0;

            for (int i = 0; i < hookedFishes.Count; i++)
            {

                var fish = hookedFishes[i];
                Debug.Log($"ADD {fish.Type.name} id={fish.Type.GetInstanceID()}");
                if (fish == null || fish.Type == null) continue;

            // ★ 図鑑登録：釣り上げ確定時のみ
            if (!fish.alreadyCounted && fishPedia != null && fish.Type != null)
            {
                fishPedia.ReportCaught(fish.Type);
                fish.alreadyCounted = true;
            }

                caughtInventory.Add(fish.Type, 1);
                hookedFishes[i].transform.SetParent(null);
                hookedFishes[i].ResetFish();
                num += hookedFishes[i].Type.price;
            }
        

        return num;

    }

    private void ResetFish()
    {
        caughtInventory.ClearAll(); 
        // 釣れていた魚もリセットして消す
        for (int i = 0; i < hookedFishes.Count; i++)
        {
            hookedFishes[i].transform.SetParent(null);
            hookedFishes[i].ResetFish();
        }
    }

    // ★危険物に当たったときの処理（この回は終了に）
    private void TriggerSnag(Fish hazard)
    {
        if (snagged) return; // 二重防止
        snagged = true;

        // 演出：SFX & フックを小刻みに揺らす・カメラを少し揺らす
        if (sfxSharkOrSnag) AudioManager.Instance.PlaySFX(sfxSharkOrSnag, transform.position);
        transform
            .DOShakeRotation(0.35f, Vector3.forward * 45f, 15, 90f, false)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        mainCamera.transform
            .DOShakePosition(0.25f, 0.2f, 10, 90f, false, true)
            .SetLink(mainCamera.gameObject, LinkBehaviour.KillOnDestroy);

        // 危険物自体はすぐリセットして流す（引っかかり続け防止）
        if (hazard != null)
        {
            hazard.Hooked();   // 動きを止める
            hazard.transform.SetParent(null);
            hazard.ResetFish();
        }

        coll.enabled = false;  // ★再確認：OFF
        _catchWindow = false;  // ★OFF

        // サメに当たったときは短い時間で帰還
        float returnTime = hazard != null && hazard.IsHazard ? hazardReturnTime : normalReturnTime;


        // 早めに強制帰還（コイン0でEND）
        cameraTween?.Kill(false);
        cameraTween = mainCamera.transform.DOMoveY(0, returnTime, false)
            .OnUpdate(() =>
            {
                if (mainCamera.transform.position.y >= -11)
                {
                    transform.SetParent(null);
                    transform.position = new Vector2(transform.position.x, -6);
                }
            })
            .OnComplete(() =>
            {
                StopFinshing(forceMiss: true);
            });
    }

    private void OnTriggerEnter2D(Collider2D target)
    {
        if (!_catchWindow) return;
        if (!target.CompareTag("Fish")) return;

        // snagged 中は何も拾わない
        if (snagged) return;

        Fish compnment = target.GetComponent<Fish>();
        if (compnment == null) return;

        // ★危険物（サメ／障害物）
        if (compnment.IsHazard)
        {
            TriggerSnag(compnment);
            return;
        }

        // --- ここから通常の魚キャッチ ---
        if (fishCount == strength) return;

        fishCount++;

        if (sfxHook && Time.time - _lastHookSfxTime > HookSfxInterval)
        {
            AudioManager.Instance.PlaySFX(sfxHook, transform.position);
            _lastHookSfxTime = Time.time;
        }

        compnment.Hooked();
        hookedFishes.Add(compnment);
        target.transform.SetParent(transform);
        target.transform.position = hookedTransfrom.position;
        target.transform.rotation = hookedTransfrom.rotation;
        target.transform.localScale = Vector3.one;

        target.transform
            .DOShakeRotation(5, Vector3.forward * 45, 10, 90, false)
            .SetLoops(1, LoopType.Yoyo)
            .OnComplete(() => { target.transform.rotation = Quaternion.identity; });

        if (fishCount >= strength)
            StopFinshing();
    }

}
