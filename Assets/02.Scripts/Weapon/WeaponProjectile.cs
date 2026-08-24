using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    private float _speed = 15f;
    private int _damage;
    private Vector3 _direction;

    private float _currentTime = 0f;
    private float _lifeTime = 2f;

    public void ProjectileInitialize(Vector3 dir, int atk)
    {
        _direction = dir.normalized;
        _damage = atk;

        _currentTime = 0f;

        if (_direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_direction) * Quaternion.Euler(0f, 90f, 0f);
        }
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        _currentTime += Time.deltaTime;
        if (_currentTime >= _lifeTime)
        {
            PoolManager.Instance.DespawnToPool(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            Debug.Log("Hit Monster");

            // MonsterHealth monsterHp = other.GetComponent<MonsterHealth>();
            // if (monsterHp != null)
            // {
            //     monsterHp.TakeDamage(_damage);
            // }

            PoolManager.Instance.DespawnToPool(gameObject);
        }
    }
}