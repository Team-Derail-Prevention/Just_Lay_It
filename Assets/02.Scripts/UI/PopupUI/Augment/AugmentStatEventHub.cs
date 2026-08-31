using UnityEngine;
using System;

public class AugmentStatEventHub : SingletonBase<AugmentStatEventHub>
{
    public event Action<long, string> OnStatRequested;
    public event Action<long, WeaponCurrentStats> OnStatCalculated;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyStatRequested(long augmentUniqueId, string weaponDataId)
    {
        OnStatRequested?.Invoke(augmentUniqueId, weaponDataId);
    }

    public void NotifyStatCalculated(long augmentUniqueId, WeaponCurrentStats stats)
    {
        OnStatCalculated?.Invoke(augmentUniqueId, stats);
    }
}
