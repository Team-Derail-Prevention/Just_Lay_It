using UnityEngine;

public class TransformMover : MonoBehaviour, IAgentMover
{
    [SerializeField, Min(0f)] private float _rotationSpeed = 15f;

    public Vector3 CurrentVelocity { get { return _currentVelocity; } }

    private Vector3 _currentVelocity;

    public void Move(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            _currentVelocity = Vector3.zero;

            return;
        }

        _currentVelocity = direction.normalized * speed;

        transform.position += _currentVelocity * Time.deltaTime;

        Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, look, _rotationSpeed * Time.deltaTime);
    }

    public void Warp(Vector3 position)
    {
        transform.position = position;

        _currentVelocity = Vector3.zero;
    }
}
