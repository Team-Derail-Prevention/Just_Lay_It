using UnityEngine;
using System;

public class NetworkRailService : SingletonBase<NetworkRailService>
{
    private const float CRAFT_DURATION = 1.5f;
    private const float CRAFT_SPEED_PERCENT_PER_LEVEL = 0.1f;
    private const int CRAFT_WOOD_COST = 2;

    private const int BONUS_RAIL_COUNT_PER_LEVEL = 2;

    private RailBuildViewModel _localRailBuildViewModel;
    private int _sessionCraftedCount;
    private int _sessionInstalledCount;

    public int SessionCraftedCount => _sessionCraftedCount;
    public int SessionInstalledCount => _sessionInstalledCount;

    public event Action<RailType> OnRequestPlaceMode;
    private int GetLobbyUpgradeLevel(string slotDataId)
    {
        if (NetworkUpgradeService.Instance == null)
        {
            return 0;
        }

        UpgradeViewModel upgradeVm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
        UpgradeSlotViewModel slotVm = upgradeVm?.GetSlot(slotDataId);

        return slotVm != null ? slotVm.CurrentLevel : 0;
    }

    private int GetBonusBaseRailCount()
    {
        return GetLobbyUpgradeLevel("LOBBY_BASE_RAIL_COUNT") * BONUS_RAIL_COUNT_PER_LEVEL;
    }

    private float GetCraftSpeedPercent()
    {
        return GetLobbyUpgradeLevel("LOBBY_RAIL_CRAFT_SPEED") * CRAFT_SPEED_PERCENT_PER_LEVEL;
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        TickCraftProgress(RailType.Straight);
        TickCraftProgress(RailType.Corner);

        TryAutoCraftStraight();
    }

    public RailBuildViewModel GetLocalRailBuildViewModel()
    {
        if (_localRailBuildViewModel == null)
        {
            _localRailBuildViewModel = new RailBuildViewModel(GetBonusBaseRailCount());
        }

        return _localRailBuildViewModel;
    }

    private void TickCraftProgress(RailType railType)
    {
        var slot = GetLocalRailBuildViewModel().GetSlot(railType);
        if (slot.CraftQueueCount <= 0)
        {
            return;
        }

        float actualDuration = CRAFT_DURATION * (1f - GetCraftSpeedPercent());
        slot.CraftProgress01 += Time.deltaTime / actualDuration;
        if (slot.CraftProgress01 < 1f)
        {
            return;
        }

        slot.CraftProgress01 = 0f;
        slot.CraftQueueCount -= 1;
        slot.OwnedCount += 1;
        _sessionCraftedCount += 1;
    }

    public bool RequestCraft(RailType railType)
    {
        if (NetworkResourceService.Instance == null || NetworkResourceService.Instance.TrySpendWood(CRAFT_WOOD_COST) == false)
        {
            Debug.LogWarning("[NetworkRailService] 나무가 부족합니다.");
            return false;
        }

        var slot = GetLocalRailBuildViewModel().GetSlot(railType);
        slot.CraftQueueCount += 1;
        return true;
    }

    public bool RequestStartPlacement(RailType railType)
    {
        var slot = GetLocalRailBuildViewModel().GetSlot(railType);
        if (slot.OwnedCount <= 0)
        {
            Debug.LogWarning($"[NetworkRailService] 보유한 {railType} 선로가 없습니다.");
            return false;
        }

        OnRequestPlaceMode?.Invoke(railType);
        return true;
    }

    public void ConsumeRailOnPlaced(RailType railType)
    {
        var slot = GetLocalRailBuildViewModel().GetSlot(railType);
        if (slot.OwnedCount <= 0)
        {
            return;
        }

        slot.OwnedCount -= 1;
        _sessionInstalledCount += 1;
    }

    public void ReturnRailToInventory(RailType railType)
    {
        var slot = GetLocalRailBuildViewModel().GetSlot(railType);
        slot.OwnedCount += 1;
    }

    public void ResetRun()
    {
        _localRailBuildViewModel = new RailBuildViewModel(GetBonusBaseRailCount());
        _sessionCraftedCount = 0;
        _sessionInstalledCount = 0;
    }

    private void TryAutoCraftStraight()
    {
        var slot = GetLocalRailBuildViewModel().GetSlot(RailType.Straight);
        if (slot.CraftQueueCount > 0)
        {
            return;
        }

        if (NetworkResourceService.Instance == null)
        {
            return;
        }

        int currentWood = NetworkResourceService.Instance.GetLocalResourceViewModel().CurrentWood;
        if (currentWood < CRAFT_WOOD_COST)
        {
            return;
        }

        RequestCraft(RailType.Straight);
    }
}
