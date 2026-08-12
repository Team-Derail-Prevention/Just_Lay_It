using UnityEngine;

public class TransformMover : MonoBehaviour, IAgentMover
{
    [SerializeField, Min(0f)] private float _rotationSpeed = 15f;
    [SerializeField, Min(0f)] private float _acceleration = 12f;
    [SerializeField, Min(0f)] private float _deceleration = 18f;

    public Vector3 CurrentVelocity { get { return _currentVelocity; } }
    public Vector3 CurrentAcceleration { get { return _currentAcceleration; } }

    private Vector3 _currentVelocity;
    private Vector3 _currentAcceleration;

    public void Move(Vector3 direction, float speed)
    {
        Vector3 targetVelocity;

        if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
        {
            targetVelocity = Vector3.zero;
        }
        else
        {
            targetVelocity = direction.normalized * speed;
        }

        float rate;

        if (targetVelocity.sqrMagnitude >= _currentVelocity.sqrMagnitude)
        {
            rate = _acceleration;
        }
        else
        {
            rate = _deceleration;
        }

        Vector3 previousVelocity = _currentVelocity;

        _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, rate * Time.deltaTime);

        if (Time.deltaTime > 0f)
        {
            _currentAcceleration = (_currentVelocity - previousVelocity) / Time.deltaTime;
        }

        transform.position += _currentVelocity * Time.deltaTime;

        if (_currentVelocity.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion look = Quaternion.LookRotation(_currentVelocity.normalized, Vector3.up);
        float turn = 1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(transform.rotation, look, turn);
    }

    public void Warp(Vector3 position)
    {
        transform.position = position;

        _currentVelocity = Vector3.zero;
        _currentAcceleration = Vector3.zero;
    }
}
