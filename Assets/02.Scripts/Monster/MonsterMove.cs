using UnityEngine;

public class MonsterMove : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator _animator;

    [SerializeField] private float _attackRange = 5.0f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform firePosition;

    private float _moveSpeed;
    private int _monsterAtk;
    private Transform _target;
    private bool _isAttackRange = false;

    private readonly int _walk = Animator.StringToHash("IsMove");
    private readonly int _attack = Animator.StringToHash("IsAttack");

    private float _attackTimer = 0f;
    private float _attackCooldown = 2f;

    private string _attackType;
    private float _debuffDuration;
    private float _debuffPower;

    private string _projectileColor;

    private Rigidbody _rb;

    private void FixedUpdate()
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

    public void Initialize(MonsterData data, Transform target)
    {
        _target = target;

        _isAttackRange = false;
        _attackTimer = 0f;

        _rb = GetComponent<Rigidbody>();

        if (firePosition == null)
        {
            firePosition = transform;
        }

        if (data != null)
        {
            _moveSpeed = data.Speed;
            _monsterAtk = data.Atk;

            _attackType = data.AttackType;
            _debuffDuration = data.DebuffDuration;
            _debuffPower = data.DebuffPower;
            _projectileColor = data.ProjectileColor;
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

            projectile.ProjectileInitialize(shootDir, _monsterAtk, _attackType, _debuffDuration, _debuffPower, _projectileColor);
        }

    }

    private void HandleMovement(Vector3 targetPos)
    {
        _isAttackRange = false;
        _attackTimer = 0f;

        if (_animator != null)
        {
            _animator.SetBool(_walk, true);
        }

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

            if (_animator != null)
            {
                _animator.SetBool(_walk, false);
            }

        }

        transform.LookAt(targetPos);

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackCooldown)
        {
            _attackTimer = 0f;

            if (_animator != null)
            {
                _animator.SetTrigger(_attack);
            }

            ShootProjectile();
        }
    }
}
