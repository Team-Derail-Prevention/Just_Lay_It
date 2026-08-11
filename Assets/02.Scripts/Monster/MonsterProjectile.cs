using UnityEngine;

public class MonsterProjectile : MonoBehaviour
{
    private float speed = 15f;
    private int damage;
    private Vector3 direction;

    public void ProjectileInitialize(Vector3 dir, int atk)
    {
        direction = dir.normalized;
        damage = atk;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        Destroy(gameObject,3f);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<TestTarget>() != null)
        {
            Debug.Log("Hit Player");
            Destroy(gameObject);
        }
    }
}
