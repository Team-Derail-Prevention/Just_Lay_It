using System;
using UnityEngine;

public class DroneRailDrop : MonoBehaviour
{
    private const float FallAcceleration = 30f;
    private const float RotateSpeed = 720f;

    private Vector3 _target;
    private Quaternion _rotation;
    private GameObject _ghost;
    private Action _onLanded;

    private float _fallSpeed;
    private bool _isLanded;

    public static void Begin(GameObject payload, Vector3 target, Quaternion rotation, GameObject ghost, Action onLanded)
    {
        DroneRailDrop drop = payload.AddComponent<DroneRailDrop>();

        drop._target = target;
        drop._rotation = rotation;
        drop._ghost = ghost;
        drop._onLanded = onLanded;
    }

    private void Update()
    {
        _fallSpeed += FallAcceleration * Time.deltaTime;

        Vector3 position = Vector3.MoveTowards(transform.position, _target, _fallSpeed * Time.deltaTime);
        Quaternion rotation = Quaternion.RotateTowards(transform.rotation, _rotation, RotateSpeed * Time.deltaTime);

        transform.SetPositionAndRotation(position, rotation);

        if (position == _target)
        {
            Land();
        }
    }

    private void Land()
    {
        _isLanded = true;

        transform.SetPositionAndRotation(_target, _rotation);

        if (_ghost != null)
        {
            Destroy(_ghost);
        }

        Action onLanded = _onLanded;

        Destroy(this);

        onLanded?.Invoke();
    }

    private void OnDestroy()
    {
        if (_isLanded)
        {
            return;
        }

        if (_ghost != null)
        {
            Destroy(_ghost);
        }
    }
}
