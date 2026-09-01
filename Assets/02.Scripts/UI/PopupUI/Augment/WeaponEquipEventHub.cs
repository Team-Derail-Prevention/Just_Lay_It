using Enums;
using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class WeaponEquipEventHub : SingletonBase<WeaponEquipEventHub>
{
    public static event Action<TrainCarSection, int, string> OnWeaponEquipped;
    public static event Action<TrainCarSection, int> OnWeaponUnequipped;

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
