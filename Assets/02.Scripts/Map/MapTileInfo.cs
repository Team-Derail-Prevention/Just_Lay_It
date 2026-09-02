using UnityEngine;
using UnityEngine.Serialization;

public class MapTileInfo : MonoBehaviour
{
    private const string DefaultLayerName = "Default";

    [Header("Runtime Debug")]
    [SerializeField] private bool _hasOnGroundOccupant;

    [SerializeField] private GameObject _detectedOccupant;
    [SerializeField] private string _detectedOccupantName;

    [Header("Grid Information")]
    [SerializeField] private Vector2Int _localGridCoordinate;
    [SerializeField] private Vector3Int _parentMapGridPos;
    [FormerlySerializedAs("_canInstallRail")]
    [SerializeField] private bool _baseCanInstallRail = true;
    [SerializeField] private bool _hasRail;

    [Header("Layer & Detection Settings")]
    [Tooltip("비어있는 땅일 때 적용할 그라운드 레이어 이름")]
    [SerializeField] private string _groundLayerName = "Ground";
    [Tooltip("타일 위 점유 오브젝트를 찾는 레이어 이름")]
    [SerializeField] private string _objectLayerName = "OnGround";
    [Tooltip("타일 위 오브젝트 감지를 위한 체크 반경 (타일 크기 대비 조절)")]
    [SerializeField] private float _checkRadius = 0.8f;
    [Tooltip("타일 위 오브젝트 감지를 위한 높이 범위")]
    [SerializeField] private float _checkHeight = 3.0f;

    [SerializeField] private bool _useTerminalReservedArea;

    private int _groundLayerIndex;
    private int _defaultLayerIndex;
    private LayerMask _objectLayerMask;

    public Vector2Int LocalGridCoordinate => _localGridCoordinate;
    public Vector3Int ParentMapGridPos => _parentMapGridPos;

    private static readonly Vector2Int TerminalCenter = Vector2Int.zero;


    public bool IsTerminalReservedArea =>
        _useTerminalReservedArea &&
        Mathf.Abs(_localGridCoordinate.x - TerminalCenter.x) <= 1 &&
        Mathf.Abs(_localGridCoordinate.y - TerminalCenter.y) <= 1;

    public bool IsTerminalEntrance =>
        _localGridCoordinate == new Vector2Int(0, 2) ||
        _localGridCoordinate == new Vector2Int(0, -2) ||
        _localGridCoordinate == new Vector2Int(2, 0) ||
        _localGridCoordinate == new Vector2Int(-2, 0);

    public bool CanInstallRail
    {
        get => _baseCanInstallRail
            && !_hasRail
            && !_hasOnGroundOccupant
            && !IsTerminalReservedArea;
        set => _baseCanInstallRail = value;
    }
    public bool HasRail
    {
        get => _hasRail;
        set => _hasRail = value;
    }
    public string CurrentLayerName => LayerMask.LayerToName(gameObject.layer);

    private void Awake()
    {
        CacheLayerIndices();
    }

    public void InitTile(Vector2Int localCoordinate, Vector3Int parentMapGridPos, bool canInstallRail = true)
    {
        _localGridCoordinate = localCoordinate;
        _parentMapGridPos = parentMapGridPos;
        _baseCanInstallRail = canInstallRail;
        _hasRail = false;

        if (IsTerminalReservedArea)
        {
            _baseCanInstallRail = false;
            gameObject.layer = _defaultLayerIndex != -1 ? _defaultLayerIndex : 0;
        }
        else
        {
            ApplyVisualLayer();
        }
    }

    public void SetParentMapGridPosition(Vector3Int parentMapGridPos)
    {
        _parentMapGridPos = parentMapGridPos;
    }

    public void SetTerminalReservedAreaEnabled(bool enabled)
    {
        _useTerminalReservedArea = enabled;

        ApplyVisualLayer();
    }

    public void RefreshOccupancy()
    {
        Collider[] hitColliders = Physics.OverlapBox(
            transform.position + Vector3.up * (_checkHeight * 0.5f),
            new Vector3(_checkRadius, _checkHeight * 0.5f, _checkRadius),
            Quaternion.identity,
            _objectLayerMask,
            QueryTriggerInteraction.Collide);

        _hasOnGroundOccupant = false;
        _detectedOccupant = null;
        _detectedOccupantName = string.Empty;

        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            _hasOnGroundOccupant = true;

            _detectedOccupant = hitCollider.gameObject;
            _detectedOccupantName = hitCollider.gameObject.name;

            break;
        }

        ApplyVisualLayer();
    }

    public void SetBakedOccupancy(bool hasOnGroundOccupant)
    {
        _hasOnGroundOccupant = hasOnGroundOccupant;
        ApplyVisualLayer();
    }

    public string GetTileDebugInfo()
    {
        return $"[Tile] Map: {_parentMapGridPos}, Local: {_localGridCoordinate}, Railable: {CanInstallRail}, HasRail: {_hasRail}, Layer: {CurrentLayerName}";
    }

    private void CacheLayerIndices()
    {
        _groundLayerIndex = LayerMask.NameToLayer(_groundLayerName);
        _defaultLayerIndex = LayerMask.NameToLayer(DefaultLayerName);

        int objectLayerIndex = LayerMask.NameToLayer(_objectLayerName);
        if (objectLayerIndex == -1)
        {
            Debug.LogWarning($"[MapTileInfo] '{_objectLayerName}' 레이어가 프로젝트에 없습니다. 점유물 탐색을 수행하지 않습니다.", this);
            _objectLayerMask = 0;
            return;
        }

        _objectLayerMask = 1 << objectLayerIndex;
    }

    private void ApplyVisualLayer()
    {
        if (IsTerminalReservedArea)
        {
            gameObject.layer = _defaultLayerIndex != -1 ? _defaultLayerIndex : 0;
            return;
        }

        if (_hasOnGroundOccupant)
        {
            gameObject.layer = _defaultLayerIndex != -1 ? _defaultLayerIndex : 0;
            return;
        }

        if (_groundLayerIndex != -1)
        {
            gameObject.layer = _groundLayerIndex;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = CanInstallRail ? Color.green : Color.red;
        Vector3 center = transform.position + Vector3.up * (_checkHeight * 0.5f);
        Vector3 size = new Vector3(_checkRadius * 2f, _checkHeight, _checkRadius * 2f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}