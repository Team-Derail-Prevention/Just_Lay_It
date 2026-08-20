using UnityEngine;

public class Train : MonoBehaviour
{
    [Header("Sensor")]
    [SerializeField] private RailDetector _railDetector;

    [Header("Detection Setting")]
    [SerializeField] private float _reachThreshold = 0.2f;
    [SerializeField] public int _targetIndex = 0;
    [SerializeField] private bool _isMoving = true;

    private TrainData _trainData;
    private float _moveSpeed = 2f;
    private float _rotateSpeed = 5f;
    private int _maxHp;
    private int _currentHp;
    private int _defense;
    private float _totalDistance = 0f;
    private bool _isBroken = false;


    public bool IsMoving
    {
        get { return _isMoving; }
    }

    public TrainData Data
    {
        get {return _trainData; }
    }

    public int CurrentHp
    {
        get { return _currentHp; }
    }

    public int MaxHp
    {
        get { return _maxHp; }
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

    public void TrainInit(TrainData data)
    {
        _trainData = data;

        if (data != null)
        {
            _moveSpeed = data.MoveSpeed;
            _rotateSpeed = data.RotateSpeed;
            _maxHp = data.MaxHp;
            _currentHp = data.MaxHp;
            _defense = data.Defense;
        }

        _totalDistance = 0f;
        _isBroken = false;

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.NotifyHpChanged(_currentHp, _maxHp);
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isBroken)
        {
            return;
        }

        int totalDamage = Mathf.Max(1, damage - _defense);
        _currentHp = Mathf.Max(0,_currentHp - totalDamage);

        Debug.Log($"[Train] 기관차 피격! 받은 피해: {totalDamage} (적용 전: {damage}, 방어력: {_defense}), 남은 HP: {_currentHp}/{_maxHp}");

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            BrokenTrain();
        }

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.NotifyHpChanged(_currentHp, _maxHp);
        }
    }


    //기차역 도착 했을때 회복시킬 경우 사용 예정.
    public void Heal(int healAmount)
    {
        if (_isBroken || healAmount <= 0)
        {
            return;
        }

        _currentHp = Mathf.Min(_maxHp, _currentHp + healAmount);

        if (TrainStatusEventHub.Instance != null) 
        {
            TrainStatusEventHub.Instance.NotifyHpChanged( _currentHp, _maxHp);
        }
    }

    private void BrokenTrain()
    {
        _isBroken = true;
        _isMoving = false;
        Debug.Log("[Train] 기관차가 파괴되었습니다! 게임 오버 처리 필요");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    private void MoveTrain()
    {
        if (TrainManager.Instance == null || TrainManager.Instance.IsStation)
        {
            if (TrainStatusEventHub.Instance != null)
            {
                TrainStatusEventHub.Instance.NotifySpeedChanged(0f);
            }
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

        Vector3 prevPos = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetNode.position, _moveSpeed * Time.deltaTime);

        float movedDelta = Vector3.Distance(prevPos, transform.position);
        _totalDistance += movedDelta;

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.NotifySpeedChanged(_moveSpeed * 10f);
            TrainStatusEventHub.Instance.NotifyDistanceChanged(_totalDistance);
        }


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
