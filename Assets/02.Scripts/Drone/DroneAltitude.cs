using UnityEngine;

public class DroneAltitude : MonoBehaviour
{
    [Header("높이")]
    [SerializeField, Min(0f)] private float _cruiseHeight = 3f;
    [SerializeField, Min(0f)] private float _workGap = 0.5f;

    [Header("속도")]
    [SerializeField, Min(0.1f)] private float _descendSpeed = 4f;
    [SerializeField, Min(0.1f)] private float _ascendSpeed = 3f;

    private IDroneWorker _worker;

    private void Awake()
    {
        _worker = GetComponent<IDroneWorker>();
    }

    public void AddCruiseOffset(float offset)
    {
        if (offset <= 0f)
        {
            return;
        }

        _cruiseHeight += offset;
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

        if (_worker != null && _worker.State == DroneState.Working)
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
        if (_worker == null)
        {
            return _cruiseHeight;
        }

        if (_worker.TryGetWorkTopY(out float topY) == false)
        {
            return _cruiseHeight;
        }

        return topY + _workGap;
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
