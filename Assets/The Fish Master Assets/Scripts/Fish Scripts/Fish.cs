
using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [SerializeField] private float surfaceY = -10f; // 水面のY座標（インスペクタから調整可）

    private FishTypeSO type;

    private CircleCollider2D coll;

    private SpriteRenderer rend;

    private float scrennleft;

    private Tweener moveTweener;     // 左右移動
    private Tweener bobTweener;      // 上下ゆらゆら
                                     // ★追加：危険物（サメ／障害物）か？
    public bool IsHazard => type != null && type.kind != FishKind.Normal;
    public FishTypeSO Type
    {

        get
        {
            return type;
        }
        set
        {
            type = value;
            coll.radius = type.collirRadisu;
            rend.sprite = type.sprite;

        }
    }
    private void Awake()
    {
        coll=GetComponent<CircleCollider2D>();
        rend = GetComponentInChildren<SpriteRenderer>();
        scrennleft = Camera.main.ScreenToWorldPoint(Vector3.zero).x;

        // ✕ 左向きに強制しない
        // var ls = transform.localScale;
        // ls.x = -Mathf.Abs(ls.x);
        // transform.localScale = ls;


        // 〇 スケールは常に正に保つ（Prefab差異を吸収）
        var ls = transform.localScale;
        ls.x = -Mathf.Abs(ls.x);
        transform.localScale = ls;

        DOTween.Init(false, true); // recycleAllByDefault=false, safeMode=true
        


    }
    void OnDisable()
    {
        // オブジェクトが無効/破棄されるときも念のため全部止める
        transform.DOKill(false);
    }
    public void ResetFish()
    {
        if (moveTweener != null) moveTweener.Kill(false);
        if (bobTweener != null) bobTweener.Kill(false);

        // 深さは type.minLenght〜type.maxLenght の範囲（正の値）
        float depth = UnityEngine.Random.Range(type.minLenght, type.maxLenght);
        coll.enabled = true;
        Vector3 position =transform.position;
        position.y = surfaceY - depth; // 水面からの距離で計算
        position.x = scrennleft;
        transform.position = position;



        float wiggle = 1f;
        float y = UnityEngine.Random.Range(position.y - wiggle, position.y + wiggle);
        Vector2 target = new Vector2(-position.x, y); // 右へ→左へヨーヨー

        // ★ 初期向き：移動方向に合わせてflipXをセット
        bool movingRight = target.x > position.x;
      


        float dur = 3f;
        float delay = UnityEngine.Random.Range(0, 2 * dur);
        // LoopType.Yoyo よーよーのように横に移動する
        // 左右移動
        moveTweener = transform.DOMove(target, dur, false)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetDelay(delay)
           .OnStepComplete(ToggleFlip) // ← flipXだけを反転
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy); // 破棄時に自動Kill

        // 上下ゆらゆら
        float upDownAmount = UnityEngine.Random.Range(0.2f, 0.5f);
        float upDownTime = UnityEngine.Random.Range(1.5f, 3f);
        bobTweener = transform.DOLocalMoveY(transform.localPosition.y + upDownAmount, upDownTime)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);



    }
    // 以前のスケール反転は廃止
    private void ToggleFlip()
    {
        rend.flipX = !rend.flipX;
    }
  
    public void Hooked()
    {
        coll.enabled = false;
        // オブジェクトが無効/破棄されるときも念のため全部止める
        if (moveTweener != null) moveTweener.Kill(false);
        if (bobTweener != null) bobTweener.Kill(false);

    }

   

  

}
