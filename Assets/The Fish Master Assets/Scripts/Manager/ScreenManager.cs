using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance;

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
    public void ChangeScreen(Screens screen)
    {
        curretScreen.SetActive(false);
        switch (screen)
        {

            case Screens.MAIN:
                curretScreen = mainScreen;
                UpDateTexts();
                ChecKIdles();
                GetCoin = 0;
                break;

            case Screens.GAME:
                curretScreen = gameScreen;
            
              
                break;

            case Screens.END:
                curretScreen = endScreen;
                SetEndScreenMoney();
                break;
            case Screens.RETURN:
                curretScreen = returnScreen;
                SetReturnScreenMoney();
                break;
        }
        curretScreen?.SetActive(true);
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
        offlineValueText.text = "$" + IdleManager.instance.OfflinePerMinute + "/min";

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
