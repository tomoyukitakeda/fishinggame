
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EncyclopediaUIManager : MonoBehaviour { 
    [Header("Elements")]
    [SerializeField] private RectTransform encyclopediaPanel;

[Header("Settings")]

private Vector2 openedPostion;
private Vector2 closePostion;
private bool isOpen = false; // �J���Ă��邩�ǂ����̃t���O

[SerializeField] Button encyclopediaButton;
private void Start()
{

    openedPostion = Vector2.zero;
    closePostion = new Vector2(encyclopediaPanel.rect.width, 0);
    encyclopediaButton.onClick.AddListener(ToggleShop);
        // �����ʒu�͕��Ă���
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
        DOTween.Kill(encyclopediaPanel); // ������Tween���L�����Z��
        Debug.Log("�I�[�v���}��");
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
