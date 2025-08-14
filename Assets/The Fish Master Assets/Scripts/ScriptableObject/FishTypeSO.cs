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
    [Header("��{�ݒ�")]
    public string fishName;         // ���O
    public string japaneseName;      // ���{�ꖼ
    [TextArea(2, 4)]
    public string description;      // �������iUI�Ȃǂŕ\���p�j

    [Header("���l�ݒ�")]
    public int price;               // �l�i
  
    public float minLenght;         // �ŏ�Y�ʒu
    public float maxLenght;         // �ő�Y�ʒu
    public float collirRadisu;      // �R���C�_�[���a

    [Header("�����ځE���")]
    public Sprite sprite;           // ������
    public FishKind kind = FishKind.Normal; // ���

    // ���ɍ���� FishTypeSO �ɂ���𑫂�
    [Header("�o����")]
    public int spawnCount = 10;   // ���̎�ނ����C�X�|�[�����邩
#if UNITY_EDITOR
    private void OnValidate()
    {
        // ���A�Z�b�g�́u�t�@�C�����v�͕ς��Ȃ��B�t�B�[���h���������B
        if (sprite != null && fishName != sprite.name)
        {
            fishName = sprite.name;
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
