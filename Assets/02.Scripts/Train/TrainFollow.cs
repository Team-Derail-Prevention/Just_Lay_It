using UnityEngine;

public class TrainFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform _frontTrain;


    [Header("Follow Setting")]
    [SerializeField] private float _followDistance = 3.0f;
    [SerializeField] private float _reachThreshold = 0.2f;
    [SerializeField] private float _maxSpeedMultiplier = 2.5f;
    [SerializeField] private int _targetIndex = 0;


    private TrainData _trainData;
    private float _moveSpeed = 2f;
    private float _rotateSpeed = 5f;

    public TrainData Data
    {
        get { return _trainData; }
    }


    private void Update()
    {
        FollowFrontTrain();
    }

    public void FollowInit(TrainData data, Transform frontTrain)
    {
        _trainData = data;

        if (data != null)
        {
            _moveSpeed = data.MoveSpeed;
            _rotateSpeed = data.RotateSpeed;
        }
    
        SetFrontTrain(frontTrain);
    }

    private void FollowFrontTrain()
    {
        if (_frontTrain == null || TrainManager.Instance == null)
        {
            return;
        }

        if (TrainManager.Instance.IsStation)
        {
            return;
        }

        float distanceFront = Vector3.Distance(transform.position, _frontTrain.position);
        if (distanceFront <= _followDistance)
        {
            return;
        }

        Transform targetNode = TrainManager.Instance.GetWaypoint(_targetIndex);
        if (targetNode == null)
        {
            return;
        }

        //벌어진 거리만큼 가속 (기본 1.0배 ~ 최대 _maxSpeedMultiplier 배)
        float distanceExcess = distanceFront - _followDistance;
        float speedMultiplier = Mathf.Clamp(1f + (distanceExcess * 1.5f), 1f, _maxSpeedMultiplier);
        float currentSpeed = _moveSpeed * speedMultiplier;



        Vector3 direction = targetNode.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetNode.position, currentSpeed * Time.deltaTime);


        if (Vector3.Distance(transform.position, targetNode.position) <= _reachThreshold)
        {
            _targetIndex++;
        }
    }

    public void SetFrontTrain(Transform frontTrain)
    {
        _frontTrain = frontTrain;
    }

    public void SetTargetIndex(int index = 0)
    {
        _targetIndex = index;
    }
}
