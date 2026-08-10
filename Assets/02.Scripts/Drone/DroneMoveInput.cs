using UnityEngine;

public class DroneMoveInput : MonoBehaviour, IAgentMovementInput
{
    [SerializeField] private TestGridMap _grid;

    public Vector2 MovementInput { get { return _movementInput; } }
    public CellPos TargetCell { get { return _targetCell; } }
    public bool HasTarget { get { return _hasTarget; } }

    private Vector2 _movementInput;
    private CellPos _targetCell;
    private bool _hasTarget;

    public bool SetTarget(CellPos cell)
    {
        if (_grid == null)
        {
            return false;
        }

        if (_grid.IsWalkable(cell) == false)
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
        _movementInput = Vector2.zero;
    }

    public Vector3 GetTargetPosition()
    {
        Vector3 world = _grid.ConvertCellToWorld(_targetCell);

        world.y = transform.position.y;

        return world;
    }

    private void Update()
    {
        if (_hasTarget == false)
        {
            _movementInput = Vector2.zero;

            return;
        }

        Vector3 offset = GetTargetPosition() - transform.position;

        offset.y = 0f;

        Vector3 direction = offset.normalized;

        _movementInput = new Vector2(direction.x, direction.z);
    }
}
