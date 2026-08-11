using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MonsterMove : MonoBehaviour
{
    [SerializeField] private string _monsterId = "Monster_01";
    [SerializeField] private float _attackRange = 5.0f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform firePosition;

    private float _moveSpeed;
    private int _monsterAtk;
    private Transform _target;
    private bool _isAttackRange = false;

    private float _attackTimer = 0f;
    private float _attackCooldown = 2f;

    private void Start()
    {
        MonsterData monsterData = DataManager.Instance.GetData<MonsterData>(_monsterId);

        if (monsterData != null)
        {
            _moveSpeed = monsterData.Speed;
            _monsterAtk = monsterData.Atk;
            Debug.Log($"Monster '{monsterData.MonsterName}' move speed: {_moveSpeed}");
        }
        else
        {
            Debug.LogError($"Monster data not found for ID: {_monsterId}");
        }

        if(firePosition == null)
        {
            firePosition = transform;
        }

    }

    private void Update()
    {
        if (_target == null)
        {
            return;
        }
        float distanceToTarget = Vector3.Distance(transform.position, _target.position);

        if(distanceToTarget > _attackRange)
        {
            _isAttackRange = false;
            _attackTimer = 0f;

            transform.position =Vector3.MoveTowards(transform.position, _target.position, _moveSpeed * Time.deltaTime);
            transform.LookAt(_target);
        }
        else
        {
            if(!_isAttackRange)
            {
                _isAttackRange = true;
            }

            transform.LookAt(_target);

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= _attackCooldown)
            {              
                _attackTimer = 0f;
                ShootProjectile();
            }
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void ShootProjectile()
    {
        if (_projectilePrefab == null)
        {
            return;
        }

        GameObject projObj = Instantiate(_projectilePrefab, firePosition.position, Quaternion.identity);

        MonsterProjectile projectile = projObj.GetComponent<MonsterProjectile>();
        if (projectile != null)
        {
            Vector3 targetCenter = _target.position + Vector3.up * 1f;
            Vector3 shootDir = (targetCenter - firePosition.position).normalized;

            projectile.ProjectileInitialize(shootDir, _monsterAtk);
        }
    }
}
