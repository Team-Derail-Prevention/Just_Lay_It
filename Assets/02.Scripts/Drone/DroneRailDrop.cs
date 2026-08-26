using System;
using UnityEngine;

public class DroneRailDrop : MonoBehaviour
{
    private const float FallAcceleration = 30f;
    private const float RotateSpeed = 720f;

    private Vector3 _target;
    private Quaternion _rotation;
    private GameObject _ghost;
    private Action<GameObject> _onLanded;

    private float _fallSpeed;
    private bool _isLanded;
    private bool _isHandedOff;

    public static void Begin(GameObject payload, Vector3 target, Quaternion rotation, GameObject ghost, Action<GameObject> onLanded)
    {
        DroneRailDrop drop = payload.AddComponent<DroneRailDrop>();

        drop._target = target;
        drop._rotation = rotation;
        drop._ghost = ghost;
        drop._onLanded = onLanded;
    }

    public static bool TryHandOff(GameObject oldPayload, GameObject newPayload)
    {
        if (oldPayload == null || newPayload == null)
        {
            return false;
        }

        DroneRailDrop drop = oldPayload.GetComponent<DroneRailDrop>();

        if (drop == null || drop._isLanded || drop._isHandedOff)
        {
            return false;
        }

        Vector3 target = drop._target;
        Quaternion rotation = drop._rotation;
        GameObject ghost = drop._ghost;
        Action<GameObject> onLanded = drop._onLanded;

        drop._isHandedOff = true;
        drop._ghost = null;
        drop._onLanded = null;

        Begin(newPayload, target, rotation, ghost, onLanded);

        return true;
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

        Action<GameObject> onLanded = _onLanded;
        GameObject landed = gameObject;

        Destroy(this);

        onLanded?.Invoke(landed);
    }

    private void OnDestroy()
    {
        if (_isLanded || _isHandedOff)
        {
            return;
        }

        if (_ghost != null)
        {
            Destroy(_ghost);
        }

        Action<GameObject> onLanded = _onLanded;

        _onLanded = null;

        Debug.LogWarning($"[순번] 낙하 중이던 레일이 사라져 순번을 되돌려줍니다: {name}");

        onLanded?.Invoke(null);
    }
}
