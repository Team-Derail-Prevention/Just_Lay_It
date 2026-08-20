using UnityEngine;

public class AugmentInventoryViewModel : AugmentSlotContainerViewModel
{
    public AugmentInventoryViewModel(int totalSlotCount)
    {
        CreateSlots(totalSlotCount, totalSlotCount);
    }
}
