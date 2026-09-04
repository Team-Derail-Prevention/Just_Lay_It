using UnityEngine;
using System.Collections.Generic;
using Enums;

public class NetworkAugmentService : SingletonBase<NetworkAugmentService>
{
    private const int TOTAL_INVENTORY_SLOT_COUNT = 49;
    private const int SLOT_COUNT_PER_CAR = 5;
    private const string WEAPON_UPGRADE_ID = "LOBBY_BASE_WEAPON_UPGrade";
    private const int BONUS_WEAPON_UNLOCK_LEVEL = 4;
    private const string BONUS_WEAPON_GRADE = "Rare";
    private static readonly string[] STARTING_WEAPON_GRADE_BY_LEVEL = { "Common", "Rare", "Epic", "Legendary", "Legendary" };

    private static readonly int[] HEAD_UNLOCK_BY_LEVEL = { 2, 2, 2, 3, 4, 5 };
    private static readonly int[] STANDARD1_UNLOCK_BY_LEVEL = { 0, 2, 2, 3, 4, 5 };
    private static readonly int[] STANDARD2_UNLOCK_BY_LEVEL = { 0, 0, 2, 3, 4, 5 };

    private AugmentInventoryViewModel _localInventoryVm;
    private readonly Dictionary<TrainCarSection, AugmentEquipViewModel> _localEquipVmDic = new Dictionary<TrainCarSection, AugmentEquipViewModel>();
    private long _lastAugmentUniqueId = 0;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (AugmentStatEventHub.Instance != null)
        {
            AugmentStatEventHub.Instance.OnStatCalculated -= OnStatCalculated;
            AugmentStatEventHub.Instance.OnStatCalculated += OnStatCalculated;
        }
    }

    private void OnEnable()
    {
        if (AugmentStatEventHub.Instance != null)
        {
            AugmentStatEventHub.Instance.OnStatCalculated += OnStatCalculated;
        }
    }

    private void OnDisable()
    {
        if (AugmentStatEventHub.Instance != null)
        {
            AugmentStatEventHub.Instance.OnStatCalculated -= OnStatCalculated;
        }

    }

    public AugmentInventoryViewModel GetLocalAugmentInventoryViewModel()
    {
        if (_localInventoryVm == null)
        {
            _localInventoryVm = new AugmentInventoryViewModel(TOTAL_INVENTORY_SLOT_COUNT);
        }

        return _localInventoryVm;
    }

    public AugmentEquipViewModel GetLocalWeaponEquipViewModel(TrainCarSection section)
    {
        AugmentEquipViewModel vm;
        if (_localEquipVmDic.TryGetValue(section, out vm) == false)
        {
            int initialUnlockedCount = GetUnlockedCountByLevel(section, 0);
            vm = new AugmentEquipViewModel(section, SLOT_COUNT_PER_CAR, initialUnlockedCount);
            _localEquipVmDic.Add(section, vm);
        }

        return vm;
    }

    public void ApplyWeaponSlotUnlockLevel(int upgradeLevel)
    {
        GetLocalWeaponEquipViewModel(TrainCarSection.Head).SetUnlockedCount(GetUnlockedCountByLevel(TrainCarSection.Head, upgradeLevel));
        GetLocalWeaponEquipViewModel(TrainCarSection.Standard1).SetUnlockedCount(GetUnlockedCountByLevel(TrainCarSection.Standard1, upgradeLevel));
        GetLocalWeaponEquipViewModel(TrainCarSection.Standard2).SetUnlockedCount(GetUnlockedCountByLevel(TrainCarSection.Standard2, upgradeLevel));
    }

    private int GetUnlockedCountByLevel(TrainCarSection section, int upgradeLevel)
    {
        int[] table = GetUnlockTable(section);
        int clampedLevel = Mathf.Clamp(upgradeLevel, 0, table.Length - 1);
        return table[clampedLevel];
    }

    private int[] GetUnlockTable(TrainCarSection section)
    {
        switch (section)
        {
            case TrainCarSection.Head:
                return HEAD_UNLOCK_BY_LEVEL;
            case TrainCarSection.Standard1:
                return STANDARD1_UNLOCK_BY_LEVEL;
            case TrainCarSection.Standard2:
                return STANDARD2_UNLOCK_BY_LEVEL;
            default:
                return HEAD_UNLOCK_BY_LEVEL;
        }
    }

    public bool AddAugment(string augmentDataId)
    {
        var inventoryVm = GetLocalAugmentInventoryViewModel();
        var emptySlot = inventoryVm.FindFirstEmptySlot();
        if (emptySlot == null)
        {
            Debug.LogWarning("[NetworkAugmentService] 보관함에 빈 칸이 없습니다.");
            return false;
        }

        WeaponData weaponData = DataManager.Instance.GetData<WeaponData>(augmentDataId);
        if (weaponData == null)
        {
            Debug.LogWarning($"[NetworkAugmentService] 무기 데이터를 찾을 수 없습니다. (Id: {augmentDataId})");
            return false;
        }

        var augmentVm = new AugmentSlotViewModel();
        augmentVm.AugmentUniqueId = GenerateAugmentUniqueId();
        augmentVm.AugmentDataId = augmentDataId;
        augmentVm.FillFromData(weaponData);

        emptySlot.Augment = augmentVm;

        AugmentStatEventHub.Instance.NotifyStatRequested(augmentVm.AugmentUniqueId, augmentDataId);
        return true;
    }

    public bool RequestMove(AugmentSlotContainerViewModel fromContainer, int fromIndex, AugmentSlotContainerViewModel toContainer, int toIndex)
    {
        var fromSlot = fromContainer.GetSlot(fromIndex);
        var toSlot = toContainer.GetSlot(toIndex);

        if (fromSlot == null || toSlot == null)
        {
            return false;
        }

        if (fromSlot.Augment == null)
        {
            return false;
        }

        if (toSlot.IsLocked == true)
        {
            Debug.LogWarning("[NetworkAugmentService] 잠긴 칸에는 넣을 수 없습니다.");
            return false;
        }

        if (toSlot.Augment != null)
        {
            Debug.LogWarning("[NetworkAugmentService] 이미 다른 증강이 있는 칸입니다.");
            return false;
        }

        string movedWeaponDataId = fromSlot.Augment.AugmentDataId;

        toSlot.Augment = fromSlot.Augment;
        fromSlot.Augment = null;

        NotifyEquipChanged(fromContainer, fromIndex, null);
        NotifyEquipChanged(toContainer, toIndex, movedWeaponDataId);

        return true;
    }

    public bool RequestSell(AugmentSlotContainerViewModel container, int slotIndex)
    {
        var slotState = container.GetSlot(slotIndex);
        if (slotState == null || slotState.Augment == null)
        {
            return false;
        }

        int sellPrice = slotState.Augment.Price;

        slotState.Augment = null;
        NotifyEquipChanged(container, slotIndex, null);

        if (NetworkResourceService.Instance != null)
        {
            NetworkResourceService.Instance.AddStoneWithWarehouseOverflow(sellPrice);
        }
        else
        {
            Debug.LogWarning("[NetworkAugmentService] NetworkResourceService가 없어 판매 재화를 지급하지 못했습니다.");
        }

        return true;
    }

    private void NotifyEquipChanged(AugmentSlotContainerViewModel container, int slotIndex, string weaponDataId)
    {
        AugmentEquipViewModel equipVm = container as AugmentEquipViewModel;
        if (equipVm == null)
        {
            return;
        }

        if (WeaponEquipEventHub.Instance == null)
        {
            Debug.LogWarning("[NetworkAugmentService] WeaponEquipEventHub가 없어 장착/해제 알림을 보내지 못했습니다.");
            return;
        }

        if (string.IsNullOrEmpty(weaponDataId))
        {
            WeaponEquipEventHub.Instance.NotifyWeaponUnequipped(equipVm.Section, slotIndex);
        }
        else
        {
            WeaponEquipEventHub.Instance.NotifyWeaponEquipped(equipVm.Section, slotIndex, weaponDataId);
        }
    }

    private long GenerateAugmentUniqueId()
    {
        _lastAugmentUniqueId += 1;
        return _lastAugmentUniqueId;
    }

    public void ResetRun()
    {
        _localInventoryVm = null;
        _localEquipVmDic.Clear();
        _lastAugmentUniqueId = 0;
    }

    public void GrantRandomStartingWeapon()
    {
        int upgradeLevel = GetLobbyWeaponUpgradeLevel();
        string mainGradeName = GetGradeNameByLevel(upgradeLevel);

        GrantWeaponToSlot(TrainCarSection.Head, 0, mainGradeName);

        if (upgradeLevel >= BONUS_WEAPON_UNLOCK_LEVEL)
        {
            GrantWeaponToSlot(TrainCarSection.Head, 1, BONUS_WEAPON_GRADE);
        }
    }

    private void GrantWeaponToSlot(TrainCarSection section, int slotIndex, string gradeName)
    {
        var equipVm = GetLocalWeaponEquipViewModel(section);
        var slotState = equipVm.GetSlot(slotIndex);
        if (slotState == null)
        {
            return;
        }

        if (slotState.Augment != null)
        {
            Debug.LogWarning($"[NetworkAugmentService] 시작 무기 지급 슬롯에 잔여 무기가 있어 덮어씁니다. (Section: {section}, Slot: {slotIndex}, 기존 Id: {slotState.Augment.AugmentDataId})");
            slotState.Augment = null;
        }

        List<WeaponData> pool = GetStartingWeaponPool(gradeName);
        if (pool.Count == 0)
        {
            Debug.LogWarning($"[NetworkAugmentService] '{gradeName}' 등급의 시작 무기 풀이 비어있습니다. (Section: {section}, Slot: {slotIndex})");
            return;
        }

        WeaponData pickedData = pool[Random.Range(0, pool.Count)];

        var augmentVm = new AugmentSlotViewModel();
        augmentVm.AugmentUniqueId = GenerateAugmentUniqueId();
        augmentVm.AugmentDataId = pickedData.Id;
        augmentVm.FillFromData(pickedData);

        slotState.Augment = augmentVm;
        WeaponEquipEventHub.Instance.NotifyWeaponEquipped(section, slotIndex, pickedData.Id);

        AugmentStatEventHub.Instance.NotifyStatRequested(augmentVm.AugmentUniqueId, pickedData.Id);
    }

    private string GetGradeNameByLevel(int upgradeLevel)
    {
        int clampedLevel = Mathf.Clamp(upgradeLevel, 0, STARTING_WEAPON_GRADE_BY_LEVEL.Length - 1);
        return STARTING_WEAPON_GRADE_BY_LEVEL[clampedLevel];
    }

    private int GetLobbyWeaponUpgradeLevel()
    {
        if (NetworkUpgradeService.Instance == null)
        {
            return 0;
        }

        var upgradeVm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
        if (upgradeVm == null)
        {
            return 0;
        }

        var slotVm = upgradeVm.GetSlot(WEAPON_UPGRADE_ID);
        return slotVm != null ? slotVm.CurrentLevel : 0;
    }

    private List<WeaponData> GetStartingWeaponPool(string gradeName)
    {
        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            Debug.LogWarning("[NetworkAugmentService] 데이터가 아직 로드되지 않았습니다.");
            return new List<WeaponData>();
        }

        List<WeaponData> weaponList = new List<WeaponData>();
        foreach (WeaponData weaponData in DataManager.Instance.GetAllData<WeaponData>())
        {
            if (weaponData.GradeName == gradeName)
            {
                weaponList.Add(weaponData);
            }
        }

        return weaponList;
    }

    private void OnStatCalculated(long augmentUniqueId, WeaponCurrentStats stats)
    {
        AugmentSlotViewModel augmentVm = FindAugmentByUniqueId(augmentUniqueId);
        if (augmentVm == null)
        {
            return;
        }

        augmentVm.SetStats(stats);
    }

    private AugmentSlotViewModel FindAugmentByUniqueId(long augmentUniqueId)
    {
        AugmentSlotViewModel found = FindAugmentInContainer(_localInventoryVm, augmentUniqueId);
        if (found != null)
        {
            return found;
        }

        foreach (var equipVm in _localEquipVmDic.Values)
        {
            found = FindAugmentInContainer(equipVm, augmentUniqueId);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private AugmentSlotViewModel FindAugmentInContainer(AugmentSlotContainerViewModel container, long augmentUniqueId)
    {
        if (container == null)
        {
            return null;
        }

        int slotCount = container.GetSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            AugmentSlotState slotState = container.GetSlot(i);
            if (slotState != null && slotState.Augment != null && slotState.Augment.AugmentUniqueId == augmentUniqueId)
            {
                return slotState.Augment;
            }
        }

        return null;
    }
}
