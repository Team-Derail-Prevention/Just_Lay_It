using System;
using UnityEngine;

public class TrainStatusEventHub : SingletonBase<TrainStatusEventHub>
{
    public event Action<float, float> OnHpChanged;
    public event Action<float> OnSpeedChanged;
    public event Action<float> OnDistanceChanged;
    public event Action<float> OnPlayTimeChanged;

    public float CurrentHp { get; private set; }
    public float CurrentMaxHp { get; private set; }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyHpChanged(float curHp, float maxHp)
    {
        CurrentHp = curHp; 
        CurrentMaxHp = maxHp;

        OnHpChanged?.Invoke(curHp, maxHp);
    }

    public void NotifySpeedChanged(float curSpeedKmh)
    {
        OnSpeedChanged?.Invoke(curSpeedKmh);
    }

    public void NotifyDistanceChanged(float totalMeters)
    {
        OnDistanceChanged?.Invoke(totalMeters);
    }

    public void NotifyPlayTimeChanged(float totalSeconds)
    {
        OnPlayTimeChanged?.Invoke(totalSeconds);
    }
}
