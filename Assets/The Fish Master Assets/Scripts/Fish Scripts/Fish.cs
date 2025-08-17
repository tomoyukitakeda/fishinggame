
using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Fish : MonoBehaviour
{
    [SerializeField] private float surfaceY = -10f; // 水面のY座標（インスペクタから調整可）

    private FishTypeSO type;
    private float leftX, rightX, surfaceYFromSpawner;
    private CircleCollider2D coll;

    private SpriteRenderer rend;

    private float scrennleft;

    private Tweener moveTweener;     // 左右移動
    private Tweener bobTweener;      // 上下ゆらゆら
                                     // ★追加：危険物（サメ／障害物）か？
    public bool IsHazard => type != null && type.kind != FishKind.Normal;

    // 釣り上げ確定時の二重登録防止用
    public bool alreadyCounted { get; set; } = false;

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
      




        // スケールは常に正（flipXで向きを制御）
        var ls = transform.localScale;
        ls.x = -Mathf.Abs(ls.x);
        transform.localScale = ls;

    
        


    }


    public void SetBounds(float leftX, float rightX, float surfaceY)
    {
        this.leftX = leftX;
        this.rightX = rightX;
        this.surfaceYFromSpawner = surfaceY;
    }
    void OnDisable()
    {
        // オブジェクトが無効/破棄されるときも念のため全部止める
        transform.DOKill(false);
    }
    public void ResetFish()
    {
        moveTweener?.Kill(false);
        bobTweener?.Kill(false);

        float depth = UnityEngine.Random.Range(type.minLenght, type.maxLenght);
        coll.enabled = true;

        // yは水面からの深さ
        var pos = transform.position;
        pos.y = surfaceYFromSpawner - depth;

        // 既にSpawnerでxは左右どちらかに置かれている
        transform.position = pos;

        // 目標を「反対側の端」にする
        float wiggle = 1f;
        float y = UnityEngine.Random.Range(pos.y - wiggle, pos.y + wiggle);
        float targetX = (Mathf.Abs(pos.x - leftX) < Mathf.Abs(pos.x - rightX)) ? rightX : leftX;
        Vector2 target = new Vector2(targetX, y);

        // 右向きデフォルト → 左へ進むときだけflipX=true
        bool movingLeft = target.x < pos.x;
        rend.flipX = movingLeft;

        float dur = 3f;
        float delay = UnityEngine.Random.Range(0, 6f);

        moveTweener = transform.DOMove(target, dur, false)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetDelay(delay)
            .OnStepComplete(() => rend.flipX = !rend.flipX)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        float upDownAmount = UnityEngine.Random.Range(0.2f, 0.5f);
        float upDownTime = UnityEngine.Random.Range(1.5f, 3f);
        bobTweener = transform.DOLocalMoveY(transform.localPosition.y + upDownAmount, upDownTime)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }



    public void Hooked()
    {
        coll.enabled = false;
        // オブジェクトが無効/破棄されるときも念のため全部止める
        if (moveTweener != null) moveTweener.Kill(false);
        if (bobTweener != null) bobTweener.Kill(false);

    }

   

  

}
