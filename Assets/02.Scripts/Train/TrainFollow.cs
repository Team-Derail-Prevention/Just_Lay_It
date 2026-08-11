using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class TrainFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform _frontTrain;


    [Header("Follow Setting")]
    [SerializeField] private float _followDistance = 1.2f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] public float _rotateSpeed = 10f;
    [SerializeField] private int _targetIndex = 0;


    private void Update()
    {
        FollowFrontTrain();
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
