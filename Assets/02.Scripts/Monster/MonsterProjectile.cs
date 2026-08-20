using UnityEngine;

public class MonsterProjectile : MonoBehaviour
{
    private float _speed = 15f;
    private int _damage;
    private Vector3 _direction;

    private string _attackType;
    private float _debuffDuration;
    private float _debuffPower;

    private float _currentTime = 0f;
    private float _lifeTime = 2f;

    public void ProjectileInitialize(Vector3 dir, int atk, string type = "None", float duration = 0f, float power = 0f)
    {
        _direction = dir.normalized;
        _damage = atk;

        _attackType = type;
        _debuffDuration = duration;
        _debuffPower = power;

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
        if (other.CompareTag("Train") || other.GetComponent<Train>() != null)
        {
            Debug.Log("플레이어 타격");

            // TrainHealth trainHp = other.GetComponent<TrainHealth>();
            // if (trainHp != null)
            // {
            //   trainHp.TakeDamage(_damage);
            //   trainHp.ApplyDebuff(_attackType, _debuffDuration, _debuffPower);
            // }

            PoolManager.Instance.DespawnToPool(gameObject);
        }
    }
}
