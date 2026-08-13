using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MaterialObject : BaseColliderTrigger
{
    public static event Action<MaterialObject, int> OnMaterialObjectCollected;

    [Header("Material Object Settings")]
    [SerializeField] private string _materialObjectID;
    [SerializeField] private int _materialObjectAmount;

    [Header("상태 변화 및 수집 연출 설정")]
    [SerializeField] private float _brokenScale = 0.5f;
    [SerializeField] private float _hoverHeight = 0.5f;
    [SerializeField] private float _shakeDuration = 0.5f;

    [Header("화면 연출 설정 (좌상단 수집)")]
    [SerializeField] private Vector2 _targetViewportPosition = new Vector2(0f, 1f);
    [SerializeField] private float _cameraDepthOffset = 10f;

    [Header("채굴 및 흔들림 연출 설정")]
    [SerializeField] private float _defaultMiningDuration = 3.0f;
    [SerializeField] private float _shakeIntensity = 0.1f;

    private bool _isMining = false;
    private bool _isBroken = false;
    private bool _isCollected = false;
    private Collider _itemCollider;
    private Vector3 _initialLocalScale;

    public string MaterialId => _materialObjectID;
    public int MaterialAmount => _materialObjectAmount;
    public bool IsBroken => _isBroken;
    public bool IsMining => _isMining;

    // TODO: 드론 이벤트 구독필요

    protected void OnEnable()
    {
        _itemCollider = GetComponent<Collider>();
        _initialLocalScale = transform.localScale;

        // TODO: 데이터 초기화 위치
    }

    private void Update()
    {
        // [테스트용 입력] 마이너스 : "채굴/수집 시작 이벤트"
        if (!_isMining && !_isBroken && !_isCollected && Input.GetKeyDown(KeyCode.Minus))
        {
            ReceiveDroneSignalAndStart();
        }
    }

    protected override bool CanInteract(Collider target)
    {
        return !_isMining && !_isBroken && !_isCollected;
    }

    protected override void HandleInteraction(Collider target)
    {
        ReceiveDroneSignalAndStart();
    }

    private void ReceiveDroneSignalAndStart()
    {
        if (_isMining || _isBroken || _isCollected)
        {
            return;
        }

        Debug.Log($"[MaterialObject] '{_materialObjectID}' 드론 수집 신호(이벤트) 수신 -> 채굴 및 수집 시작");
        StartMiningAndCollectionProcessAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid StartMiningAndCollectionProcessAsync(CancellationToken cancellationToken)
    {
        _isMining = true;
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < _defaultMiningDuration && !cancellationToken.IsCancellationRequested)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _defaultMiningDuration;

            transform.localScale = _initialLocalScale * (1f + Mathf.Sin(elapsedTime * 20f) * (0.1f * (1f - t * 0.5f)));
            transform.position = startPos + UnityEngine.Random.insideUnitSphere * _shakeIntensity;
            transform.position = new Vector3(transform.position.x, startPos.y, transform.position.z);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        transform.position = startPos;

        BreakObject();

        await CollectItem(cancellationToken);
    }

    private void BreakObject()
    {
        _isMining = false;
        _isBroken = true;
        transform.localScale = Vector3.one * _brokenScale;
        transform.position += Vector3.up * _hoverHeight;

        if (_itemCollider != null)
        {
            _itemCollider.isTrigger = true;
        }

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

        // TODO: 수집 시 데이터 업데이트 위치

        // 수집 이벤트 발신 (중앙역 창고나 UI 등)
        OnMaterialObjectCollected?.Invoke(this, _materialObjectAmount);

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

        // TODO: Destroy 대신 PoolManager로 수정
        Destroy(gameObject);
    }
}