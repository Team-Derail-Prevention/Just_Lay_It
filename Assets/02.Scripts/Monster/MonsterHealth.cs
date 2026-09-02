using UnityEngine;
using System;
using System.ComponentModel;

public class MonsterHealth : MonoBehaviour
{
    public static event Action<int> OnMonsterDiedWithStone;
    public event Action<Transform>OnMonsterDied;

    private int _maxHp;
    private int _currentHp;
    private bool _isDead = false;

    public int _dropStone;

    private string _takeDamageSound;
    private string _dieSound;
    public void Initialize(MonsterData data,float hpMultiplier = 1.0f)
    {
        if(data != null)
        {
            _maxHp = Mathf.RoundToInt(data.Hp * hpMultiplier);
            _currentHp = _maxHp;
            _dropStone = data.DropGold;
            _takeDamageSound = data.UseTakeDamageSound;
            _dieSound = data.UseDieSound;
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

            return;
        }

        PlayMonsterSfx(_takeDamageSound, SfxAddress.Monster.Hit);
    }

    private void PlayMonsterSfx(string dataSoundName, string fallbackAddress)
    {
        string address = SfxAddress.Resolve(SfxAddress.Monster.Prefix, dataSoundName, fallbackAddress);

        SoundManager.Instance?.PlaySFXAt(address, transform.position);
    }

    private void Die()
    {
        _isDead = true;

        PlayMonsterSfx(_dieSound, SfxAddress.Monster.Die);

        OnMonsterDiedWithStone?.Invoke(_dropStone);
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
