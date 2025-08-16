using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class Hock : MonoBehaviour
{
    // �ǉ��F���̓f�o�E���X & ���d�J�n�u���b�N
    private bool _inProgress = false;
    private float _blockInputUntil = 0f;
    [SerializeField] private float startDebounceSeconds = 0.25f;
    [SerializeField] private float normalReturnTime = 1.2f; // �ʏ�A�Ҏ���
    [SerializeField] private float hazardReturnTime = 0.5f; // �T���E��Q���A�Ҏ���
    [SerializeField] private float hookStopY = -5f;          // �t�b�N���~�߂�������
    [SerializeField] private float surfaceThresholdY = -11f;  // �قڐ��ʂƂ݂Ȃ�Y
    public Transform hookedTransfrom;

    private Camera mainCamera;
    private Collider2D coll;

    [Header("References")]
    [SerializeField] private LengthUIController lengthUI; // Inspector �Ŋ��蓖��
    [SerializeField] private CaughtFishUIManager caughtUI;  // �� �ǉ��i�C���X�y�N�^���蓖�āj

    private int length;
    private int strength;
    private int fishCount;

    private bool canMove =false;
    private bool canFishing =true;

    public List<Fish> hookedFishes;

    private Tweener cameraTween;

    private int  GetFishCoin;

    private bool snagged = false;

    [SerializeField] private CaughtFishInventoryMB caughtInventory;

    // --- SFX�i�C���X�y�N�^�� AudioCueSO �����蓖�āj ---
    [SerializeField] private AudioCueSO sfxCast;   // �ނ�J�n�i������j
    [SerializeField] private AudioCueSO sfxHook;   // ������������
    [SerializeField] private AudioCueSO sfxReel;   // ���[�����グ�n��
    [SerializeField] private AudioCueSO sfxCash;   // ���/���Z
    [SerializeField] private AudioCueSO sfxMiss;   // ���n�[���̂Ƃ��i�C�Ӂj
                                                   // ���C�Ӓǉ��F�T���^���|����pSFX�i����΁j
    [SerializeField] private AudioCueSO sfxSharkOrSnag;

    // �A�ŁE���d�Đ��̗}���p
    private float _lastHookSfxTime;
    private const float HookSfxInterval = 0.08f;

    private PooledAudioSource _reelHandle; // �� �ǉ��F�Đ����̃n���h��

    private void Awake()
    {
        mainCamera = Camera.main;
        coll = GetComponent<Collider2D>();
        hookedFishes = new List<Fish>();
    


    }
    private void Start()
    {
        // �N������̘A�ő΍�F�����̊Ԃ�������
        _blockInputUntil = Time.unscaledTime + startDebounceSeconds;
        // �O�̂��ߏ����ʒu�E�e�𐮂���
        transform.SetParent(null);
        transform.position = Vector2.down * 6f;
    }


    void Update()
    {
        if(canMove && Input.GetMouseButton(0))
        {

            Vector3 vector = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 postion = transform.position;
            postion.x = vector.x;
            transform.position = postion;
        }
        
    }
    public void StartFishing()
    {

        Debug.Log("�ނ�J�n");
        // �A�ŁE���d�h�~�i�N������̃f�o�E���X + �i�s���u���b�N + ������canFishing�j
        if (Time.unscaledTime < _blockInputUntil) return;
        if (_inProgress || !canFishing) return;

        _inProgress = true;          // �� �ŏ�i�ŗ��Ă�
        canFishing = false;
        ResetFish();

        // �\���������Ȃ��ی�
        gameObject.SetActive(true);

        // �����F������
        if (sfxCast) AudioManager.Instance.PlaySFX(sfxCast, transform.position);

        // ���J�n���Ƀ��Z�b�g
        snagged = false;
        canFishing = false;
        GetFishCoin = 0;



        // UI �őI�񂾒l�𕉂ɂ��Đݒ�i���̕����֐i�ށj
        int maxReach = Mathf.Max(1, IdleManager.instance.CurrentLength);
        int uiDepth = Mathf.Clamp(lengthUI.SelectedDepth, 1, maxReach);

        Debug.Log(lengthUI.SelectedDepth+"�Z���N�g�f�B�[�v");

        length = -uiDepth;


        strength = IdleManager.instance.CurrentStrength;
        fishCount = 0;
        float time = (-length) * 0.1f;

        lengthUI.SetInteractable(false);

        cameraTween = mainCamera.transform.DOMoveY(length, 1 + time * 0.25f, false)
            .OnUpdate(delegate
              {
                  if (mainCamera.transform.position.y <= surfaceThresholdY)
                  {
                      transform.SetParent(mainCamera.transform);
                  }
              })
           .OnComplete(() =>
           {
               // ��ɓ��B�F�����ŃL���b�`�J�n
               coll.enabled = true;

           
               // �����㏸�J�n�i��ő҂��������Ȃ� DOVirtual.DelayedCall �����ށj
               cameraTween = mainCamera.transform.DOMoveY(0, time * 5f, false)
                   .OnUpdate(() =>
                   {
                       float cy = mainCamera.transform.position.y;

                       // ���J������ -5m ���z�����u�ԂɃt�b�N�����~�߂�i�e����O���Ay�Œ�j
                       if (transform.parent == mainCamera.transform && cy >= hookStopY)
                       {
                           transform.SetParent(null);
                           var p = transform.position;
                           p.y = hookStopY;          // �t�b�N��y�� -5m �ɌŒ�
                           transform.position = p;
                       }

                       // ������ StopFinshing �͌Ă΂Ȃ��i�Ō�� OnComplete �ɔC����j
                   })
                   .OnComplete(() =>
                   {
                       // �J����������(0)�܂ŏオ��؂�����I������
                       StopFinshing();
                   });
           });

        ScreenManager.Instance.ChangeScreen(Screens.GAME);
        coll.enabled = false;
        canMove = true;
        hookedFishes.Clear();   
    }

    void StopFinshing(bool forceMiss = false)
    {
        canMove = false;


        // ���ǉ��F�ނ�グ���͓����蔻�������
        coll.enabled = false;
        // �����F���[���J�n�i�グ�铮���ɓ������u�ԁj
        // ���[�����F�グ�n�߂Ŗ炷�i�܂����ĂȂ���΁j
        if (!forceMiss && sfxReel)
        {
            _reelHandle = AudioManager.Instance.PlaySFX(sfxReel, transform.position, true);
        }
        cameraTween.Kill(false);
        cameraTween = mainCamera.transform.DOMoveY(0, 2, false).OnUpdate(delegate
        {
            if (mainCamera.transform.position.y >= -11)
            {
                transform.SetParent(null);
                transform.position = new Vector2(transform.position.x, -6);


                // ���z�b�N��~�ŉ����~�߂�i����~�j
                if (_reelHandle != null)
                {
                    AudioManager.Instance.StopSFX(_reelHandle);
                    _reelHandle = null;
                }
            }
        }).OnComplete(delegate
        {
            transform.position = Vector2.down * 6;
            coll.enabled = true;
            int num = 0;

            // �����~�X�Ȃ���n�[���B�ʏ�͍��v
            if (!forceMiss)
            {
                for (int i = 0; i < hookedFishes.Count; i++)
                {
               
                    var fish = hookedFishes[i];
                    Debug.Log($"ADD {fish.Type.name} id={fish.Type.GetInstanceID()}");
                    if (fish == null || fish.Type == null) continue;
                        caughtInventory.Add(fish.Type, 1);
                    hookedFishes[i].transform.SetParent(null);
                    hookedFishes[i].ResetFish();
                    num += hookedFishes[i].Type.price;
                }
            }
            else
            {
                // ���s���͒މʂ��N���A�iUI��Clear�Ŕ�\���ɂȂ�j
                caughtInventory.ClearAll();
                ResetFish();
            }

            GetFishCoin = forceMiss ? 0 : num;


            
            ScreenManager.Instance.GetCoin = GetFishCoin;
            IdleManager.instance.wallet += GetFishCoin;

           

            // �����F���Z�i�R�C�� or �~�X�j
            if (GetFishCoin > 0)
            {
                if (sfxCash) AudioManager.Instance.PlayUISFX(sfxCash);
            }
            else
            {
                if (sfxMiss) AudioManager.Instance.PlayUISFX(sfxMiss);
            }



            ScreenManager.Instance.ChangeScreen(Screens.END);
          
            hookedFishes.Clear();
            lengthUI.SetInteractable(true);
        
            // ������͂̃f�o�E���X�i��ʑJ�ڒ���̌�^�b�v�΍�j
            _blockInputUntil = Time.unscaledTime + 0.15f;
            // �������ŃC���x���g�����N���A
         

            // ��ԉ���
            canFishing = true;
            _inProgress = false;
            GetFishCoin = 0;
        });
    }

    private void ResetFish()
    {
        caughtInventory.ClearAll();  // �� �N���A�֐���������΍��
        // �ނ�Ă����������Z�b�g���ď���
        for (int i = 0; i < hookedFishes.Count; i++)
        {
            hookedFishes[i].transform.SetParent(null);
            hookedFishes[i].ResetFish();
        }
    }

    // ���댯���ɓ��������Ƃ��̏����i���̉�͏I���Ɂj
    private void TriggerSnag(Fish hazard)
    {
        if (snagged) return; // ��d�h�~
        snagged = true;

        // ���o�FSFX & �t�b�N�������݂ɗh�炷�E�J�����������h�炷
        if (sfxSharkOrSnag) AudioManager.Instance.PlaySFX(sfxSharkOrSnag, transform.position);
        transform
            .DOShakeRotation(0.35f, Vector3.forward * 45f, 15, 90f, false)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        mainCamera.transform
            .DOShakePosition(0.25f, 0.2f, 10, 90f, false, true)
            .SetLink(mainCamera.gameObject, LinkBehaviour.KillOnDestroy);

        // �댯�����̂͂������Z�b�g���ė����i���������葱���h�~�j
        if (hazard != null)
        {
            hazard.Hooked();   // �������~�߂�
            hazard.transform.SetParent(null);
            hazard.ResetFish();
        }

        // �Ȍ�L���b�`�s��
        coll.enabled = false;

        // �T���ɓ��������Ƃ��͒Z�����ԂŋA��
        float returnTime = hazard != null && hazard.IsHazard ? hazardReturnTime : normalReturnTime;


        // ���߂ɋ����A�ҁi�R�C��0��END�j
        cameraTween?.Kill(false);
        cameraTween = mainCamera.transform.DOMoveY(0, returnTime, false)
            .OnUpdate(() =>
            {
                if (mainCamera.transform.position.y >= -11)
                {
                    transform.SetParent(null);
                    transform.position = new Vector2(transform.position.x, -6);
                }
            })
            .OnComplete(() =>
            {
                StopFinshing(forceMiss: true);
            });
    }

    private void OnTriggerEnter2D(Collider2D target)
    {
        if (!target.CompareTag("Fish")) return;

        // snagged ���͉����E��Ȃ�
        if (snagged) return;

        Fish compnment = target.GetComponent<Fish>();
        if (compnment == null) return;

        // ���댯���i�T���^��Q���j
        if (compnment.IsHazard)
        {
            TriggerSnag(compnment);
            return;
        }

        // --- ��������ʏ�̋��L���b�` ---
        if (fishCount == strength) return;

        fishCount++;

        if (sfxHook && Time.time - _lastHookSfxTime > HookSfxInterval)
        {
            AudioManager.Instance.PlaySFX(sfxHook, transform.position);
            _lastHookSfxTime = Time.time;
        }

        compnment.Hooked();
        hookedFishes.Add(compnment);
        target.transform.SetParent(transform);
        target.transform.position = hookedTransfrom.position;
        target.transform.rotation = hookedTransfrom.rotation;
        target.transform.localScale = Vector3.one;

        target.transform
            .DOShakeRotation(5, Vector3.forward * 45, 10, 90, false)
            .SetLoops(1, LoopType.Yoyo)
            .OnComplete(() => { target.transform.rotation = Quaternion.identity; });

        if (fishCount >= strength)
            StopFinshing();
    }

}
