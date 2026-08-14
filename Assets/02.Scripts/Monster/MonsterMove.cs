using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MonsterMove : MonoBehaviour
{    
    [SerializeField] private float _attackRange = 5.0f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform firePosition;

    private string _monsterId;
    private float _moveSpeed;
    private int _monsterAtk;
    private Transform _target;
    private bool _isAttackRange = false;

    private float _attackTimer = 0f;
    private float _attackCooldown = 2f;

    private Rigidbody _rb;

    private void Update()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 flatTargetPos = new Vector3(_target.position.x, transform.position.y, _target.position.z);
        float distanceToTarget = Vector3.Distance(transform.position, flatTargetPos);

        if (distanceToTarget > _attackRange)
        {
            HandleMovement(flatTargetPos);
        }
        else
        {
            HandleAttack(flatTargetPos);
        }
    }

    public void Initialize(string monsterId, Transform target)
    {
        _monsterId = monsterId;
        _target = target;

        _isAttackRange = false;
        _attackTimer = 0f;

        _rb = GetComponent<Rigidbody>();

        if (firePosition == null)
        {
            firePosition = transform;
        }

        LoadMonsterData();
    }

    private void LoadMonsterData()
    {
        MonsterData monsterData = DataManager.Instance.GetData<MonsterData>(_monsterId);

        if (monsterData != null)
        {
            _moveSpeed = monsterData.Speed;
            _monsterAtk = monsterData.Atk;
        }
        else
        {
            Debug.LogError($"{_monsterId} 몬스터 데이터를 찾을 수 없습니다");
        }
    }
    private void ShootProjectile()
    {
        if (_projectilePrefab == null)
        {
            return;
        }

        string projectilePoolId = _projectilePrefab.name;
        GameObject projObj = PoolManager.Instance.SpawnFromPool(projectilePoolId, firePosition.position, Quaternion.identity);

        MonsterProjectile projectile = projObj.GetComponent<MonsterProjectile>();
        if (projectile != null)
        {
            Vector3 targetCenter = new Vector3(_target.position.x, firePosition.position.y, _target.position.z);
            Vector3 shootDir = (targetCenter - firePosition.position).normalized;

            projectile.ProjectileInitialize(shootDir, _monsterAtk);
        }
    }

    private void HandleMovement(Vector3 targetPos)
    {
        _isAttackRange = false;
        _attackTimer = 0f;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
        if (_rb != null)
        {
            _rb.MovePosition(newPos);
        }
        else
        {
            transform.position = newPos;
        }
        transform.LookAt(targetPos);
    }
    private void HandleAttack(Vector3 targetPos)
    {
        if (!_isAttackRange)
        {
            _isAttackRange = true;
        }

        transform.LookAt(targetPos);

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackCooldown)
        {
            _attackTimer = 0f;
            ShootProjectile();
        }
    }
}
