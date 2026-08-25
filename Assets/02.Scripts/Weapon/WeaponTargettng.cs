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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_enemyTag))
        {
            if (!_targetMonster.Contains(other.transform))
            {
                _targetMonster.Add(other.transform);
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
}
