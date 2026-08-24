using UnityEngine;

public class WeaponFire : MonoBehaviour
{
    [SerializeField] private string _weaponId;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePosition;
    [SerializeField] private WeaponTargeting _weaponTargeting;

    private int _weaponAtk;
    private float _fireRate;
    private int _magazineSize;
    private float _reloadTime;

    private int _currentAmmo;
    private float _fireTimer = 0f;
    private float _reloadTimer = 0f;
    private bool _isReloading = false;

    private void Awake()
    {
        if (_firePosition == null)
        {
            _firePosition = transform;
        }

        if (_weaponTargeting == null)
        {
            _weaponTargeting = GetComponent<WeaponTargeting>();
        }
    }

    private void Start()
    {
        LoadWeaponData();
    }

    private void Update()
    {
        if (_isReloading)
        {
            HandleReload();
            return;
        }

        if (!_weaponTargeting.HasTarget)
        {
            return;
        }

        _fireTimer += Time.deltaTime;
        if (_fireTimer < _fireRate)
        {
            return;
        }

        if (_currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        _fireTimer = 0f;
        ShootProjectile();
    }

    private void LoadWeaponData()
    {
        WeaponData weaponData = DataManager.Instance.GetData<WeaponData>(_weaponId);

        if (weaponData != null)
        {
            _weaponAtk = weaponData.Atk;
            _fireRate = weaponData.FireRate;
            _magazineSize = weaponData.MagazineSize;
            _reloadTime = weaponData.ReloadTime;
            _currentAmmo = _magazineSize;
        }
        else
        {
            Debug.LogError($"{_weaponId} 무기 데이터를 찾을 수 없습니다");
        }
    }

    private void ShootProjectile()
    {
        if (_projectilePrefab == null)
        {
            return;
        }

        Transform target = _weaponTargeting.CurrentTarget;
        if (target == null)
        {
            return;
        }

        string projectilePoolId = _projectilePrefab.name;
        GameObject projObj = PoolManager.Instance.SpawnFromPool(projectilePoolId, _firePosition.position, Quaternion.identity);

        WeaponProjectile projectile = projObj.GetComponent<WeaponProjectile>();
        if (projectile != null)
        {
            Vector3 targetCenter = new Vector3(target.position.x, _firePosition.position.y, target.position.z);
            Vector3 shootDir = (targetCenter - _firePosition.position).normalized;

            projectile.ProjectileInitialize(shootDir, _weaponAtk);
        }

        _currentAmmo--;
    }

    private void StartReload()
    {
        _isReloading = true;
        _reloadTimer = 0f;
    }

    private void HandleReload()
    {
        _reloadTimer += Time.deltaTime;
        if (_reloadTimer >= _reloadTime)
        {
            _currentAmmo = _magazineSize;
            _isReloading = false;
        }
    }
}