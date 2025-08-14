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
    [Tooltip("押してからリピート開始までの待ち時間（秒）")]
    public float initialDelay = 0.35f;
    [Tooltip("リピートの間隔（秒）")]
    public float repeatInterval = 0.08f;

    [Header("Acceleration (optional)")]
    [Tooltip("リピート中に間隔を短くして加速するならON")]
    public bool useAcceleration = true;
    [Tooltip("1回ごとにこの倍率で間隔を短くする（0.9なら10%短縮）")]
    public float intervalMultiplierPerStep = 0.92f;
    [Tooltip("短縮してもこの値より短くはしない")]
    public float minRepeatInterval = 0.03f;

    [Header("Events")]

    [Header("Events")]
    public UnityEvent onRepeat = new UnityEvent();   // ← ここで即生成

    private Coroutine _repeatRoutine;
    private Button _button;

    private void Awake()
    {
        if (onRepeat == null) onRepeat = new UnityEvent(); // ← 念のための保険
        _button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button == null || !_button.interactable) return;
        // タップの単発は Button.onClick に任せる。ここではリピートだけ扱う
        if (_repeatRoutine == null) _repeatRoutine = StartCoroutine(RepeatCoroutine());
    }

    public void OnPointerUp(PointerEventData eventData) => StopRepeat();
    public void OnPointerExit(PointerEventData eventData) => StopRepeat();

    private void OnDisable() => StopRepeat();

    private IEnumerator RepeatCoroutine()
    {
        // リピート開始までの初期待機
        float wait = Mathf.Max(0f, initialDelay);
        if (wait > 0f) yield return new WaitForSeconds(wait);

        float interval = Mathf.Max(0.001f, repeatInterval);

        // 押しっぱなし中は繰り返し発火
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
