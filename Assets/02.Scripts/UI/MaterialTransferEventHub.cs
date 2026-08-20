using UnityEngine;
using System;
using Enums;

public class MaterialTransferEventHub : SingletonBase<MaterialTransferEventHub>
{
    public event Action<int> OnWarehouseStoneChanged;
    public event Action<int> OnWarehouseWoodChanged;

    public event Action<MaterialObejct, int, Action<bool>> OnRequestPutIntoWarehouse;
    public event Action<MaterialObejct, int, Action<bool>> OnRequestTakeFromWarehouse;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyWarehouseStoneChanged(int curStoneCount)
    {
        OnWarehouseStoneChanged?.Invoke(curStoneCount);
    }

    public void NotifyWarehouseWoodChanged(int curWoodCount)
    {
        OnWarehouseWoodChanged?.Invoke(curWoodCount);
    }

    public void RequestPutIntoWarehouse(MaterialObejct materialType, int amount, Action<bool> onResult)
    {
        if (OnRequestPutIntoWarehouse == null)
        {
            Debug.LogWarning("[MaterialTransferEventHub] 구독하는 외부 시스템이 없습니다.");
            onResult?.Invoke(false);
            return;
        }

        OnRequestPutIntoWarehouse.Invoke(materialType, amount, onResult);
    }

    public void RequestTakeFromWarehouse(MaterialObejct materialType, int amount, Action<bool> onResult)
    {
        if (OnRequestTakeFromWarehouse == null)
        {
            Debug.LogWarning("[MaterialTransferEventHub] 구독하는 외부 시스템이 없습니다.");
            onResult?.Invoke(false);
            return;
        }

        OnRequestTakeFromWarehouse.Invoke(materialType, amount, onResult);
    }
}
