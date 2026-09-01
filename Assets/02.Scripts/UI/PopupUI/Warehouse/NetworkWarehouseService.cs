using UnityEngine;
using System;
using Enums;

public class NetworkWarehouseService : SingletonBase<NetworkWarehouseService>
{
    private WarehouseViewModel _localVm;

    public int TotalStoredResource
    {
        get
        {
            var vm = GetLocalWarehouseViewModel();
            return vm.CurrentWood + vm.CurrentStone;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (MaterialTransferEventHub.Instance != null)
        {
            MaterialTransferEventHub.Instance.OnRequestPutIntoWarehouse += OnRequestPutIntoWarehouse;
            MaterialTransferEventHub.Instance.OnRequestTakeFromWarehouse += OnRequestTakeFromWarehouse;
        }
    }

    private void OnDestroy()
    {
        if (MaterialTransferEventHub.Instance != null)
        {
            MaterialTransferEventHub.Instance.OnRequestPutIntoWarehouse -= OnRequestPutIntoWarehouse;
            MaterialTransferEventHub.Instance.OnRequestTakeFromWarehouse -= OnRequestTakeFromWarehouse;
        }
    }

    public WarehouseViewModel GetLocalWarehouseViewModel()
    {
        if (_localVm == null)
        {
            _localVm = new WarehouseViewModel();
        }

        return _localVm;
    }

    public void AddStoneDirect(int amount)
    {
        if (amount <= 0) return;
        var vm = GetLocalWarehouseViewModel();
        vm.CurrentStone += amount;
        NotifyWarehouseStone(vm.CurrentStone);
    }

    public void AddWoodDirect(int amount)
    {
        if (amount <= 0) return;
        var vm = GetLocalWarehouseViewModel();
        vm.CurrentWood += amount;
        NotifyWarehouseWood(vm.CurrentWood);
    }

    private void OnRequestPutIntoWarehouse(MaterialObejctType materialType, int amount, Action<bool> onResult)
    {
        if (NetworkResourceService.Instance == null)
        {
            onResult?.Invoke(false);
            return;
        }

        bool isSpent;
        if (materialType == MaterialObejctType.Rock)
        {
            isSpent = NetworkResourceService.Instance.TrySpendStone(amount);
        }
        else
        {
            isSpent = NetworkResourceService.Instance.TrySpendWood(amount);
        }

        if (isSpent == false)
        {
            onResult?.Invoke(false);
            return;
        }

        var vm = GetLocalWarehouseViewModel();

        if (materialType == MaterialObejctType.Rock)
        {
            vm.CurrentStone += amount;
            NotifyWarehouseStone(vm.CurrentStone);
        }
        else
        {
            vm.CurrentWood += amount;
            NotifyWarehouseWood(vm.CurrentWood);
        }

        onResult?.Invoke(true);
    }

    private void OnRequestTakeFromWarehouse(MaterialObejctType materialType, int amount, Action<bool> onResult)
    {
        var vm = GetLocalWarehouseViewModel();

        bool hasEnough;
        if (materialType == MaterialObejctType.Rock)
        {
            hasEnough = vm.CurrentStone >= amount;
        }
        else
        {
            hasEnough = vm.CurrentWood >= amount;
        }

        if (hasEnough == false || NetworkResourceService.Instance == null)
        {
            onResult?.Invoke(false);
            return;
        }

        if (materialType == MaterialObejctType.Rock)
        {
            vm.CurrentStone -= amount;
            NetworkResourceService.Instance.AddStone(amount);
            NotifyWarehouseStone(vm.CurrentStone);
        }
        else
        {
            vm.CurrentWood -= amount;
            NetworkResourceService.Instance.AddWood(amount);
            NotifyWarehouseWood(vm.CurrentWood);
        }

        onResult?.Invoke(true);
    }

    private void NotifyWarehouseWood(int curWoodCount)
    {
        if (MaterialTransferEventHub.Instance != null)
        {
            MaterialTransferEventHub.Instance.NotifyWarehouseWoodChanged(curWoodCount);
        }
    }

    private void NotifyWarehouseStone(int curStoneCount)
    {
        if (MaterialTransferEventHub.Instance != null)
        {
            MaterialTransferEventHub.Instance.NotifyWarehouseStoneChanged(curStoneCount);
        }
    }

    public void ResetRun()
    {
        _localVm = new WarehouseViewModel();
        NotifyWarehouseWood(0);
        NotifyWarehouseStone(0);
    }
}
