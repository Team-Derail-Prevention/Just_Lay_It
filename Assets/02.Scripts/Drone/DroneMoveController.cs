using System;
using UnityEngine;

public class DroneMoveController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField, Min(0.1f)] private float _moveSpeed = 4f;
    [SerializeField, Min(0f)] private float _slowDownDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float _arriveSpeed = 0.5f;
    [SerializeField, Min(0.001f)] private float _arriveDistance = 0.05f;

    [Header("가감속 / 회전")]
    [SerializeField, Min(0f)] private float _rotationSpeed = 15f;
    [SerializeField, Min(0f)] private float _acceleration = 12f;
    [SerializeField, Min(0f)] private float _deceleration = 18f;

    public event Action OnArrived;

    public Vector3 TargetPosition { get { return _targetPosition; } }
    public bool HasTarget { get { return _hasTarget; } }

    public IAgentMover Mover
    {
        get
        {
            if (_mover == null)
            {
                _mover = new TransformMover(transform, _rotationSpeed, _acceleration, _deceleration);
            }

            return _mover;
        }
    }

    private IAgentMover _mover;

    private Vector3 _targetPosition;
    private bool _hasTarget;

    public void MoveTo(Vector3 worldPosition)
    {
        _targetPosition = worldPosition;
        _hasTarget = true;
    }

    public void Stop()
    {
        _hasTarget = false;
    }

    private void Update()
    {
        if (_hasTarget == false)
        {
            Mover.Move(Vector3.zero, 0f);

            return;
        }

        Vector3 offset = GetFlatTarget() - transform.position;
        float distance = offset.magnitude;

        if (HasArrived(distance))
        {
            Arrive();

            return;
        }

        Mover.Move(offset, GetDesiredSpeed(distance));
    }

    private bool HasArrived(float distance)
    {
        float step = Mover.CurrentVelocity.magnitude * Time.deltaTime;

        return distance <= Mathf.Max(_arriveDistance, step);
    }

    private void Arrive()
    {
        Mover.Warp(GetFlatTarget());

        _hasTarget = false;

        OnArrived?.Invoke();
    }

    private Vector3 GetFlatTarget()
    {
        Vector3 world = _targetPosition;

        world.y = transform.position.y;

        return world;
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

    private void OnDrawGizmosSelected()
    {
        if (_hasTarget == false)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Vector3 target = GetFlatTarget();

        Gizmos.DrawLine(transform.position, target);
        Gizmos.DrawWireSphere(target, 0.25f);
    }
}
