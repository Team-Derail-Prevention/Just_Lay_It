using UnityEngine;
using Enums;

public class NetworkResourceService : SingletonBase<NetworkResourceService>
{
    private const int BASE_CARGO_LIMIT = 400;

    private ResourceViewModel _localVm;
    private int _sessionTotalWoodCollected;
    private int _sessionTotalStoneCollected;
    private bool _isBaseRepairFreeUsed;
    private bool _isStationRepairFreeUsed;

    public bool IsBaseRepairFreeAvailable
    {
        get
        {
            return _isBaseRepairFreeUsed == false;
        }
    }

    public bool IsStationRepairFreeAvailable
    {
        get
        {
            return _isStationRepairFreeUsed == false;
        }
    }

    public int SessionTotalWoodCollected => _sessionTotalWoodCollected;
    public int SessionTotalStoneCollected => _sessionTotalStoneCollected;

    private void OnEnable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnLobbyUpgraded += OnLobbyUpgraded;
            UpgradeEventHub.Instance.OnInGameUpgraded += OnInGameUpgraded;
        }
    }

    private void OnDisable()
    {
        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnLobbyUpgraded -= OnLobbyUpgraded;
            UpgradeEventHub.Instance.OnInGameUpgraded -= OnInGameUpgraded;
        }
    }

    private void OnLobbyUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId == "LOBBY_BASE_CARGO_LIMIT" && ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyCargoLimitChanged(CargoLimit);
        }
    }

    private void OnInGameUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId == "CARGO_RESOURCE_LIMIT" && ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyCargoLimitChanged(CargoLimit);
        }
    }

    public int CargoLimit
    {
        get
        {
            return BASE_CARGO_LIMIT + GetLobbyCargoLimitBonus() + GetInGameCargoLimitBonus();
        }
    }

    public int CurrentCargoLoad
    {
        get
        {
            var vm = GetLocalResourceViewModel();
            return vm.CurrentStone;
        }
    }

    public int RemainingCargoCapacity
    {
        get
        {
            int remaining = CargoLimit - CurrentCargoLoad;
            return Mathf.Max(0, remaining);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        MaterialObject.OnMaterialObjectCollected += OnMaterialObjectCollected;

        if (MoneyRequestEventHub.Instance != null)
        {
            MoneyRequestEventHub.Instance.OnRequestSpendMoney += OnRequestSpendMoney;
        }
    }

    private void OnDestroy()
    {
        MaterialObject.OnMaterialObjectCollected -= OnMaterialObjectCollected;

        if (MoneyRequestEventHub.Instance != null)
        {
            MoneyRequestEventHub.Instance.OnRequestSpendMoney -= OnRequestSpendMoney;
        }
    }

    public ResourceViewModel GetLocalResourceViewModel()
    {
        if (_localVm == null)
        {
            _localVm = new ResourceViewModel();
        }

        return _localVm;
    }

    private void OnMaterialObjectCollected(MaterialObjectData data)
    {
        if (data == null)
        {
            return;
        }

        bool isParsed = System.Enum.TryParse(data.Type, out MaterialObejctType materialType);
        if (isParsed == false)
        {
            Debug.LogWarning($"[NetworkResourceService] 알 수 없는 자원 타입입니다 : {data.Type}");
            return;
        }

        if (materialType == MaterialObejctType.Rock)
        {
            AddStone(data.amount);
        }
        else if (materialType == MaterialObejctType.DeadTree)
        {
            AddWood(data.amount);
        }
    }

    public int AddWood(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var vm = GetLocalResourceViewModel();
        vm.CurrentWood += amount;
        _sessionTotalWoodCollected += amount;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyWoodChanged(vm.CurrentWood);
        }

        return amount;
    }

    public int AddStone(int amount)
    {
        int addable = Mathf.Min(amount, RemainingCargoCapacity);
        if (addable <= 0)
        {
            Debug.LogWarning("[NetworkResourceService] 적재 한도가 가득 찼습니다.");
            return 0;
        }

        var vm = GetLocalResourceViewModel();
        vm.CurrentStone += addable;
        _sessionTotalStoneCollected += addable;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyStoneChanged(vm.CurrentStone);
        }

        return addable;
    }

    public int AddStoneWithWarehouseOverflow(int amount)
    {
        int addable = Mathf.Min(amount, RemainingCargoCapacity);
        int overflow = amount - addable;

        if (addable > 0)
        {
            var vm = GetLocalResourceViewModel();
            vm.CurrentStone += addable;
            _sessionTotalStoneCollected += addable;

            if (ResourceStatusEventHub.Instance != null)
            {
                ResourceStatusEventHub.Instance.NotifyStoneChanged(vm.CurrentStone);
            }
        }

        if (overflow > 0)
        {
            if (NetworkWarehouseService.Instance != null)
            {
                NetworkWarehouseService.Instance.AddStoneDirect(overflow);
            }
            else
            {
                Debug.LogWarning($"[NetworkResourceService] 적재 한도 초과분({overflow})을 보관할 창고를 찾지 못했습니다.");
            }
        }

        return addable + overflow;
    }

    public bool TrySpendWood(int amount)
    {
        var vm = GetLocalResourceViewModel();

        if (vm.CurrentWood < amount)
        {
            return false;
        }

        vm.CurrentWood -= amount;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyWoodChanged(vm.CurrentWood);
        }

        return true;
    }

    public bool HasEnoughStone(int amount)
    {
        var vm = GetLocalResourceViewModel();
        return vm.CurrentStone >= amount;
    }

    public bool TrySpendStoneForBaseRepair(int cost)
    {
        if (_isBaseRepairFreeUsed == false)
        {
            _isBaseRepairFreeUsed = true;
            return true;
        }

        return TrySpendStone(cost);
    }

    public bool TrySpendStoneForStationRepair(int cost)
    {
        if (_isStationRepairFreeUsed == false)
        {
            _isStationRepairFreeUsed = true;
            return true;
        }

        return TrySpendStone(cost);
    }

    public bool TrySpendStone(int amount)
    {
        var vm = GetLocalResourceViewModel();

        if (vm.CurrentStone < amount)
        {
            return false;
        }

        vm.CurrentStone -= amount;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyStoneChanged(vm.CurrentStone);
        }

        return true;
    }

    public void AddRescuedHuman(int amount)
    {
        var vm = GetLocalResourceViewModel();
        vm.RescuedHumanCount += amount;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyRescuedHumanChanged(vm.RescuedHumanCount);
        }
    }

    private int GetLobbyCargoLimitBonus()
    {
        if (NetworkUpgradeService.Instance == null)
        {
            return 0;
        }

        UpgradeViewModel upgradeVm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
        UpgradeSlotViewModel slotVm = upgradeVm?.GetSlot("LOBBY_BASE_CARGO_LIMIT");
        int level = slotVm != null ? slotVm.CurrentLevel : 0;

        return level * 100;
    }

    private int GetInGameCargoLimitBonus()
    {
        if (NetworkTrainStrengtheningService.Instance == null)
        {
            return 0;
        }

        TrainStrengtheningViewModel strengtheningVm = NetworkTrainStrengtheningService.Instance.GetLocalTrainStrengtheningViewModel();
        TrainStatSlotViewModel slotVm = strengtheningVm?.GetSlot("CARGO_RESOURCE_LIMIT");
        int level = slotVm != null ? slotVm.CurrentLevel : 0;

        return level * 100;
    }

    private void OnRequestSpendMoney(int amount, System.Action<bool> onResult)
    {
        bool isSpent = TrySpendStone(amount);
        onResult?.Invoke(isSpent);
    }

    public void ResetRun()
    {
        _localVm = new ResourceViewModel();
        _sessionTotalWoodCollected = 0;
        _sessionTotalStoneCollected = 0;
        _isBaseRepairFreeUsed = false;
        _isStationRepairFreeUsed = false;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyWoodChanged(_localVm.CurrentWood);
            ResourceStatusEventHub.Instance.NotifyStoneChanged(_localVm.CurrentStone);
            ResourceStatusEventHub.Instance.NotifyRescuedHumanChanged(0);
        }
    }

}
