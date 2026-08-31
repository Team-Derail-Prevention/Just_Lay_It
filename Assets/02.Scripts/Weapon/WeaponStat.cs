using UnityEngine;

public class WeaponStat : SingletonBase<WeaponStat>
{
    protected override void Init()
    {
        base.Init();
    }

    private void OnEnable()
    {
        if (AugmentStatEventHub.Instance != null)
        {
            AugmentStatEventHub.Instance.OnStatRequested += HandleStatRequested;
        }
    }

    private void OnDisable()
    {
        if (AugmentStatEventHub.Instance != null)
        {
            AugmentStatEventHub.Instance.OnStatRequested -= HandleStatRequested;
        }
    }

    private void HandleStatRequested(long augmentUniqueId, string weaponDataId)
    {
        if (WeaponStatManager.TryGetCurrentWeaponStats(weaponDataId, out WeaponCurrentStats stats))
        {
            AugmentStatEventHub.Instance.NotifyStatCalculated(augmentUniqueId, stats);
        }
        else
        {
            Debug.LogWarning($"[WeaponStat] {weaponDataId} 스탯 계산 실패");
        }
    }
}
