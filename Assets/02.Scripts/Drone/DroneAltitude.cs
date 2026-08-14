using UnityEngine;

[RequireComponent(typeof(DroneStateMachine))]
public class DroneAltitude : MonoBehaviour
{
    [Header("높이")]
    [SerializeField, Min(0f)] private float _cruiseHeight = 3f;
    [SerializeField, Min(0f)] private float _workGap = 0.5f;

    [Header("속도")]
    [SerializeField, Min(0.1f)] private float _descendSpeed = 4f;
    [SerializeField, Min(0.1f)] private float _ascendSpeed = 3f;

    private DroneStateMachine _stateMachine;

    private void Awake()
    {
        _stateMachine = GetComponent<DroneStateMachine>();
    }

    private void Update()
    {
        float targetHeight = GetTargetHeight();
        float speed = GetSpeed(targetHeight);

        Vector3 position = transform.position;

        position.y = Mathf.MoveTowards(position.y, targetHeight, speed * Time.deltaTime);

        transform.position = position;
    }

    private float GetTargetHeight()
    {
        float workHeight = GetWorkHeight();

        if (_stateMachine.State == DroneState.Working)
        {
            return workHeight;
        }

        if (workHeight > _cruiseHeight)
        {
            return workHeight;
        }

        return _cruiseHeight;
    }

    private float GetWorkHeight()
    {

        MaterialObject target = _stateMachine.WorkTarget;

        if (target == null)
        {
            return _cruiseHeight;
        }

        if (target.TryGetComponent(out Collider targetCollider) == false)
        {
            return _cruiseHeight;
        }

        return targetCollider.bounds.max.y + _workGap;
    }

    private float GetSpeed(float targetHeight)
    {
        if (targetHeight < transform.position.y)
        {
            return _descendSpeed;
        }

        return _ascendSpeed;
    }
}
