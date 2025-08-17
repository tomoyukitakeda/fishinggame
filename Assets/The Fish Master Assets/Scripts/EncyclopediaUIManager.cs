
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EncyclopediaUIManager : MonoBehaviour {


    public static EncyclopediaUIManager Instance { get; private set; }

    [Header("Elements")]
    [SerializeField] private RectTransform encyclopediaPanel;

[Header("Settings")]

private Vector2 openedPostion;
private Vector2 closePostion;
private bool isOpen = false; // 開いているかどうかのフラグ

[SerializeField] Button encyclopediaButton;

    public bool IsOpen => isOpen; // 外部から参照用

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
{

    openedPostion = Vector2.zero;
    closePostion = new Vector2(encyclopediaPanel.rect.width, 0);
    encyclopediaButton.onClick.AddListener(ToggleShop);
        // 初期位置は閉じておく
        encyclopediaPanel.anchoredPosition = closePostion;

}
private void OnDestroy()
{
        encyclopediaButton.onClick.RemoveListener(ToggleShop);
}
private void ToggleShop()
{
    if (isOpen)
        Close();
    else
        Open();
}

    public void Open()
    {
        DOTween.Kill(encyclopediaPanel); // 既存のTweenをキャンセル
        Debug.Log("オープン図鑑");
        encyclopediaPanel.DOAnchorPos(openedPostion, 0.3f)
            .SetEase(Ease.InOutSine);
        isOpen = true;
    }

    public void Close()
    {
        DOTween.Kill(encyclopediaPanel);
        encyclopediaPanel.DOAnchorPos(closePostion, 0.3f)
            .SetEase(Ease.InOutSine);
        isOpen = false;
    }
}
