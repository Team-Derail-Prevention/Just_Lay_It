using UnityEngine;
using System.Collections.Generic;
using Enums;

public class NetworkAugmentService : SingletonBase<NetworkAugmentService>
{
    private const int TOTAL_INVENTORY_SLOT_COUNT = 49;
    private const int SLOT_COUNT_PER_CAR = 5;
    private const string STARTING_WEAPON_GRADE = "Common";

    private static readonly int[] HEAD_UNLOCK_BY_LEVEL = { 2, 2, 2, 3, 4, 5 };
    private static readonly int[] STANDARD1_UNLOCK_BY_LEVEL = { 0, 2, 2, 3, 4, 5 };
    private static readonly int[] STANDARD2_UNLOCK_BY_LEVEL = { 0, 0, 2, 3, 4, 5 };

    private AugmentInventoryViewModel _localInventoryVm;
    private readonly Dictionary<TrainCarSection, AugmentEquipViewModel> _localEquipVmDic = new Dictionary<TrainCarSection, AugmentEquipViewModel>();
    private long _lastAugmentUniqueId = 0;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        AugmentStatEventHub.Instance.OnStatCalculated += OnStatCalculated;
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

        slotState.Augment = null;
        NotifyEquipChanged(container, slotIndex, null);

        return true;
    }

    private void NotifyEquipChanged(AugmentSlotContainerViewModel container, int slotIndex, string weaponDataId)
    {
        AugmentEquipViewModel equipVm = container as AugmentEquipViewModel;
        if (equipVm == null)
        {
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
        var equipVm = GetLocalWeaponEquipViewModel(TrainCarSection.Head);
        var slotState = equipVm.GetSlot(0);
        if (slotState == null || slotState.Augment != null)
        {
            return;
        }

        List<WeaponData> pool = GetStartingWeaponPool();
        if (pool.Count == 0)
        {
            Debug.LogWarning("[NetworkAugmentService] 기본 지급용 무기 데이터가 없습니다.");
            return;
        }

        WeaponData pickedData = pool[Random.Range(0, pool.Count)];

        var augmentVm = new AugmentSlotViewModel();
        augmentVm.AugmentUniqueId = GenerateAugmentUniqueId();
        augmentVm.AugmentDataId = pickedData.Id;
        augmentVm.FillFromData(pickedData);

        slotState.Augment = augmentVm;
        WeaponEquipEventHub.Instance.NotifyWeaponEquipped(TrainCarSection.Head, 0, pickedData.Id);

        AugmentStatEventHub.Instance.NotifyStatRequested(augmentVm.AugmentUniqueId, pickedData.Id);
    }

    private List<WeaponData> GetStartingWeaponPool()
    {
        if (DataManager.Instance == null || DataManager.Instance.IsLoaded == false)
        {
            Debug.LogWarning("[NetworkAugmentService] 데이터가 아직 로드되지 않았습니다.");
            return new List<WeaponData>();
        }

        List<WeaponData> commonWeaponList = new List<WeaponData>();
        foreach (WeaponData weaponData in DataManager.Instance.GetAllData<WeaponData>())
        {
            if (weaponData.GradeName == STARTING_WEAPON_GRADE)
            {
                commonWeaponList.Add(weaponData);
            }
        }

        return commonWeaponList;
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
