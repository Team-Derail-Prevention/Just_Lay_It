using UnityEngine;

public class MonsterProjectile : MonoBehaviour
{
    private float speed = 15f;
    private int damage;
    private Vector3 direction;

    private float currentTime = 0f;
    private float lifeTime = 2f;

    public void ProjectileInitialize(Vector3 dir, int atk)
    {
        direction = dir.normalized;
        damage = atk;

        currentTime = 0f;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        currentTime += Time.deltaTime;
        if (currentTime >= lifeTime)
        {
            PoolManager.Instance.DespawnToPool(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<TestTarget>() != null)
        {
            Debug.Log("Hit Player");
            PoolManager.Instance.DespawnToPool(gameObject);
        }
    }
}
