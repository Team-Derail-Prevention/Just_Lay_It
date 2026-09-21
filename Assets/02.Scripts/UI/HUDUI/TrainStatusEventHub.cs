using System;
using UnityEngine;

public class TrainStatusEventHub : SingletonBase<TrainStatusEventHub>
{
    private const float LOW_HP_WARNING_RATIO = 0.2f;

    public event Action<float, float> OnHpChanged;
    public event Action<float> OnSpeedChanged;
    public event Action<float> OnDistanceChanged;
    public event Action<float> OnPlayTimeChanged;
    public event Action OnLowHpWarning;

    public float CurrentHp { get; private set; }
    public float CurrentMaxHp { get; private set; }

    private bool _isLowHpWarned;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyHpChanged(float curHp, float maxHp)
    {
        CurrentHp = curHp; 
        CurrentMaxHp = maxHp;

        OnHpChanged?.Invoke(curHp, maxHp);
        CheckLowHpWarning(curHp, maxHp);
    }

    private void CheckLowHpWarning(float curHp, float maxHp)
    {
        if (maxHp <= 0f)
        {
            return;
        }

        float hpRatio = curHp / maxHp;

        if (hpRatio > LOW_HP_WARNING_RATIO)
        {
            _isLowHpWarned = false;
            return;
        }

        if (_isLowHpWarned == true || curHp <= 0f)
        {
            return;
        }

        _isLowHpWarned = true;
        OnLowHpWarning?.Invoke();
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
