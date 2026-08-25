using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class WeaponFire : MonoBehaviour
{
    [SerializeField] private string _weaponId;
    [SerializeField] private string _projectileAddressableKey = "WeaponProjectile";
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

    private bool _isInitialized = false;

    private GameObject _loadedProjectilePrefab;

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

    private async void Start()
    {
        await InitializeAsync();
    }

    private async UniTask InitializeAsync()
    {
        LoadWeaponData();

        _loadedProjectilePrefab = await ResourceManager.Instance.LoadAsset<GameObject>(_projectileAddressableKey);

        if (_loadedProjectilePrefab == null)
        {
            Debug.LogError($"{_projectileAddressableKey} 프리팹을 찾을 수 없습니다! Addressable 체크를 확인하세요.");
            return;
        }

        Dictionary<string, int> initialPool = new Dictionary<string, int>
        {
            { _projectileAddressableKey, 20 }
        };

        PoolManager.Instance.Init(this.transform, initialPool, GetProjectilePrefab);

        _isInitialized = true;
        Debug.Log("[WeaponFire] 투사체 프리팹 어드레서블 로드 및 풀 초기화 완료!");
    }

    private GameObject GetProjectilePrefab(string id)
    {
        if (id == _projectileAddressableKey)
        {
            return _loadedProjectilePrefab;
        }
        return null;
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

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
        Transform target = _weaponTargeting.CurrentTarget;
        if (target == null)
        {
            return;
        }

        GameObject projObj = PoolManager.Instance.SpawnFromPool(_projectileAddressableKey, _firePosition.position, Quaternion.identity);

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