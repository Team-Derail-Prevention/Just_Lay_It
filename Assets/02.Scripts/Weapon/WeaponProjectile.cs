using UnityEngine;

public class WeaponProjectile : MonoBehaviour
{
    private float _speed = 15f;
    private int _damage;
    private Vector3 _direction;

    [Header("Splash Damage")]
    [SerializeField] private float _explosionRadius = 0f;

    private float _currentTime = 0f;
    private float _lifeTime = 2f;

    public void ProjectileInitialize(Vector3 dir, int atk)
    {
        _direction = dir.normalized;
        _damage = atk;

        _currentTime = 0f;
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

            if (_explosionRadius > 0f)
            {
                SplashDamage();
            }
            else
            {
                MonsterHealth monsterHp = other.GetComponent<MonsterHealth>();
                if (monsterHp != null)
                {
                    monsterHp.TakeDamage(_damage);
                }
            }

            PoolManager.Instance.DespawnToPool(gameObject);
        }
    }

    // 명중한 지점(transform.position) 기준으로 반경 안의 몬스터 전부에게 데미지를 줌.
    // 태그로 필터링해서 레이어 세팅을 따로 안 잡아도 기존 방식(태그 기반)과 일관되게 동작함.
    private void SplashDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Monster"))
            {
                continue;
            }

            MonsterHealth monsterHp = hits[i].GetComponent<MonsterHealth>();
            if (monsterHp != null)
            {
                monsterHp.TakeDamage(_damage);
            }
        }
    }
}