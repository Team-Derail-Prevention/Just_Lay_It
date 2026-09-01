using UnityEngine;


public struct TrainCurrentStats
{
    public int MaxHp;
    public int Defense;
}

public static class TrainStat
{

    //로비
    private const int LOBBY_HP_STEP = 50;
    private const int LOBBY_DEFENSE_STEP = 2;
    //인게임
    private const int INGAME_HP_STEP = 50;
    private const int INGAME_DEFENSE_STEP = 2;


    /// 기본 데이터 + 로비 영구 강화 + 인게임 세션 강화를 모두 합산해 최종 스탯 계산
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

        //로비 강화 레벨 조회
        int lobbyHpLevel = GetLobbyUpgradeLevel("LOBBY_TRAIN_MAX_HP");
        if (lobbyHpLevel == 0) lobbyHpLevel = GetLobbyUpgradeLevel("TRAIN_MAX_HP");

        int lobbyDefLevel = GetLobbyUpgradeLevel("LOBBY_TRAIN_DEFENSE");
        if (lobbyDefLevel == 0) lobbyDefLevel = GetLobbyUpgradeLevel("TRAIN_DEFENSE");

        // 인게임 강화 레벨 조회
        int inGameHpLevel = GetInGameUpgradeLevel("INGAME_TRAIN_MAX_HP");
        if (inGameHpLevel == 0) inGameHpLevel = GetInGameUpgradeLevel("BATTLE_HP");

        int inGameDefLevel = GetInGameUpgradeLevel("INGAME_TRAIN_DEFENSE");
        if (inGameDefLevel == 0) inGameDefLevel = GetInGameUpgradeLevel("BATTLE_DEFENSE");

        //로비 관련 요청 들어오면 사용
        stats.MaxHp = trainData.MaxHp
                    + (lobbyHpLevel * LOBBY_HP_STEP)
                    + (inGameHpLevel * INGAME_HP_STEP);

        stats.Defense = trainData.Defense
                      + (lobbyDefLevel * LOBBY_DEFENSE_STEP)
                      + (inGameDefLevel * INGAME_DEFENSE_STEP);

        Debug.Log($"[TrainStat 연산]</color> TrainId: {trainId} | 기본HP: {trainData.MaxHp}, 로비HP Lv: {lobbyHpLevel}(+{lobbyHpLevel * LOBBY_HP_STEP}), 인게임HP Lv: {inGameHpLevel}(+{inGameHpLevel * INGAME_HP_STEP}) => 최종 MaxHp: {stats.MaxHp} | 방어력: {stats.Defense}");
        return true;
    }

    // 로비 강화 서비스(NetworkUpgradeService)에서 현재 레벨 조회
    public static int GetLobbyUpgradeLevel(string slotDataId)
    {
        if (NetworkUpgradeService.Instance == null)
        {
            return 0;
        }

        var upgradeValue = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
        if (upgradeValue == null)
        {
            return 0;
        }

        var slotValue = upgradeValue.GetSlot(slotDataId);
        return slotValue != null ? slotValue.CurrentLevel : 0;
    }




    //인게임 강화 서비스(NetworkTrainStrengtheningService)에서 현재 레벨 조회
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
