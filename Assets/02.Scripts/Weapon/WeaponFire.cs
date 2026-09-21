using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public struct WeaponCurrentStats
{
    public int Atk;
    public float FireRate;
    public int MagazineSize;
    public float ReloadTime;
    public float Range;
    public int CurrentAmmo;
}

public class WeaponFire : MonoBehaviour
{
    [SerializeField] private string _weaponId;
    [SerializeField] private string _projectileId;
    [SerializeField] private Transform _firePosition;
    [SerializeField] private WeaponTargeting _weaponTargeting;
    [SerializeField] private Transform _auraRoot;

    private WeaponData _weaponData;
    private GameObject _projectilePrefab;

    private int _weaponAtk;
    private float _fireRate;
    private int _magazineSize;
    private float _reloadTime;
    private float _currentRange;

    private int _currentAmmo;
    private float _fireTimer = 0f;
    private float _reloadTimer = 0f;
    private bool _isReloading = false;

    private int _baseAtk;
    private float _baseRange;

    private int _lobbyAtkBonus;
    private bool _weaponIdAssigned;

    private Train _parentTrain;

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

        _parentTrain = FindAnyObjectByType<Train>();
    }

    private async void Start()
    {
        if (_weaponIdAssigned)
        {
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogError("[WeaponFire] 데이터 매니저가 인스탄스 되어있지 않습니다");
            return;
        }

        await InitializeAsync();
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

        if (_fireTimer < GetModifiedFireRate())
        {
            return;
        }

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

    public async void SetWeaponId(string weaponId)
    {
        _weaponIdAssigned = true;
        _weaponId = weaponId;

        await InitializeAsync();
    }

    public WeaponCurrentStats GetCurrentStats()
    {
        WeaponCurrentStats stats;
        stats.Atk = _weaponAtk;
        stats.FireRate = _fireRate;
        stats.MagazineSize = _magazineSize;
        stats.ReloadTime = _reloadTime;
        stats.Range = _currentRange;
        stats.CurrentAmmo = _currentAmmo;

        return stats;
    }

    public void ApplyAuraColor()
    {
        ApplyGradeAura();
    }

    private float GetModifiedFireRate()
    {
        if (_parentTrain != null && _parentTrain.IsFrozen)
        {
            return _fireRate * 2.0f;
        }
        return _fireRate;
    }

    private void OnInGameUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId != "BATTLE_DAMAGE" || _weaponData == null)
        {
            return;
        }

        RecalculateAtk();
    }

    private void InitWeponLevel()
    {
        if (_weaponData == null)
        {
            return;
        }

        int reloadLevel = WeaponStat.GetLobbyUpgradeLevel("LOBBY_WEAPON_RELOAD");
        int magazineLevel = WeaponStat.GetLobbyUpgradeLevel("LOBBY_WEAPON_MAGAZINE");
        int atkLevel = WeaponStat.GetLobbyUpgradeLevel("LOBBY_WEAPON_ATK");
        int rangeLevel = WeaponStat.GetLobbyUpgradeLevel("LOBBY_WEAPON_RANGE");

        _reloadTime = Mathf.Max(0f, _reloadTime - (reloadLevel * _weaponData.LobbyReloadByLevel));
        _magazineSize += magazineLevel * _weaponData.LobbyMagazineByLevel;
        _currentAmmo = _magazineSize;

        _lobbyAtkBonus = atkLevel * _weaponData.LobbyATKByLevel;
        RecalculateAtk();

        _currentRange = _baseRange + (rangeLevel * _weaponData.LobbyRangeByLevel);
        if (_weaponTargeting != null)
        {
            _weaponTargeting.ApplyRange(_currentRange);
        }
    }

    private void RecalculateAtk()
    {
        int battleDamageLevel = WeaponStat.GetInGameUpgradeLevel("BATTLE_DAMAGE");
        int battleDamageBonus = battleDamageLevel * _weaponData.InGameATKByLevel;

        _weaponAtk = _baseAtk + _lobbyAtkBonus + battleDamageBonus;
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
        _weaponData = DataManager.Instance.GetData<WeaponData>(_weaponId);

        if (_weaponData != null)
        {

            _baseAtk = _weaponData.Atk;
            _fireRate = _weaponData.FireRate;
            _magazineSize = _weaponData.MagazineSize;
            _reloadTime = _weaponData.ReloadTime;
            _baseRange = _weaponData.Range;
            _currentAmmo = _magazineSize;

            ApplyGradeAura();
        }
        else
        {
            Debug.LogError($"{_weaponId} 무기 데이터를 찾을 수 없습니다");
        }

        Debug.Log($"[WeaponFire] LoadWeaponData: id={_weaponId}, aura={_weaponData.AuraColor}, frame={Time.frameCount}", this);
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

        Vector3 targetCenter = new Vector3(target.position.x, _firePosition.position.y, target.position.z);
        Vector3 shootDir = (targetCenter - _firePosition.position).normalized;

        Quaternion spawnRotation = shootDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(shootDir)
            : Quaternion.identity;

        GameObject projObj = PoolManager.Instance.SpawnFromPool(_projectileId, _firePosition.position, spawnRotation);

        PlayWeaponSfx(_weaponData?.UseFireSound, SfxAddress.Weapon.Fire);

        WeaponProjectile projectile = projObj.GetComponent<WeaponProjectile>();
        if (projectile != null)
        {
            int finalAtk = _weaponAtk;
            if (_parentTrain != null && _parentTrain.IsElectrified)
            {
                finalAtk = Mathf.RoundToInt(finalAtk * 0.5f);
            }

            projectile.ProjectileInitialize(shootDir, finalAtk);
        }

        _currentAmmo--;
    }

    private void StartReload()
    {
        _isReloading = true;
        _reloadTimer = 0f;

        PlayWeaponSfx(_weaponData?.UseReloadSound, SfxAddress.Weapon.Reload);
    }

    private void PlayWeaponSfx(string dataSoundName, string fallbackAddress)
    {
        string address = SfxAddress.Resolve(SfxAddress.Weapon.Prefix, dataSoundName, fallbackAddress);

        SoundManager.Instance?.PlaySFXAt(address, _firePosition.position);
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

    private void ApplyGradeAura()
    {
        Debug.Log($"[WeaponFire] ApplyGradeAura 진입. _auraRoot={_auraRoot}, WeaponAura.Instance={WeaponAura.Instance}, AuraColor={_weaponData?.AuraColor}");

        if (_auraRoot == null)
        {
            Debug.LogWarning("[WeaponFire] _auraRoot가 null입니다");
            return;
        }

        if (WeaponAura.Instance == null)
        {
            Debug.LogWarning("[WeaponFire] WeaponAura.Instance가 null입니다");
            return;
        }

        WeaponAura.Instance.ApplyGradeColor(_auraRoot.gameObject, _weaponData.AuraColor);
    }



    private async UniTask InitializeAsync()
    {
        LoadWeaponData();
        await RegisterProjectilePoolAsync();
        InitWeponLevel();
    }
}