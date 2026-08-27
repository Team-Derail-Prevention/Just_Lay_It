using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WeaponFire : MonoBehaviour
{
    [SerializeField] private string _weaponId;
    [SerializeField] private string _projectileId;
    [SerializeField] private Transform _firePosition;
    [SerializeField] private WeaponTargeting _weaponTargeting;

    [Header("레벨당 증가 스탯")]
    [SerializeField] private int _magazineByLevel = 5; // 레벨당 탄창 증가량
    [SerializeField] private int _atkByLevel = 2; // 레벨당 공격력 증가량
    [SerializeField] private float _reloadByLevel = 0.2f; // 레벨당 재장전 속도 감소량
    [SerializeField] private float _rangeByLevel = 1f; // 레벨당 사거리 증가량

    private GameObject _projectilePrefab;

    private int _weaponAtk;
    private float _fireRate;
    private int _magazineSize;
    private float _reloadTime;

    private int _currentAmmo;
    private float _fireTimer = 0f;
    private float _reloadTimer = 0f;
    private bool _isReloading = false;

    private int _baseAtk;
    private float _baseRange;

    private int _lobbyAtkBonus;
    private int _battleDamageBonus;

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
        if (DataManager.Instance == null)
        {
            Debug.LogError("[WeaponFire] 데이터 매니저가 인스탄스 되어있지 않습니다");
            return;
        }
        LoadWeaponData();
        await RegisterProjectilePoolAsync();
        InitWeponLevel();
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

    private void OnEnable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded += OnInGameUpgraded;
        }
    }

    private void OnDisable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded -= OnInGameUpgraded;
        }
    }

    private void OnInGameUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId != "BATTLE_DAMAGE")
        {
            return;
        }

        _battleDamageBonus = newLevel * _atkByLevel;
        RecalculateAtk();
    }

    private void InitWeponLevel()
    {
        int reloadLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_RELOAD");
        int magazineLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_MAGAZINE");
        int atkLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_ATK");
        int rangeLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_RANGE");

        _reloadTime = Mathf.Max(0f, _reloadTime - (reloadLevel * _reloadByLevel));
        _magazineSize += magazineLevel * _magazineByLevel;
        _currentAmmo = _magazineSize;

        _lobbyAtkBonus = atkLevel * _atkByLevel;
        RecalculateAtk();

        float finalRange = _baseRange + (rangeLevel * _rangeByLevel);
        if (_weaponTargeting != null)
        {
            _weaponTargeting.ApplyRange(finalRange);
        }
    }

    private int GetLobbyUpgradeLevel(string slotDataId)
    {
        UpgradeViewModel upgradeValue = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
        if (upgradeValue == null)
        {
            return 0;
        }

        UpgradeSlotViewModel slotValue = upgradeValue.GetSlot(slotDataId);
        if (slotValue == null)
        {
            return 0;
        }

        return slotValue.CurrentLevel;
    }

    private void RecalculateAtk()
    {
        _weaponAtk = _baseAtk + _lobbyAtkBonus + _battleDamageBonus;
    }

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

    private void LoadWeaponData()
    {
        WeaponData weaponData = DataManager.Instance.GetData<WeaponData>(_weaponId);

        if (weaponData != null)
        {
            _baseAtk = weaponData.Atk;
            _fireRate = weaponData.FireRate;
            _magazineSize = weaponData.MagazineSize;
            _reloadTime = weaponData.ReloadTime;
            _baseRange = weaponData.Range;
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