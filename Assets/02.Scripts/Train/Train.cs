using UnityEngine;

public class Train : MonoBehaviour
{
    [Header("Detection Setting")]
    [SerializeField] private float _reachThreshold = 0.2f;
    [SerializeField] public int _targetIndex = 0;
    [SerializeField] private bool _isMoving = true;

    [Header("Acceleration Setting")]
    [SerializeField] private float _currentSpeed = 0f; // 현재 속도
    [Header("Debuff State")]
    private bool _isFrozen = false;
    private bool _isElectrified = false;
    private float _corrosionMultiplier = 1.0f;

    public bool IsFrozen => _isFrozen;
    public bool IsElectrified => _isElectrified;

    private TrainData _trainData;
    private float _maxMoveSpeed = 2f;
    private float _acceleration = 0.1f; // 가속도 (점점 빨라지는 폭)
    private float _rotateSpeed = 5f;
    private int _maxHp;
    private int _currentHp;
    private int _defense;
    private float _totalDistance = 0f;
    private bool _isBroken = false;

    private Coroutine _freezeCoroutine;
    private Coroutine _electricCoroutine;
    private Coroutine _corrosionCoroutine;

    public float TotalDistance { get { return _totalDistance; } }


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

    public float CurrentSpeed
    {
        get { return _currentSpeed; }
    }

    public float Defense
    {
        get { return _defense; }
    }


    // TODO : 인게임 기차 업그레이드 요소 추가하면 구독or취소 할거 (체력강화나 이것저것)
    //private void OnEnable()
    //{
    //    UpgradeEventHub.Instance.OnInGameUpgraded += ;
    //}



    //private void OnDisable()
    //{
    //    UpgradeEventHub.Instance.OnInGameUpgraded -= ;

    //}


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _isMoving = !_isMoving;
        }

        if (GameManager.Instance.CurrentGameState == GameState.Playing && _isMoving && !_isBroken)
        {
            MoveTrain();
        }
        else
        {
            _currentSpeed = 0f;
            TrainStatusEventHub.Instance?.NotifySpeedChanged(0f);
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
            _maxMoveSpeed = data.MoveSpeed;
            _rotateSpeed = data.RotateSpeed;
            _maxHp = data.MaxHp;
            _currentHp = data.MaxHp;
            _defense = data.Defense;
        }

        _currentSpeed = 0f;

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.NotifyHpChanged(_currentHp, _maxHp);
        }
    }

    public void UpgradeMaxHp(int addHp, bool healAmount)
    {
        if (addHp <= 0)
        {
            return;
        }

        _maxHp += addHp;
        if (healAmount)
        {
            _currentHp += addHp;
        }

        Debug.Log($"[Train Upgrade] 최대 HP 강화! MaxHP: {_maxHp}, CurrentHP: {_currentHp}");
        TrainStatusEventHub.Instance?.NotifyHpChanged(_currentHp, _maxHp);
    }

    public void UpgradeDefense(int addDefense)
    {
        if (addDefense <= 0)
        {
            return;
        }

        _defense += addDefense;
        Debug.Log($"[Train Upgrade] 방어력 강화! 현재 방어력: {_defense}");
    }

    public void TakeDamage(int damage)
    {
        if (_isBroken)
        {
            return;
        }

        int calculateDmg = Mathf.Max(1, damage - _defense);
        int totalDamage = Mathf.Max(1, Mathf.RoundToInt(calculateDmg * _corrosionMultiplier));

        _currentHp = Mathf.Max(0, _currentHp - totalDamage);

        Debug.Log($"[Train] 기관차 피격! 받은 피해: {totalDamage} (기본 피해: {damage}, 방어력: {_defense}, 받는 데미지 배율: {_corrosionMultiplier}배), 남은 HP: {_currentHp}/{_maxHp}");

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
    public void Heal(int healPercent)
    {
        if (_isBroken || healPercent <= 0)
        {
            return;
        }

        int healAmount = Mathf.RoundToInt(_maxHp * (healPercent / 100f));

        _currentHp = Mathf.Min(_maxHp, _currentHp + healAmount);

        Debug.Log($"[Train] 열차 수리 완료! 회복량: {healAmount} ({healPercent}%), 현재 HP: {_currentHp}/{_maxHp}");
        

        if (GameManager.TrainStatusEventHub != null)
        {
            GameManager.TrainStatusEventHub.NotifyHpChanged(_currentHp, _maxHp);
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
            _currentSpeed = 0f;
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
                _currentSpeed = 0f;
                TrainStatusEventHub.Instance.NotifySpeedChanged(0);
            }
            return;
        }

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _maxMoveSpeed, _acceleration * Time.deltaTime);

        Vector3 direction = (targetNode.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed);
        }

        Vector3 prevPos = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetNode.position, _currentSpeed * Time.deltaTime);

        float movedDelta = Vector3.Distance(prevPos, transform.position);
        _totalDistance += movedDelta;

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.NotifySpeedChanged(_currentSpeed * 10f);
            TrainStatusEventHub.Instance.NotifyDistanceChanged(_totalDistance);
        }


        if (Vector3.Distance(transform.position, targetNode.position) <= _reachThreshold)
        {
            _targetIndex++;
        }

    }

    public void SetTargetIndex(int index)
    {
        _targetIndex = index;
    }

    public void ApplyDebuff(string debuffType, float duration, float power)
    {
        if (_isBroken)
        {
            return;
        }
        switch (debuffType)
        {
            case "Freeze":
                if (_freezeCoroutine != null) StopCoroutine(_freezeCoroutine);
                _freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
                break;
            case "Electric":
                if (_electricCoroutine != null) StopCoroutine(_electricCoroutine);
                _electricCoroutine = StartCoroutine(ElectricRoutine(duration));
                break;
            case "Corrosion":
                if (_corrosionCoroutine != null) StopCoroutine(_corrosionCoroutine);
                _corrosionCoroutine = StartCoroutine(CorrosionRoutine(duration, power));
                break;
            case "Steal":
                StealCargo(power);
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

    private System.Collections.IEnumerator CorrosionRoutine(float duration, float damageMultiplier)
    {
        _corrosionMultiplier = damageMultiplier;

        yield return new WaitForSeconds(duration);

        _corrosionMultiplier = 1.0f;
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


   // 업그레이드 테스트용 
    [ContextMenu("Test / Upgrade MaxHp (+50)")]
    private void TestUpgradeHp() => UpgradeMaxHp(50, true);

    [ContextMenu("Test / Upgrade Defense (+5)")]
    private void TestUpgradeDef() => UpgradeDefense(5);

}
