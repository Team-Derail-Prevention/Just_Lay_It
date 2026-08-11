using UnityEngine;

public class Train : MonoBehaviour
{
    [Header("Move Setting")]
    [SerializeField] public float _moveSpeed = 5f;
    [SerializeField] public float _rotateSpeed = 10f;
    [SerializeField] public int _targetIndex = 0;

    [Header("Detection Setting")]
    [SerializeField] private float _detectForwardOffset = 0.8f;

    private void Update()
    {
        MoveTrain();
    }


    private void MoveTrain()
    {
        if (TrainManager.Instance == null)
        {
            return;
        }

        if (TrainManager.Instance.IsStation)
        {
            return;
        }

        Vector3 frontPivotPos = transform.position + (transform.forward * _detectForwardOffset);

        TrainManager.Instance.DetectRail(frontPivotPos);
        TrainManager.Instance.DetectStation(frontPivotPos);


        Transform targetNode = TrainManager.Instance.GetWaypoint(_targetIndex);

        if (targetNode != null)
        {
            Vector3 direction = (targetNode.position - transform.position).normalized;
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
    }

    //public void SetTargetIndex(int index)
    //{
    //    _targetIndex = index;
    //}
}
