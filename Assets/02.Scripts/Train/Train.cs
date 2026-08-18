using UnityEngine;

public class Train : MonoBehaviour
{
    [Header("Move Setting")]
    [SerializeField] public float _moveSpeed = 5f;
    [SerializeField] public float _rotateSpeed = 10f;
    [SerializeField] private float _reachThreshold = 0.15f;
    [SerializeField] public int _targetIndex = 0;

    [Header("Detection Setting")]
    [SerializeField] private RailDetector _railDetector;
    [SerializeField] private bool _isMoving = true;

    public bool IsMoving
    {
        get { return _isMoving; }
    }

    private void Awake()
    {
        if (_railDetector == null)
        {
            _railDetector = GetComponentInChildren<RailDetector>();
        }
    }

    private void OnEnable()
    {
        if (_railDetector != null)
        {
            _railDetector.OnRailDetected += HandleRailDetected;
            _railDetector.OnStationDetected += HandleStationDetected;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _isMoving = !_isMoving;
        }

        if (_isMoving)
        {
            MoveTrain();
        }
    }

    private void OnDisable()
    {
        if (_railDetector != null)
        {
            _railDetector.OnRailDetected -= HandleRailDetected;
            _railDetector.OnStationDetected -= HandleStationDetected;
        }
        
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

        Transform targetNode = TrainManager.Instance.GetWaypoint(_targetIndex);
        if (targetNode == null)
        {
            return;
        }

        Vector3 direction = (targetNode.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetNode.position, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetNode.position) <= _reachThreshold)
        {
            _targetIndex++;
        }
       
    }

    public void StartMove()
    {
        _isMoving = true;
    }

    public void StopMove()
    {
        _isMoving = false;
    }

    private void HandleRailDetected(Transform railTransform)
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.DetectRail(railTransform);
        }
    }

    private void HandleStationDetected(GameObject stationObj)
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.ArriveStation(stationObj);
        }
    }

    //public void SetTargetIndex(int index)
    //{
    //    _targetIndex = index;
    //}
}
