using Enums;
using UnityEngine;

public class AugmentEquipViewModel : AugmentSlotContainerViewModel
{
    public TrainCarSection Section { get; private set; }

    public AugmentEquipViewModel(TrainCarSection section, int totalSlotCount, int initialUnlockedCount)
    {
        Section = section;
        CreateSlots(totalSlotCount, initialUnlockedCount);
    }

    public void UnlockSlot(int slotIndex)
    {
        var slotState = GetSlot(slotIndex);
        if (slotState != null)
        {
            slotState.IsLocked = false;
        }
    }

    public void SetUnlockedCount(int unlockedCount)
    {
        for (int i = 0; i < _slotList.Count; i++)
        {
            _slotList[i].IsLocked = (i >= unlockedCount);
        }
    }
}
