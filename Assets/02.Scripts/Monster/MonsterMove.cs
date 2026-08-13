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

    private void Update()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 flatTargetPos = new Vector3(_target.position.x, transform.position.y, _target.position.z);

        float distanceToTarget = Vector3.Distance(transform.position, flatTargetPos);

        if(distanceToTarget > _attackRange)
        {
            _isAttackRange = false;
            _attackTimer = 0f;

            transform.position =Vector3.MoveTowards(transform.position, flatTargetPos , _moveSpeed * Time.deltaTime);
            transform.LookAt(flatTargetPos);
        }
        else
        {
            if(!_isAttackRange)
            {
                _isAttackRange = true;
            }

            transform.LookAt(flatTargetPos);

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= _attackCooldown)
            {              
                _attackTimer = 0f;
                ShootProjectile();
            }
        }
    }

    public void Initialize(string monsterId, Transform target)
    {
        _monsterId = monsterId;
        _target = target;

        _isAttackRange = false;
        _attackTimer = 0f;

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
}
