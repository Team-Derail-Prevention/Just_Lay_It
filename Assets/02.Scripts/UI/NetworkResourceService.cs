using UnityEngine;
using Enums;

public class NetworkResourceService : SingletonBase<NetworkResourceService>
{
    private ResourceViewModel _localVm;

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

        bool isParsed = System.Enum.TryParse(data.Type, out MaterialObejct materialType);
        if (isParsed == false)
        {
            Debug.LogWarning($"[NetworkResourceService] 알 수 없는 자원 타입입니다 : {data.Type}");
            return;
        }

        if (materialType == MaterialObejct.Rock)
        {
            AddStone(data.amount);
        }
        else if (materialType == MaterialObejct.DeadTree)
        {
            AddWood(data.amount);
        }
    }

    public void AddWood(int amount)
    {
        var vm = GetLocalResourceViewModel();
        vm.CurrentWood += amount;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyWoodChanged(vm.CurrentWood);
        }
    }

    public void AddStone(int amount)
    {
        var vm = GetLocalResourceViewModel();
        vm.CurrentStone += amount;

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyStoneChanged(vm.CurrentStone);
        }
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
    private void OnRequestSpendMoney(int amount, System.Action<bool> onResult)
    {
        bool isSpent = TrySpendStone(amount);
        onResult?.Invoke(isSpent);
    }

    public void ResetRun()
    {
        _localVm = new ResourceViewModel();

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.NotifyWoodChanged(0);
            ResourceStatusEventHub.Instance.NotifyStoneChanged(0);
            ResourceStatusEventHub.Instance.NotifyRescuedHumanChanged(0);
        }
    }
}
