
using TMPro;

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CaughtFishItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] protected TextMeshProUGUI PriceText;
    [SerializeField] private TextMeshProUGUI countLabel;


    private FishTypeSO _type;


    public void Set(FishTypeSO type, int count)
    {
        _type = type;
        if (icon) icon.sprite = type.sprite;
        if (nameLabel) nameLabel.text = type.japaneseName; // アセット名でOK
        if (PriceText) PriceText.text =type.price.ToString();
      
    }

    public void UpdateCount(int count)
    {
        Debug.Log($"[ItemUI] {_type?.name} -> {count}");
        Debug.Log("カウント"+count);
        if (countLabel) countLabel.text = $"×{count}";
        // ここで軽いポップ演出を入れても良い
         transform.DOKill(); transform.DOPunchScale(Vector3.one*0.08f, 0.2f);
    }
}
