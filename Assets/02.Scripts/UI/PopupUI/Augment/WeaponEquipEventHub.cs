using UnityEngine;
using System;
using Enums;

public class WeaponEquipEventHub : SingletonBase<WeaponEquipEventHub>
{
    public event Action<TrainCarSection, int, string> OnWeaponEquipped;
    public event Action<TrainCarSection, int> OnWeaponUnequipped;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyWeaponEquipped(TrainCarSection section, int slotIndex, string weaponDataId)
    {
        OnWeaponEquipped?.Invoke(section, slotIndex, weaponDataId);
    }

    public void NotifyWeaponUnequipped(TrainCarSection section, int slotIndex)
    {
        OnWeaponUnequipped?.Invoke(section, slotIndex);
    }
}
