using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WeaponFire : MonoBehaviour
{
    [SerializeField] private string _weaponId;
    [SerializeField] private string _projectileId; // Addressable 주소이자 풀 Id로 그대로 사용
    [SerializeField] private Transform _firePosition;
    [SerializeField] private WeaponTargeting _weaponTargeting;

    private GameObject _projectilePrefab; // Addressable 로드 결과 캐시 (null 체크용)

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

    private async void Start()
    {
        LoadWeaponData();
        await RegisterProjectilePoolAsync();
    }

    // MonsterSpawn.InitializePoolAsync와 동일한 패턴: Addressable로 프리팹을 미리 로드해서
    // PoolManager.Init()의 prefabMap(Dictionary)으로 넘김. 키 단위로 병합되니 몬스터 등록을 안 지움.
    private async UniTask RegisterProjectilePoolAsync()
    {
        if (string.IsNullOrEmpty(_projectileId))
        {
            Debug.LogWarning("[WeaponFire] _projectileId가 비어있어 발사체 풀을 준비하지 못했습니다.");
            return;
        }

        _projectilePrefab = await ResourceManager.Instance.LoadAsset<GameObject>(_projectileId);

        if (_projectilePrefab == null)
        {
            Debug.LogError($"[WeaponFire] {_projectileId} 발사체 프리팹을 로드하지 못했습니다. Addressable 등록을 확인하세요.");
            return;
        }

        Dictionary<string, int> initialPool = new Dictionary<string, int>
        {
            { _projectileId, 20 }
        };

        Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>
        {
            { _projectileId, _projectilePrefab }
        };

        PoolManager.Instance.Init(null, initialPool, prefabMap);

        Debug.Log("[WeaponFire] 발사체 풀 초기화 완료!");
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

            if (_weaponTargeting != null)
            {
                _weaponTargeting.ApplyRange(weaponData.Range);
            }
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

        GameObject projObj = PoolManager.Instance.SpawnFromPool(_projectileId, _firePosition.position, Quaternion.identity);

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