using System.Collections.Generic;
using UnityEngine;

public class AugmentSlotContainerViewModel : ViewModelBase
{
    protected readonly List<AugmentSlotState> _slotList = new List<AugmentSlotState>();

    protected void CreateSlots(int totalSlotCount, int initialUnlockedCount)
    {
        for (int i = 0; i < totalSlotCount; i++)
        {
            var slotState = new AugmentSlotState();
            slotState.SlotIndex = i;
            slotState.IsLocked = (i >= initialUnlockedCount);
            _slotList.Add(slotState);
        }
    }

    public AugmentSlotState GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotList.Count)
        {
            return null;
        }

        return _slotList[slotIndex];
    }

    public int GetSlotCount()
    {
        return _slotList.Count;
    }

    public AugmentSlotState FindFirstEmptySlot()
    {
        foreach (var slotState in _slotList)
        {
            if (slotState.IsLocked == false && slotState.Augment == null)
            {
                return slotState;
            }
        }

        return null;
    }
}
