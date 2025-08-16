using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class FishPediaDetailUI : MonoBehaviour
{
    [Header("親（FishPediaRoot）※ここは常時ONのまま")]
    [SerializeField] private GameObject root;  // ← FishPediaRoot を指していてOK。非表示にしない！

    [Header("Panel")]
    [SerializeField] private RectTransform panelRect; // アニメーションさせるパネル

    [Header("Widgets")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text countLabel;    // 「釣った x 回」
    [SerializeField] private TMP_Text priceLabel;    // 価格
    [SerializeField] private TMP_Text descFullLabel; // 解説全文
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float tweenDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    // 未発見時の黒シルエット色
    [SerializeField] private Color lockedSilhouetteColor = new Color(0f, 0f, 0f, 0.9f);

    private Vector2 openedPos;
    private Vector2 closedPos;
    private bool isOpen;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Close);

        // 現在の anchoredPosition を「開いた位置」とする
        openedPos = panelRect.anchoredPosition;

        // パネルの幅ぶん右にずらした位置を「閉じた位置」とする
        float width = panelRect.rect.width;
        closedPos = openedPos + Vector2.right * (width + 50f);

        // 初期状態は右側に隠す
        // ★ root は消さない、panelRect だけ右に隠す
        if (panelRect) panelRect.anchoredPosition = closedPos;

        isOpen = false;
    }

    public void Show(FishTypeSO type, FishPediaEntry entry)
    {
        if (type == null) return;
        bool unlocked = entry != null && entry.unlocked;

        // --- アイコン ---
        if (icon)
        {
            icon.sprite = type.sprite;
            icon.preserveAspect = true;
            icon.color = unlocked ? Color.white : lockedSilhouetteColor;
            icon.material = null;
        }

        // --- テキスト ---
        if (nameLabel)
            nameLabel.text = unlocked
                ? (string.IsNullOrEmpty(type.japaneseName) ? type.fishName : type.japaneseName)
                : "？？？";

        if (countLabel)
            countLabel.text = unlocked && entry != null ? $"釣った {entry.caughtCount} 回" : "未発見";

        if (priceLabel)
            priceLabel.text = unlocked ? $"価格: {type.price.ToString("N0")} コイン" : "価格: ？？？";

        if (descFullLabel)
            descFullLabel.text = unlocked
                ? (string.IsNullOrEmpty(type.description) ? "—" : type.description)
                : "この魚を一度釣ると解説が表示されます。";

        // --- アニメーション表示 ---
        if (root) root.SetActive(true);
        DOTween.Kill(panelRect);
        panelRect.anchoredPosition = closedPos; // 右外からスタート
        panelRect.DOAnchorPos(openedPos, tweenDuration).SetEase(easeType);

        isOpen = true;
    }

    public void Close()
    {
        if (!isOpen) return;
        DOTween.Kill(panelRect);
        panelRect.DOAnchorPos(closedPos, tweenDuration)
      .SetEase(easeType);
     
        isOpen = false;
    }
}
