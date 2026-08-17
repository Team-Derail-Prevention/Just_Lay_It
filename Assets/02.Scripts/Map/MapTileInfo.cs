using UnityEngine;

public class MapTileInfo : MonoBehaviour
{
    [Header("Grid Information")]
    [SerializeField] private Vector2Int _localGridCoordinate;
    [SerializeField] private Vector3Int _parentMapGridPos;
    [SerializeField] private bool _canInstallRail = true;
    [SerializeField] private bool _hasRail = false;

    [Header("Layer & Detection Settings")]
    [Tooltip("비어있는 땅일 때 적용할 그라운드 레이어 이름")]
    [SerializeField] private string _groundLayerName = "Ground";
    [Tooltip("타일 위 오브젝트(장애물, 자재 등)에 적용할 오브젝트 레이어 이름")]
    [SerializeField] private string _objectLayerName = "OnGround";
    [Tooltip("타일 위 오브젝트 감지를 위한 체크 반경 (타일 크기 대비 조절)")]
    [SerializeField] private float _checkRadius = 0.8f;
    [Tooltip("타일 위 오브젝트 감지를 위한 높이 범위")]
    [SerializeField] private float _checkHeight = 3.0f;

    private LayerMask _objectLayerMask;

    public Vector2Int LocalGridCoordinate => _localGridCoordinate;
    public Vector3Int ParentMapGridPos => _parentMapGridPos;
    public bool CanInstallRail
    {
        get => _canInstallRail;
        set => _canInstallRail = value;
    }
    public bool HasRail
    {
        get => _hasRail;
        set => _hasRail = value;
    }

    public string CurrentLayerName => LayerMask.LayerToName(gameObject.layer);

    private void Awake()
    {
        int layerIdx = LayerMask.NameToLayer(_objectLayerName);
        if (layerIdx != -1)
        {
            _objectLayerMask = 1 << layerIdx;
        }
        else
        {
            Debug.LogWarning($"[MapTileInfo] '{_objectLayerName}' 레이어가 프로젝트에 존재하지 않습니다! 기본 레이어로 대체합니다.");
            _objectLayerMask = 1 << 0; 
        }
    }

    public void InitTile(Vector2Int localCoordinate, Vector3Int parentMapGridPos, bool canInstallRail = true)
    {
        _localGridCoordinate = localCoordinate;
        _parentMapGridPos = parentMapGridPos;
        _canInstallRail = canInstallRail;
        _hasRail = false;

        UpdateTileStateByOccupant();
    }

    public void UpdateTileStateByOccupant()
    {
        int groundLayerIdx = LayerMask.NameToLayer(_groundLayerName);
        int objectLayerIdx = LayerMask.NameToLayer(_objectLayerName);

        Vector3 center = transform.position + Vector3.up * (_checkHeight * 0.5f);
        Vector3 halfExtents = new Vector3(_checkRadius, _checkHeight * 0.5f, _checkRadius);

        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, _objectLayerMask);

        bool hasOccupant = false;
        foreach (Collider col in hitColliders)
        {
            if (col.gameObject != gameObject && col.transform.root != transform.root)
            {
                hasOccupant = true;
                break;
            }
        }

        if (hasOccupant)
        {
            gameObject.layer = objectLayerIdx != -1 ? objectLayerIdx : 0;
            _canInstallRail = false;
        }
        else
        {
            if (groundLayerIdx != -1)
            {
                gameObject.layer = groundLayerIdx;
            }
            _canInstallRail = true;
        }
    }

    public string GetTileDebugInfo()
    {
        return $"[Tile] Map: {_parentMapGridPos}, Local: {_localGridCoordinate}, Railable: {_canInstallRail}, HasRail: {_hasRail}, Layer: {CurrentLayerName}";
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _canInstallRail ? Color.green : Color.red;
        Vector3 center = transform.position + Vector3.up * (_checkHeight * 0.5f);
        Vector3 size = new Vector3(_checkRadius * 2f, _checkHeight, _checkRadius * 2f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}