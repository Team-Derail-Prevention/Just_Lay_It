using UnityEngine;

public struct WeaponLobbyStats
{
    public int Atk;
    public float FireRate;
    public int MagazineSize;
    public float ReloadTime;
    public float Range;
}

public static class WeaponStatManager
{
    public static WeaponLobbyStats GetLobbyStats(string weaponId)
    {
        WeaponLobbyStats stats = default;

        WeaponData weaponData = DataManager.Instance.GetData<WeaponData>(weaponId);
        if (weaponData == null)
        {
            Debug.LogError($"[WeaponStatCalculator] {weaponId} 무기 데이터를 찾을 수 없습니다");
            return stats;
        }

        int atkLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_ATK");
        int magazineLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_MAGAZINE");
        int reloadLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_RELOAD");
        int rangeLevel = GetLobbyUpgradeLevel("LOBBY_WEAPON_RANGE");

        stats.Atk = weaponData.Atk + (atkLevel * weaponData.LobbyATKByLevel);
        stats.FireRate = weaponData.FireRate;
        stats.MagazineSize = weaponData.MagazineSize + (magazineLevel * weaponData.LobbyMagazineByLevel);
        stats.ReloadTime = Mathf.Max(0f, weaponData.ReloadTime - (reloadLevel * weaponData.LobbyReloadByLevel));
        stats.Range = weaponData.Range + (rangeLevel * weaponData.LobbyRangeByLevel);

        return stats;
    }

    public static int GetLobbyUpgradeLevel(string slotDataId)
    {
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
}