using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FishPediaItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text subLabel;   // 「釣った x 回」
  
    [SerializeField] private Button rootButton;   // ★ 押下で詳細を開く
    [SerializeField] private Color lockedSilhouetteColor = new Color(0f, 0f, 0f, 0.9f);

    private FishTypeSO _type;
    private FishPediaEntry _entry;
    private Action<FishTypeSO, FishPediaEntry> _onClick; // ★ 詳細表示呼出
                                                         // ★ 未発見時の黒シルエット色（不透明度はお好みで）
   
    // UI側からハンドラを注入
    public void Init(Action<FishTypeSO, FishPediaEntry> onClick)
    {
        _onClick = onClick;
        if (rootButton != null)
        {
            rootButton.onClick.RemoveAllListeners();
            rootButton.onClick.AddListener(() =>
            {
                if (_type != null) _onClick?.Invoke(_type, _entry);
            });
        }
    }

    public void Bind(FishTypeSO type, FishPediaEntry entry)
    {
        _type = type;
        _entry = entry;

        var unlocked = entry != null && entry.unlocked;

        // ★ 黒シルエット化：未発見時は色を黒、発見済は白
        if (icon)
        {
            // 形状を見せるなら元スプライトを使う。形状も隠したいなら genericSilhouette を使う。
            icon.sprite = type.sprite;
            icon.preserveAspect = true;
            icon.color = unlocked ? Color.white : lockedSilhouetteColor;
            // 余計なマテリアルが設定されていたら外す
            icon.material = null;
        }
        nameLabel.text = unlocked
            ? (string.IsNullOrEmpty(type.japaneseName) ? type.fishName : type.japaneseName)
            : "？？？";

        subLabel.text = unlocked ? $"釣った {entry.caughtCount} 回" : "未発見";

       
    }
}
