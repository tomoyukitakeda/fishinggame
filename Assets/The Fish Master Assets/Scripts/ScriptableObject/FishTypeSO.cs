using System;
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

    // �� �}�ӗp�̉i��ID�i�����^�C���ۑ��̃L�[�Ɏg���B�A�Z�b�g���ύX�̉e�����󂯂Ȃ��j
    [SerializeField, HideInInspector] private string persistentId;
    public string PersistentId => persistentId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ���A�Z�b�g�́u�t�@�C�����v�͕ς��Ȃ��B�t�B�[���h���������B
        if (sprite != null && fishName != sprite.name)
        {
            fishName = sprite.name;
            EditorUtility.SetDirty(this);
        }
        // �i��ID���ݒ�Ȃ玩���̔ԁi1�x�����j
        if (string.IsNullOrEmpty(persistentId))
        {
            persistentId = Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
