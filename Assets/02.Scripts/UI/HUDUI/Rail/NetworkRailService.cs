using UnityEngine;
using System;

public class NetworkRailService : SingletonBase<NetworkRailService>
{
    // 임시 선로 제작 소요 시간 3초
    private const float CRAFT_DURATION = 1.5f;
    private float _craftSpeedPercent = 0f;

    private RailBuildViewModel _localRailBuildViewModel;

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
            _localRailBuildViewModel = new RailBuildViewModel();
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
    }

    public bool RequestCraft(RailType railType)
    {
        // 선로 제작 필요 재료 정해지면 수정
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
    }

    public void ReturnRailToInventory(RailType railType)
    {
        var slot = GetLocalRailBuildViewModel().GetSlot(railType);
        slot.OwnedCount += 1;
    }
}
