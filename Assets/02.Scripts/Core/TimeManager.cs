using System;
using UnityEngine;

public class TimeManager
{
    public bool IsPaused { get { return _pauseCount > 0; } }

    public event Action OnPaused;
    public event Action OnResumed;

    private int _pauseCount;

    public void Pause()
    {
        _pauseCount++;

        if (_pauseCount > 1)
        {
            return;
        }

        Time.timeScale = 0f;
        OnPaused?.Invoke();
    }

    public void Resume()
    {
        _pauseCount = Mathf.Max(0, _pauseCount - 1);

        if (_pauseCount > 0)
        {
            return;
        }

        Time.timeScale = 1f;
        OnResumed?.Invoke();
    }

    // TODO: 일부만 멈추거나 슬로우 모션이 필요해지면 GameDeltaTime 추가.
}