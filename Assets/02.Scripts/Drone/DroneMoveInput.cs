using UnityEngine;

public class DroneMoveInput : MonoBehaviour
{
    public Vector3 TargetPosition { get { return _targetPosition; } }
    public bool HasTarget { get { return _hasTarget; } }

    private Vector3 _targetPosition;
    private bool _hasTarget;

    public void SetTarget(Vector3 worldPosition)
    {
        _targetPosition = worldPosition;
        _hasTarget = true;
    }

    public void ClearTarget()
    {
        _hasTarget = false;
    }

    // 드론은 자기 높이를 유지한 채 수평으로만 이동합니다.
    public Vector3 GetTargetPosition()
    {
        Vector3 world = _targetPosition;

        world.y = transform.position.y;

        return world;
    }
}
