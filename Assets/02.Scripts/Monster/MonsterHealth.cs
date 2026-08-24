using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    private int _maxHp;
    private int _currentHp;
    private bool _isDead = false;

    public void Initialize(MonsterData data)
    {
        if(data != null)
        {
            _maxHp = data.Hp;
            _currentHp = _maxHp;
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
