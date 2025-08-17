using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance;

    // ... 既存フィールド ...
    [Header("釣り開始ボタン（Hockと同じものを割り当て）")]
    [SerializeField] private Button startFishingButton; // ★追加


    private GameObject curretScreen;
    public GameObject endScreen;
    public GameObject gameScreen;
    public GameObject mainScreen;
    public GameObject returnScreen;

    public Button lengthButton;
    public Button StreegthButton;
    public Button offlineButton;

    public TextMeshProUGUI CurrentMoney;
    public TextMeshProUGUI lengthCostText;
    public TextMeshProUGUI lengthValueText;
    public TextMeshProUGUI strengthCostText;
    public TextMeshProUGUI strengthValueText;
  
    public TextMeshProUGUI offlineCostText;
    public TextMeshProUGUI offlineValueText;
    public TextMeshProUGUI fishGetendScreenMoney;
    public TextMeshProUGUI returnscreenMoemy;

    public int GetCoin;

    private void Awake()
    {
        if (ScreenManager.Instance)
        {
            Destroy(gameObject);
        }
        else
        {
            ScreenManager.Instance = this;
        }

        curretScreen = mainScreen;
    }
    private void Start()
    {
        ChecKIdles();
        UpDateTexts();

    }
    // ★ いつでもMAINのUIを更新できるユーティリティ
    public void RefreshMainUI()
    {
        UpDateTexts();
        ChecKIdles();
    }
    public void ChangeScreen(Screens screen)
    {
        var target = ScreenToGO(screen);
        bool same = (curretScreen == target);

        // 画面のアクティブ切替は同一画面ならスキップ
        if (!same)
        {
            curretScreen?.SetActive(false);
            curretScreen = target;
            curretScreen?.SetActive(true);
            Debug.Log($"[ScreenManager] ChangeScreen -> {screen}");
        }

     
        switch (screen)
        {

            case Screens.MAIN:
            
                UpDateTexts();
                ChecKIdles();
                SetFishingButtonInteractable(true);   // ★MAINは有効
                                                      // MAINに戻ったタイミングのみコインを0にしたいなら「同一画面でない時だけ」ゼロ化
                if (!same) GetCoin = 0;

                break;

            case Screens.GAME:
              

                SetFishingButtonInteractable(true);   // ★GAMEは有効
                break;

            case Screens.END:
             
                SetEndScreenMoney();
                SetFishingButtonInteractable(false);  // ★ENDは無効！
                break;
            case Screens.RETURN:
               
                SetReturnScreenMoney();
                SetFishingButtonInteractable(false);  // ★RETURNも無効
                break;
        }
      
    }

    private GameObject ScreenToGO(Screens screen)
    {
        switch (screen)
        {
            case Screens.MAIN: return mainScreen;
            case Screens.GAME: return gameScreen;
            case Screens.END: return endScreen;
            case Screens.RETURN: return returnScreen;
        }
        return null;
    }


    private void SetFishingButtonInteractable(bool value)  // ★追加
    {
        if (startFishingButton) startFishingButton.interactable = value;
    }
    public void SetEndScreenMoney()
    {
        fishGetendScreenMoney.text ="$"+GetCoin;
    }
    public void SetReturnScreenMoney()
    {
        returnscreenMoemy.text = "$" + IdleManager.instance.totalGain + "gain while waiting";
    }

    private void UpDateTexts()
    {
        CurrentMoney.text = "$" + IdleManager.instance.wallet;
        lengthCostText.text = "$" + IdleManager.instance.lengthCost;
        lengthValueText.text = IdleManager.instance.CurrentLength + "M";
        strengthCostText.text = "$" + IdleManager.instance.strengthCost;
        strengthValueText.text =IdleManager.instance.CurrentStrength + " fishes.";
        offlineCostText.text ="$"+IdleManager.instance.offlineEarningsCost;
        offlineValueText.text =
      "$" + IdleManager.instance.OfflinePerMinuteFloatForUI.ToString("0.##") + "/min";

    }

    private void ChecKIdles()
    {
        int lengthCost =IdleManager.instance.lengthCost;
        int StrengthCost = IdleManager.instance.strengthCost;
        int offlineEarningCost = IdleManager.instance.offlineEarningsCost;
        int wallet =IdleManager.instance.wallet;
       
        if(wallet < lengthCost)
        {
            lengthButton.interactable = false;
        }
        else
        {
            lengthButton.interactable = true;
        }
        if (wallet < StrengthCost)
        {
            StreegthButton.interactable = false;
        }
        else
        {
            StreegthButton.interactable = true;
        }
        if (wallet < offlineEarningCost)
        {
            offlineButton.interactable = false;
        }
        else
        {
            offlineButton.interactable = true;
        }
      
    }
}
