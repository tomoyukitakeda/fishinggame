// PressAndHoldButton.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PressAndHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Repeat Settings")]
    [Tooltip("�����Ă��烊�s�[�g�J�n�܂ł̑҂����ԁi�b�j")]
    public float initialDelay = 0.35f;
    [Tooltip("���s�[�g�̊Ԋu�i�b�j")]
    public float repeatInterval = 0.08f;

    [Header("Acceleration (optional)")]
    [Tooltip("���s�[�g���ɊԊu��Z�����ĉ�������Ȃ�ON")]
    public bool useAcceleration = true;
    [Tooltip("1�񂲂Ƃɂ��̔{���ŊԊu��Z������i0.9�Ȃ�10%�Z�k�j")]
    public float intervalMultiplierPerStep = 0.92f;
    [Tooltip("�Z�k���Ă����̒l���Z���͂��Ȃ�")]
    public float minRepeatInterval = 0.03f;

    [Header("Events")]

    [Header("Events")]
    public UnityEvent onRepeat = new UnityEvent();   // �� �����ő�����

    private Coroutine _repeatRoutine;
    private Button _button;

    private void Awake()
    {
        if (onRepeat == null) onRepeat = new UnityEvent(); // �� �O�̂��߂̕ی�
        _button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button == null || !_button.interactable) return;
        // �^�b�v�̒P���� Button.onClick �ɔC����B�����ł̓��s�[�g��������
        if (_repeatRoutine == null) _repeatRoutine = StartCoroutine(RepeatCoroutine());
    }

    public void OnPointerUp(PointerEventData eventData) => StopRepeat();
    public void OnPointerExit(PointerEventData eventData) => StopRepeat();

    private void OnDisable() => StopRepeat();

    private IEnumerator RepeatCoroutine()
    {
        // ���s�[�g�J�n�܂ł̏����ҋ@
        float wait = Mathf.Max(0f, initialDelay);
        if (wait > 0f) yield return new WaitForSeconds(wait);

        float interval = Mathf.Max(0.001f, repeatInterval);

        // �������ςȂ����͌J��Ԃ�����
        while (true)
        {
            if (_button == null || !_button.interactable) break;

            onRepeat?.Invoke();

            if (useAcceleration)
            {
                interval = Mathf.Max(minRepeatInterval, interval * intervalMultiplierPerStep);
            }

            yield return new WaitForSeconds(interval);
        }

        _repeatRoutine = null;
    }

    private void StopRepeat()
    {
        if (_repeatRoutine != null)
        {
            StopCoroutine(_repeatRoutine);
            _repeatRoutine = null;
        }
    }
}
