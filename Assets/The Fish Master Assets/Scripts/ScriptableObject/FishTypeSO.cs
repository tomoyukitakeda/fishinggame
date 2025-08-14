using UnityEditor;
using UnityEngine;

public enum FishKind
{
    Normal,
    Shark,
    Obstacle
}

[CreateAssetMenu(fileName = "NewFishType", menuName = "Fishing/Fish Type", order = 1)]
public class FishTypeSO : ScriptableObject
{
    [Header("基本設定")]
    public string fishName;         // 名前
    public string japaneseName;      // 日本語名
    [TextArea(2, 4)]
    public string description;      // 説明文（UIなどで表示用）

    [Header("数値設定")]
    public int price;               // 値段
  
    public float minLenght;         // 最小Y位置
    public float maxLenght;         // 最大Y位置
    public float collirRadisu;      // コライダー半径

    [Header("見た目・種別")]
    public Sprite sprite;           // 見た目
    public FishKind kind = FishKind.Normal; // 種別

    // 既に作った FishTypeSO にこれを足す
    [Header("出現数")]
    public int spawnCount = 10;   // この種類を何匹スポーンするか
#if UNITY_EDITOR
    private void OnValidate()
    {
        // ※アセットの「ファイル名」は変えない。フィールドだけ同期。
        if (sprite != null && fishName != sprite.name)
        {
            fishName = sprite.name;
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
