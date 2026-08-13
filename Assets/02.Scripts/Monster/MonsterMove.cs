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

    private async void Start()
    {
        //테스트 시점꼬임 방지용 비동기 로딩 대기 코드
        while (DataManager.Instance == null)
        {
            await Cysharp.Threading.Tasks.UniTask.Yield();
        }

        while (!DataManager.Instance.IsLoaded)
        {
            await Cysharp.Threading.Tasks.UniTask.Yield();
        }

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

        //if (_target == null && TestTarget.Instance != null)
        //{
        //    _target = TestTarget.Instance.transform;
        //}
    }

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
