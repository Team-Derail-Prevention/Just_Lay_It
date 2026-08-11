using System;
using UnityEngine;

[RequireComponent(typeof(DroneMoveInput))]
public class Drone : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _moveSpeed = 4f;

    public event Action<CellPos> OnArrived;

    private DroneMoveInput _moveInput;
    private IAgentMovementInput _input;
    private IAgentMover _mover;

    private void Awake()
    {
        _moveInput = GetComponent<DroneMoveInput>();
        _input = _moveInput;
        _mover = GetComponent<IAgentMover>();
    }

    public bool MoveTo(CellPos cell)
    {
        return _moveInput.SetTarget(cell);
    }

    private void Update()
    {
        if (_moveInput.HasTarget == false)
        {
            return;
        }

        Vector3 offset = _moveInput.GetTargetPosition() - transform.position;

        offset.y = 0f;

        float step = _moveSpeed * Time.deltaTime;

        if (offset.magnitude <= step)
        {
            Arrive();

            return;
        }

        Vector2 movement = _input.MovementInput;

        _mover.Move(new Vector3(movement.x, 0f, movement.y), _moveSpeed);
    }

    private void Arrive()
    {
        CellPos arrivedCell = _moveInput.TargetCell;

        _mover.Warp(_moveInput.GetTargetPosition());
        _moveInput.ClearTarget();

        OnArrived?.Invoke(arrivedCell);
    }

    private void OnDrawGizmosSelected()
    {
        if (_moveInput == null)
        {
            return;
        }

        if (_moveInput.HasTarget == false)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Vector3 target = _moveInput.GetTargetPosition();

        Gizmos.DrawLine(transform.position, target);
        Gizmos.DrawWireSphere(target, 0.25f);
    }
}
