using UnityEngine;
using System;

public class NetworkRailService : SingletonBase<NetworkRailService>
{
    // 임시 선로 제작 소요 시간 3초
    private const float CRAFT_DURATION = 1.5f;
    private float _craftSpeedPercent = 0f;
    private const int CRAFT_WOOD_COST = 2;

    private const int BONUS_RAIL_COUNT_PER_LEVEL = 2;
    private int _bonusBaseRailCount = 0;

    private RailBuildViewModel _localRailBuildViewModel;
    private int _sessionCraftedCount;
    private int _sessionInstalledCount;

    public int SessionCraftedCount => _sessionCraftedCount;
    public int SessionInstalledCount => _sessionInstalledCount;

    public event Action<RailType> OnRequestPlaceMode;

    private void OnEnable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnLobbyUpgraded += OnLobbyUpgraded;
        }
    }

    private void OnDisable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnLobbyUpgraded -= OnLobbyUpgraded;
        }
    }

    private void OnLobbyUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId == "LOBBY_RAIL_CRAFT_SPEED")
        {
            _craftSpeedPercent += 0.1f;
        }
        else if (slotDataId == "LOBBY_BASE_RAIL_COUNT")
        {
            _bonusBaseRailCount += BONUS_RAIL_COUNT_PER_LEVEL;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        TickCraftProgress(RailType.Straight);
        TickCraftProgress(RailType.Corner);
    }

    public RailBuildViewModel GetLocalRailBuildViewModel()
    {
        if (_localRailBuildViewModel == null)
        {
            _localRailBuildViewModel = new RailBuildViewModel(_bonusBaseRailCount);
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

        float actualDuration = CRAFT_DURATION * (1f - _craftSpeedPercent);
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
        _localRailBuildViewModel = new RailBuildViewModel(_bonusBaseRailCount);
        _sessionCraftedCount = 0;
        _sessionInstalledCount = 0;
    }
}
