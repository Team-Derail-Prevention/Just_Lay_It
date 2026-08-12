using UnityEngine;

public class DroneMoveInput : MonoBehaviour
{
    [SerializeField] private GridMapBase _grid;

    public CellPos TargetCell { get { return _targetCell; } }
    public bool HasTarget { get { return _hasTarget; } }

    private CellPos _targetCell;
    private bool _hasTarget;

    public bool SetTarget(CellPos cell)
    {
        if (IsWalkable(cell) == false)
        {
            return false;
        }

        _targetCell = cell;
        _hasTarget = true;

        return true;
    }

    public void ClearTarget()
    {
        _hasTarget = false;
    }

    public Vector3 GetTargetPosition()
    {
        if (_grid == null)
        {
            return transform.position;
        }

        Vector3 world = _grid.ConvertCellToWorld(_targetCell);

        world.y = transform.position.y;

        return world;
    }

    public bool IsWalkable(CellPos cell)
    {
        if (_grid == null)
        {
            return false;
        }

        return _grid.IsInside(cell);
    }
}
