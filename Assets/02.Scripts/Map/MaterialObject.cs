using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MaterialObject : BaseColliderTrigger
{
    public static event Action<MaterialObjectData> OnMaterialObjectCollected;
    public event Action OnMiningFinished;

    [Header("Material Object Settings")]
    [SerializeField] private string _materialObjectID;
    [SerializeField] private int _materialObjectAmount;
    [SerializeField]private string _materialObjectType;

    private MaterialObjectData _myData;

    [Header("상태 변화 및 수집 연출 설정")]
    [SerializeField] private float _brokenScale = 0.5f;
    [SerializeField] private float _hoverHeight = 0.5f;
    [SerializeField] private float _shakeDuration = 0.5f;

    [Header("화면 연출 설정 (좌상단 수집)")]
    [SerializeField] private Vector2 _targetViewportPosition = new Vector2(0f, 1f);
    [SerializeField] private float _cameraDepthOffset = 10f;

    [Header("채굴 및 흔들림 연출 설정")]
    [Tooltip("채굴 속도 (초 단위)")]
    [SerializeField] private float _defaultMiningDuration = 3.0f;
    [SerializeField] private float _shakeIntensity = 0.1f;

    private bool _isMining = false;
    private bool _isBroken = false;
    private bool _isCollected = false;
    private Collider _itemCollider;
    private Vector3 _initialLocalScale;

    public string MaterialId => _materialObjectID;
    public string MaterialType => _materialObjectType;
    public int MaterialAmount => _materialObjectAmount;

    public bool IsBroken => _isBroken;
    public bool IsMining => _isMining;


    protected void OnEnable()
    {
        _itemCollider = GetComponent<Collider>();
        _initialLocalScale = transform.localScale;
    }

    private void Update()
    {
        if (!_isMining && !_isBroken && !_isCollected && Input.GetKeyDown(KeyCode.Minus))
        {
            ReceiveDroneSignalAndStart();
        }
    }

    public void InitializeData(MaterialObjectData data)
    {
        _myData = data; 

        _materialObjectID = data.Id;
        _materialObjectType = data.Type;
        _materialObjectAmount = data.amount;

        Debug.Log($"[MaterialObject 초기화 완료] ID: {_materialObjectID}, Type: {_materialObjectType}, Amount: {_materialObjectAmount}");
    }

    protected override bool CanInteract(Collider target)
    {
        return !_isMining && !_isBroken && !_isCollected;
    }

    protected override void HandleInteraction(Collider target)
    {
        ReceiveDroneSignalAndStart();
    }

    public bool TryStartMining(float speedMultiplier)
    {
        if (_isMining || _isBroken || _isCollected)
        {
            return false;
        }

        ReceiveDroneSignalAndStart(Mathf.Max(0.01f, speedMultiplier));

        return true;
    }

    private void ReceiveDroneSignalAndStart(float speedMultiplier = 1f)
    {
        if (_isMining || _isBroken || _isCollected)
        {
            return;
        }

        Debug.Log($"{_materialObjectID},{_materialObjectType},{_materialObjectAmount}");
        Debug.Log($"[MaterialObject] '{_materialObjectID}' 드론 수집 신호(이벤트) 수신 -> 채굴 및 수집 시작");
        StartMiningAndCollectionProcessAsync(speedMultiplier, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid StartMiningAndCollectionProcessAsync(float speedMultiplier, CancellationToken cancellationToken)
    {
        _isMining = true;
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        float miningDuration = _defaultMiningDuration / speedMultiplier;

        while (elapsedTime < miningDuration && !cancellationToken.IsCancellationRequested)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / miningDuration;

            transform.localScale = _initialLocalScale * (1f + Mathf.Sin(elapsedTime * 20f) * (0.1f * (1f - t * 0.5f)));
            transform.position = startPos + UnityEngine.Random.insideUnitSphere * _shakeIntensity;
            transform.position = new Vector3(transform.position.x, startPos.y, transform.position.z);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        transform.position = startPos;

        BreakObject();

        OnMiningFinished?.Invoke();

        await CollectItem(cancellationToken);
    }

    private void BreakObject()
    {
        _isMining = false;
        _isBroken = true;
        transform.localScale = Vector3.one * _brokenScale;
        transform.position += Vector3.up * _hoverHeight;

        Debug.Log($"[MaterialObject] '{_materialObjectID}' 채굴 완료 및 파괴 전환 (수집 연출로 자동 전환)");
    }

    private async UniTask CollectItem(CancellationToken cancellationToken)
    {
        if (_isCollected)
        {
            return;
        }

        _isCollected = true;
        if (_itemCollider != null)
        {
            _itemCollider.enabled = false;
        }

        GameManager.Map?.RefreshTileAtWorldPosition(transform.position);

        if (_myData != null)
        {
            OnMaterialObjectCollected?.Invoke(_myData);
        }
        else
        {
            Debug.LogWarning($"[MaterialObject] 데이터가 비어 있습니다! 맵메이커의 주입이 정상적으로 이루어졌는지 확인하세요.");
        }

        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        Vector3 targetWorldPosition = startPos + Vector3.up * 5f;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            targetWorldPosition = mainCam.ViewportToWorldPoint(new Vector3(_targetViewportPosition.x, _targetViewportPosition.y, _cameraDepthOffset));
        }

        float elapsedTime = 0f;
        while (elapsedTime < _shakeDuration && !cancellationToken.IsCancellationRequested)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _shakeDuration);

            transform.position = Vector3.Lerp(startPos, targetWorldPosition, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        Destroy(gameObject);
    }
}