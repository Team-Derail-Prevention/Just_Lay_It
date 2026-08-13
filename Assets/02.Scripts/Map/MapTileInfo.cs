using UnityEngine;

public class MapTileInfo : MonoBehaviour
{
    [Header("Grid Information")]
    [SerializeField] private Vector2Int _localGridCoordinate;
    [SerializeField] private Vector3Int _parentMapGridPos;   
    [SerializeField] private bool _canInstallRail = true;
    [SerializeField] private bool _hasRail = false;      

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

    public void InitTile(Vector2Int localCoordinate, Vector3Int parentMapGridPos, bool canInstallRail = true)
    {
        _localGridCoordinate = localCoordinate;
        _parentMapGridPos = parentMapGridPos;
        _canInstallRail = canInstallRail;
        _hasRail = false;
    }

    public string GetTileDebugInfo()
    {
        return $"[Tile] Map: {_parentMapGridPos}, Local: {_localGridCoordinate}, Railable: {_canInstallRail}, HasRail: {_hasRail}";
    }
}