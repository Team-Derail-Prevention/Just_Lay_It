using UnityEngine;
using System.Collections;

public class Train : MonoBehaviour
{
    [Header("Detection Setting")]
    [SerializeField] private float _reachThreshold = 0.2f;
    [SerializeField] public int _targetIndex = 0;
    [SerializeField] private bool _isMoving = true;

    [Header("Debuff State")]
    private bool _isFrozen = false;
    private bool _isElectrified = false;
    private float _corrodeMultiplier = 1.0f;

    public bool IsFrozen => _isFrozen;
    public bool IsElectrified => _isElectrified;

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
        get { return _trainData; }
    }

    public int CurrentHp
    {
        get { return _currentHp; }
    }

    public int MaxHp
    {
        get { return _maxHp; }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _isMoving = !_isMoving;
        }

        if (_isMoving && !_isBroken)
        {
            MoveTrain();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("[Cheat] I키 입력: 20 데미지 적용");
            TakeDamage(20);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("[Cheat] O키 입력: 20 회복 적용");
            Heal(20);
        }
    }

    public void TrainInit(TrainData data)
    {
        _trainData = data;
        _totalDistance = 0f;
        _isBroken = false;

        if (data != null)
        {
            _moveSpeed = data.MoveSpeed;
            _rotateSpeed = data.RotateSpeed;
            _maxHp = data.MaxHp;
            _currentHp = data.MaxHp;
            _defense = data.Defense;
        }

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

        int totalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * _corrodeMultiplier));

        _currentHp = Mathf.Max(0, _currentHp - totalDamage);

        Debug.Log($"[Train] 기관차 피격! 받은 피해: {totalDamage} (기본 피해: {damage}, 받는 데미지 배율: {_corrodeMultiplier}배), 남은 HP: {_currentHp}/{_maxHp}");

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
            TrainStatusEventHub.Instance.NotifyHpChanged(_currentHp, _maxHp);
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
            if (TrainStatusEventHub.Instance != null)
            {
                TrainStatusEventHub.Instance.NotifySpeedChanged(0f);
            }
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


    public void SetTargetIndex(int index)
    {
        _targetIndex = index;
    }

    public void ApplyDebuff(string debuffType, float duration, float power)
    {
        if (_isBroken) return;

        Debug.Log($" 디버프 적용 타입: {debuffType}, 지속시간: {duration}초, 위력: {power}");

        switch (debuffType)
        {
            case "Freeze":
                StartCoroutine(FreezeRoutine(duration));
                break;
            case "Electric":
                StartCoroutine(ElectricRoutine(duration));
                break;
            case "Corrode":
                StartCoroutine(CorrodeRoutine(duration, power));
                break;
            case "Steal":
                StealCargo(power);
                break;
            case "None":
            default:
                break;
        }
    }

    private System.Collections.IEnumerator FreezeRoutine(float duration)
    {
        _isFrozen = true;
        yield return new WaitForSeconds(duration);
        _isFrozen = false;
    }

    private System.Collections.IEnumerator ElectricRoutine(float duration)
    {
        _isElectrified = true;
        yield return new WaitForSeconds(duration);
        _isElectrified = false;
    }

    private System.Collections.IEnumerator CorrodeRoutine(float duration, float damageMultiplier)
    {
        _corrodeMultiplier = damageMultiplier;

        yield return new WaitForSeconds(duration);

        _corrodeMultiplier = 1.0f;
    }

    private void StealCargo(float stealAmount)
    {
        TrainContainer container = GetComponent<TrainContainer>();
        if (container != null)
        {
            container.UseCargo(stealAmount);
            Debug.Log($"몬스터가 자재를 {stealAmount}만큼 훔침 남은 자재: {container.CurrentAmount}");
        }
    }

}
