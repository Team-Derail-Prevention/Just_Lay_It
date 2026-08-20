using UnityEngine;

public class TrainFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform _frontTrain;


    [Header("Follow Setting")]
    [SerializeField] private float _followDistance = 3.0f;
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

        Vector3 direction = targetNode.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetNode.position, _moveSpeed * Time.deltaTime);


        if (Vector3.Distance(transform.position, targetNode.position) < 0.1f)
        {
            _targetIndex++;
        }
    }

    public void SetFrontTrain(Transform frontTrain)
    {
        _frontTrain = frontTrain;
    }
}
