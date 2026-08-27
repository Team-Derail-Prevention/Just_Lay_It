using System;
using UnityEngine;

[RequireComponent(typeof(DroneMoveInput))]
public class Drone : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _moveSpeed = 4f;
    [SerializeField, Min(0f)] private float _slowDownDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float _arriveSpeed = 0.5f;
    [SerializeField, Min(0.001f)] private float _arriveDistance = 0.05f;

    public event Action OnArrived;

    private DroneMoveInput _moveInput;
    private IAgentMover _mover;

    private void Awake()
    {
        _moveInput = GetComponent<DroneMoveInput>();
        _mover = GetComponent<IAgentMover>();
    }

    public void MoveTo(Vector3 worldPosition)
    {
        _moveInput.SetTarget(worldPosition);
    }

    public void Stop()
    {
        _moveInput.ClearTarget();
    }

    private void Update()
    {
        if (_moveInput.HasTarget == false)
        {
            _mover.Move(Vector3.zero, 0f);

            return;
        }

        Vector3 offset = _moveInput.GetTargetPosition() - transform.position;

        offset.y = 0f;

        float distance = offset.magnitude;
        float step = _mover.CurrentVelocity.magnitude * Time.deltaTime;

        if (distance <= Mathf.Max(_arriveDistance, step))
        {
            Arrive();

            return;
        }

        _mover.Move(offset, GetDesiredSpeed(distance));
    }

    private float GetDesiredSpeed(float distance)
    {
        float moveSpeed = GetUpgradedMoveSpeed();

        if (_slowDownDistance <= 0f || distance >= _slowDownDistance)
        {
            return moveSpeed;
        }

        return Mathf.Lerp(_arriveSpeed, moveSpeed, distance / _slowDownDistance);
    }

    private float GetUpgradedMoveSpeed()
    {
        if (DroneManager.Instance == null)
        {
            return _moveSpeed;
        }

        return _moveSpeed * DroneManager.Instance.MoveSpeedMultiplier;
    }

    private void Arrive()
    {
        _mover.Warp(_moveInput.GetTargetPosition());
        _moveInput.ClearTarget();

        OnArrived?.Invoke();
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
