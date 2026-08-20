using UnityEngine;

public class NetworkAugmentService : SingletonBase<NetworkAugmentService>
{
    private const int TOTAL_INVENTORY_SLOT_COUNT = 49; 
    private const int TOTAL_EQUIP_SLOT_COUNT = 21; 
    private const int INITIAL_UNLOCKED_EQUIP_COUNT = 3;

    private AugmentInventoryViewModel _localInventoryVm;
    private AugmentEquipViewModel _localEquipVm;
    private long _lastAugmentUniqueId = 0;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public AugmentInventoryViewModel GetLocalAugmentInventoryViewModel()
    {
        if (_localInventoryVm == null)
        {
            _localInventoryVm = new AugmentInventoryViewModel(TOTAL_INVENTORY_SLOT_COUNT);
        }

        return _localInventoryVm;
    }

    public AugmentEquipViewModel GetLocalAugmentEquipViewModel()
    {
        if (_localEquipVm == null)
        {
            _localEquipVm = new AugmentEquipViewModel(TOTAL_EQUIP_SLOT_COUNT, INITIAL_UNLOCKED_EQUIP_COUNT);
        }

        return _localEquipVm;
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

        var augmentVm = new AugmentSlotViewModel();
        augmentVm.AugmentUniqueId = GenerateAugmentUniqueId();
        augmentVm.AugmentDataId = augmentDataId;

        emptySlot.Augment = augmentVm;
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

        toSlot.Augment = fromSlot.Augment;
        fromSlot.Augment = null;

        return true;
    }

    public bool RequestSell(AugmentSlotContainerViewModel container, int slotIndex)
    {
        var slotState = container.GetSlot(slotIndex);
        if (slotState == null || slotState.Augment == null)
        {
            return false;
        }

        // 증가 정해지면 추후 수정
        slotState.Augment = null;
        return true;
    }

    // 무기 적제 관련 스탯이 변경 될때 호출
    public void SetEquipUnlockedCount(int unlockedCount)
    {
        GetLocalAugmentEquipViewModel().SetUnlockedCount(unlockedCount);
    }

    public void UnlockEquipSlot(int slotIndex)
    {
        GetLocalAugmentEquipViewModel().UnlockSlot(slotIndex);
    }

    private long GenerateAugmentUniqueId()
    {
        _lastAugmentUniqueId += 1;
        return _lastAugmentUniqueId;
    }
}
