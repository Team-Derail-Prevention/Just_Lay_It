using UnityEngine;
using System;

public class UpgradeEventHub : SingletonBase<UpgradeEventHub>
{
    public event Action<string, int> OnInGameUpgraded;  
    public event Action<string, int> OnLobbyUpgraded;   

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyInGameUpgraded(string slotDataId, int newLevel)
    {
        OnInGameUpgraded?.Invoke(slotDataId, newLevel);
    }

    public void NotifyLobbyUpgraded(string slotDataId, int newLevel)
    {
        OnLobbyUpgraded?.Invoke(slotDataId, newLevel);
    }
}
