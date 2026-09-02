using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

public struct WeaponLobbyStats
{
    public int Atk;
    public float FireRate;
    public int MagazineSize;
    public float ReloadTime;
    public float Range;
}

public static class WeaponStat
{
    public static WeaponLobbyStats GetLobbyStats(string weaponId)
    {
        WeaponLobbyStats stats = default;

        if (!TryCalculate(weaponId, out int atk, out float fireRate, out int magazine, out float reload, out float range))
        {
            return stats;
        }

        stats.Atk = atk;
        stats.FireRate = fireRate;
        stats.MagazineSize = magazine;
        stats.ReloadTime = reload;
        stats.Range = range;

        return stats;
    }

    // 무기 Id만으로 스탯 계산 (장착 여부 무관, 미리보기/상점 UI용)
    public static bool TryGetCurrentWeaponStats(string weaponId, out WeaponCurrentStats stats)
    {
        stats = default;

        if (!TryCalculate(weaponId, out int atk, out float fireRate, out int magazine, out float reload, out float range))
        {
            return false;
        }

        stats.Atk = atk;
        stats.FireRate = fireRate;
        stats.MagazineSize = magazine;
        stats.ReloadTime = reload;
        stats.Range = range;
        stats.CurrentAmmo = magazine;

        return true;
    }

    // 공통 계산 로직 (GetLobbyStats / TryGetCalculatedStats가 함께 사용)
    private static bool TryCalculate(string weaponId, out int atk, out float fireRate, out int magazine, out float reload, out float range)
    {
        atk = 0;
        fireRate = 0f;
        magazine = 0;
        reload = 0f;
        range = 0f;

        WeaponData weaponData = DataManager.Instance.GetData<WeaponData>(weaponId);
        if (weaponData == null)
        {
            Debug.LogError($"[WeaponStatManager] {weaponId} 무기 데이터를 찾을 수 없습니다");
            return false;
        }

        int atkLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_ATK");
        int magazineLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_MAGAZINE");
        int reloadLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_RELOAD");
        int rangeLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_RANGE");
        int battleDamageLevel = GetInGameUpgradeLevel("BATTLE_DAMAGE");

        atk = weaponData.Atk
            + (atkLevel * weaponData.LobbyATKByLevel)
            + (battleDamageLevel * weaponData.InGameATKByLevel);
        fireRate = weaponData.FireRate;
        magazine = weaponData.MagazineSize + (magazineLevel * weaponData.LobbyMagazineByLevel);
        reload = Mathf.Max(0f, weaponData.ReloadTime - (reloadLevel * weaponData.LobbyReloadByLevel));
        range = weaponData.Range + (rangeLevel * weaponData.LobbyRangeByLevel);

        return true;
    }

    public static int GetLobbyUpgradeLevel(string slotDataId)
    {
        if (NetworkUpgradeService.Instance == null)
        {
            return 0;
        }

        UpgradeViewModel upgradeValue = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
        if (upgradeValue == null)
        {
            return 0;
        }

        UpgradeSlotViewModel slotValue = upgradeValue.GetSlot(slotDataId);
        if (slotValue == null)
        {
            return 0;
        }

        return slotValue.CurrentLevel;
    }

    public static int GetInGameUpgradeLevel(string slotDataId)
    {
        if (NetworkTrainStrengtheningService.Instance == null)
        {
            return 0;
        }

        TrainStrengtheningViewModel upgradeValue = NetworkTrainStrengtheningService.Instance.GetLocalTrainStrengtheningViewModel();
        if (upgradeValue == null)
        {
            return 0;
        }

        TrainStatSlotViewModel slotValue = upgradeValue.GetSlot(slotDataId);
        if (slotValue == null)
        {
            return 0;
        }

        return slotValue.CurrentLevel;
    }
}