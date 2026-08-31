using UnityEngine;

public class TransformMover : IAgentMover
{
    private readonly Transform _transform;
    private readonly float _rotationSpeed;
    private readonly float _acceleration;
    private readonly float _deceleration;

    private Vector3 _currentVelocity;
    private Vector3 _currentAcceleration;

    public Vector3 CurrentVelocity { get { return _currentVelocity; } }
    public Vector3 CurrentAcceleration { get { return _currentAcceleration; } }

    public TransformMover(Transform transform, float rotationSpeed, float acceleration, float deceleration)
    {
        _transform = transform;
        _rotationSpeed = rotationSpeed;
        _acceleration = acceleration;
        _deceleration = deceleration;
    }

    public void Move(Vector3 direction, float speed)
    {
        Vector3 targetVelocity = GetTargetVelocity(direction, speed);
        float rate = GetChangeRate(targetVelocity);

        Vector3 previousVelocity = _currentVelocity;

        _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, rate * Time.deltaTime);

        if (Time.deltaTime > 0f)
        {
            _currentAcceleration = (_currentVelocity - previousVelocity) / Time.deltaTime;
        }

        _transform.position += _currentVelocity * Time.deltaTime;

        ApplyLookRotation();
    }

    public void Warp(Vector3 position)
    {
        _transform.position = position;

        _currentVelocity = Vector3.zero;
        _currentAcceleration = Vector3.zero;
    }

    private Vector3 GetTargetVelocity(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
        {
            return Vector3.zero;
        }

        return direction.normalized * speed;
    }

    private float GetChangeRate(Vector3 targetVelocity)
    {
        if (targetVelocity.sqrMagnitude >= _currentVelocity.sqrMagnitude)
        {
            return _acceleration;
        }

        return _deceleration;
    }

    private void ApplyLookRotation()
    {
        if (_currentVelocity.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion look = Quaternion.LookRotation(_currentVelocity.normalized, Vector3.up);
        float turn = 1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime);

        _transform.rotation = Quaternion.Slerp(_transform.rotation, look, turn);
    }
}
