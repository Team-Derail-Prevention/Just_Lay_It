using UnityEngine;


public struct TrainCurrentStats
{
    public int MaxHp;
    public int Defense;
}

public static class TrainStat
{
    private const int HP_PER_LEVEL = 50;
    private const int DEFENSE_PER_LEVEL = 2;

    public static bool TryGetCurrentTrainStats(string trainId, out TrainCurrentStats stats)
    {
        stats = default;

        if (DataManager.Instance == null)
        {
            Debug.LogError("[TrainStat] DataManager를 찾을 수 없습니다.");
            return false;
        }

        TrainData trainData = DataManager.Instance.GetData<TrainData>(trainId);
        if (trainData == null)
        {
            Debug.LogError($"[TrainStat] {trainId} 기차 데이터를 찾을 수 없습니다.");
            return false;
        }

        // 인게임 강화 레벨 조회
        int hpLevel = GetInGameUpgradeLevel("INGAME_TRAIN_MAX_HP");
        if (hpLevel == 0) hpLevel = GetInGameUpgradeLevel("BATTLE_HP");

        int defLevel = GetInGameUpgradeLevel("INGAME_TRAIN_DEFENSE");
        if (defLevel == 0) defLevel = GetInGameUpgradeLevel("BATTLE_DEFENSE");

        // 합산 스탯 연산
        stats.MaxHp = trainData.MaxHp + (hpLevel * HP_PER_LEVEL);
        stats.Defense = trainData.Defense + (defLevel * DEFENSE_PER_LEVEL);

        return true;
    }

    public static int GetInGameUpgradeLevel(string slotDataId)
    {
        if (NetworkTrainStrengtheningService.Instance == null)
        {
            return 0;
        }

        var upgradeValue = NetworkTrainStrengtheningService.Instance.GetLocalTrainStrengtheningViewModel();
        if (upgradeValue == null)
        {
            return 0;
        }

        var slotValue = upgradeValue.GetSlot(slotDataId);
        return slotValue != null ? slotValue.CurrentLevel : 0;
    }
}
