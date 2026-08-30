using UnityEngine;
using Enums;

public class NetworkResourceService : SingletonBase<NetworkResourceService>
{
    private const int BASE_CARGO_LIMIT = 300;

    private ResourceViewModel _localVm;
    private int _cargoLimit = BASE_CARGO_LIMIT;
    private int _sessionTotalWoodCollected;
    private int _sessionTotalStoneCollected;

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
        if (slotDataId == "LOBBY_BASE_CARGO_LIMIT")
        {
            IncreaseCargoLimit(100);
        }
    }

    private void OnInGameUpgraded(string slotDataId, int newLevel)
    {
        if (slotDataId == "CARGO_RESOURCE_LIMIT")
        {
            IncreaseCargoLimit(100);
        }
    }

    public int CargoLimit
    {
        get
        {
            return _cargoLimit;
        }
    }

    public int CurrentCargoLoad
    {
        get
        {
            var vm = GetLocalResourceViewModel();
            return vm.CurrentWood + vm.CurrentStone;
        }
    }

    public int RemainingCargoCapacity
    {
        get
        {
            int remaining = _cargoLimit - CurrentCargoLoad;
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
        int addable = Mathf.Min(amount, RemainingCargoCapacity);
        if (addable <= 0)
        {
            Debug.LogWarning("[NetworkResourceService] 적재 한도가 가득 찼습니다.");
            return 0;
        }

        var vm = GetLocalResourceViewModel();
        vm.CurrentWood += addable;
        _sessionTotalWoodCollected += addable;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyWoodChanged(vm.CurrentWood);
        }

        return addable;
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

    public void IncreaseCargoLimit(int amount)
    {
        _cargoLimit += amount;
    }

    private void OnRequestSpendMoney(int amount, System.Action<bool> onResult)
    {
        bool isSpent = TrySpendStone(amount);
        onResult?.Invoke(isSpent);
    }

    public void ResetRun()
    {
        _localVm = new ResourceViewModel();
        _cargoLimit = BASE_CARGO_LIMIT;
        _sessionTotalWoodCollected = 0;
        _sessionTotalStoneCollected = 0;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyWoodChanged(_localVm.CurrentWood);
            ResourceStatusEventHub.Instance.NotifyStoneChanged(_localVm.CurrentStone);
            ResourceStatusEventHub.Instance.NotifyRescuedHumanChanged(0);
        }
    }

}
