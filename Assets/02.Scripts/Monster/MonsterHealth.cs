using UnityEngine;
using System;
using System.ComponentModel;

public class MonsterHealth : MonoBehaviour
{
    public static event Action<int> OnMonsterDiedWithGold;
    public event Action<Transform>OnMonsterDied;

    private int _maxHp;
    private int _currentHp;
    private bool _isDead = false;

    public int _dropGold;
    public void Initialize(MonsterData data)
    {
        if(data != null)
        {
            _maxHp = data.Hp;
            _currentHp = _maxHp;
            _dropGold = data.DropGold;
        }

        _isDead = false;
    }

    public void TakeDamage(int damage)
    {
        if (_isDead)
        {
            return;
        }
        _currentHp -= damage;

        Debug.Log($"Monster took {damage} damage. Current HP: {_currentHp}/{_maxHp}");

        if (_currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;

        OnMonsterDiedWithGold?.Invoke(_dropGold);
        OnMonsterDied?.Invoke(transform);

        if (MonsterSpawn.Instance != null)
        { 
            MonsterSpawn.Instance.DecreaseMonsterCount();
        }

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.DespawnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
