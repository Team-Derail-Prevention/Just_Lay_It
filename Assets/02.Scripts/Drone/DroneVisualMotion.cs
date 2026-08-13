using UnityEngine;

public class DroneVisualMotion : MonoBehaviour
{
    [SerializeField] private Transform _visual;

    [SerializeField, Min(0f)] private float _maxTiltAngle = 20f;
    [SerializeField, Min(0f)] private float _tiltSmoothTime = 0.15f;
    [SerializeField, Min(0.01f)] private float _tiltReferenceAcceleration = 12f;

    [SerializeField, Min(0f)] private float _bobAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float _bobFrequency = 1.5f;

    private IAgentMover _mover;
    private Vector3 _visualLocalOrigin;
    private Vector2 _currentTilt;
    private Vector2 _tiltVelocity;

    private void Awake()
    {
        _mover = GetComponent<IAgentMover>();

        if (_visual == null)
        {
            return;
        }

        _visualLocalOrigin = _visual.localPosition;
    }

    private void LateUpdate()
    {
        if (_visual == null || _mover == null)
        {
            return;
        }

        ApplyTilt();
        ApplyBob();
    }

    private void ApplyTilt()
    {
        Vector3 localAcceleration = transform.InverseTransformDirection(_mover.CurrentAcceleration);

        float pitch = Mathf.Clamp(localAcceleration.z / _tiltReferenceAcceleration, -1f, 1f);
        float roll = Mathf.Clamp(localAcceleration.x / _tiltReferenceAcceleration, -1f, 1f);

        Vector2 targetTilt = new Vector2(pitch, roll) * _maxTiltAngle;

        _currentTilt = Vector2.SmoothDamp(_currentTilt, targetTilt, ref _tiltVelocity, _tiltSmoothTime);

        _visual.localRotation = Quaternion.Euler(_currentTilt.x, 0f, -_currentTilt.y);
    }

    private void ApplyBob()
    {
        float offset = Mathf.Sin(Time.time * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;

        _visual.localPosition = _visualLocalOrigin + new Vector3(0f, offset, 0f);
    }
}
