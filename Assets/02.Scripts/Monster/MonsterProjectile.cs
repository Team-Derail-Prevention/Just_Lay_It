using UnityEngine;
using UnityEngine.UIElements;

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

    [SerializeField] private Renderer _renderer;

    public void ProjectileInitialize(Vector3 dir, int atk, string type = "None", float duration = 0f, float power = 0f, string colorCode = "#FF0000")
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

        SetColorByHex(colorCode);
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
            Debug.Log("Train 타격");

            Train train = other.GetComponent<Train>();

            if (train != null)
            {
 
                train.TakeDamage(_damage);
                train.ApplyDebuff(_attackType, _debuffDuration, _debuffPower);
            }

            PoolManager.Instance.DespawnToPool(gameObject);
        }
    }

    private void SetColorByHex(string hexCode)
    {
        if (_renderer == null || string.IsNullOrEmpty(hexCode))
        { 
            return; 
        }

        if (ColorUtility.TryParseHtmlString(hexCode, out Color parsedColor))
        {
            _renderer.material.color = parsedColor;
        }
        else
        {
            _renderer.material.color = Color.white;
            Debug.LogWarning($"Projectile 색상 코드 오류: {hexCode}");
        }
    }
}
