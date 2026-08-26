using UnityEngine;
using System;

public class ResourceStatusEventHub : SingletonBase<ResourceStatusEventHub>
{
    public event Action<int> OnWoodChanged;
    public event Action<int> OnStoneChanged;
    public event Action<int> OnRescuedHumanChanged;
    public event Action<int> OnMoneyChanged;
    public event Action<int, int> OnStationProgressChanged;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyWoodChanged(int curWoodCount)
    {
        OnWoodChanged?.Invoke(curWoodCount);
    }

    public void NotifyStoneChanged(int curStoneCount)
    {
        OnStoneChanged?.Invoke(curStoneCount);
    }

    public void NotifyRescuedHumanChanged(int curRescuedCount)
    {
        OnRescuedHumanChanged?.Invoke(curRescuedCount);
    }

    public void NotifyMoneyChanged(int curMoneyCount)
    {
        OnMoneyChanged?.Invoke(curMoneyCount);
    }

    public void NotifyStationProgressChanged(int completedStationCount, int requiredStationCount)
    {
        OnStationProgressChanged?.Invoke(completedStationCount, requiredStationCount);
    }
}
