using System.Collections.Generic;
using UnityEngine;
public class WeaponTargeting : MonoBehaviour
{
    [SerializeField] private string _enemyTag = "Monster";
    private Transform _LockTarget;
    private List<Transform> _targetMonster = new List<Transform>();
    public bool HasTarget { get { return _targetMonster.Count > 0; } }
    public Transform CurrentTarget
    {
        get
        {
            if (IsLockTarget())
            {
                return _LockTarget;
            }
            _LockTarget = GetNearTarget();
            return _LockTarget;
        }
    }

    private void Update()
    {
        if (!HasTarget)
        {
            return;
        }
        transform.LookAt(CurrentTarget);
    }

    public void ApplyRange(float range)
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[WeaponTargeting] 사거리를 적용할 Collider가 없습니다.");
            return;
        }

        if (col is SphereCollider sphereCollider)
        {
            sphereCollider.radius = range;
        }
        else if (col is BoxCollider boxCollider)
        {
            boxCollider.size = Vector3.one * range * 2f;
        }
        else
        {
            Debug.LogWarning($"[WeaponTargeting] 지원하지 않는 Collider 타입입니다: {col.GetType().Name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_enemyTag))
        {
            if (!_targetMonster.Contains(other.transform))
            {
                _targetMonster.Add(other.transform);
                MonsterHealth monsterHp = other.GetComponent<MonsterHealth>();
                if (monsterHp != null)
                {
                    monsterHp.OnMonsterDied += MonsterDaath;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_enemyTag))
        {
            if (_targetMonster.Contains(other.transform))
            {
                _targetMonster.Remove(other.transform);

                // 사거리를 벗어난 것뿐(죽은 게 아님)이라도 구독은 반드시 해제.
                // 안 그러면 나중에 다시 들어올 때 또 구독돼서 중복 구독이 쌓임.
                MonsterHealth monsterHp = other.GetComponent<MonsterHealth>();
                if (monsterHp != null)
                {
                    monsterHp.OnMonsterDied -= MonsterDaath;
                }
            }
        }
    }

    // 오브젝트에 대한 참조 가져오는 로직
    private Transform GetNearTarget()
    {
        for (int i = _targetMonster.Count - 1; i >= 0; i--)
        {
            if (_targetMonster[i] == null)
            {
                _targetMonster.RemoveAt(i);
            }
        }
        Transform nearest = null;
        float nearestSqrDist = float.MaxValue;
        for (int i = 0; i < _targetMonster.Count; i++)
        {
            float sqrDist = (_targetMonster[i].position - transform.position).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = _targetMonster[i];
            }
        }
        return nearest;
    }
    private bool IsLockTarget()
    {
        if (_LockTarget == null)
        {
            return false;
        }
        return _targetMonster.Contains(_LockTarget);
    }
    private void MonsterDaath(Transform deadMonster)
    {
        if (_targetMonster.Contains(deadMonster))
        {
            _targetMonster.Remove(deadMonster);
        }

        MonsterHealth monsterHp = deadMonster.GetComponent<MonsterHealth>();
        if (monsterHp != null)
        {
            monsterHp.OnMonsterDied -= MonsterDaath;
        }

        if (_LockTarget == deadMonster)
        {
            _LockTarget = null;
        }
    }

    private void OnEnable()
    {
        PoolManager.OnAllDespawnedToPool += ClearAllTargets;
    }

    private void OnDisable()
    {
        PoolManager.OnAllDespawnedToPool -= ClearAllTargets;

        ClearAllTargets();
    }

    public void ClearAllTargets()
    {
        for (int i = 0; i < _targetMonster.Count; i++)
        {
            if (_targetMonster[i] == null)
            {
                continue;
            }

            MonsterHealth monsterHp = _targetMonster[i].GetComponent<MonsterHealth>();
            if (monsterHp != null)
            {
                monsterHp.OnMonsterDied -= MonsterDaath;
            }
        }

        _targetMonster.Clear();
        _LockTarget = null;
    }
}