using UnityEngine;
using Enums;
using System;

public class LocalizationEventHub : SingletonBase<LocalizationEventHub>
{
    public event Action<LanguageType> OnLanguageChanged;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void NotifyLanguageChanged(LanguageType language)
    {
        OnLanguageChanged?.Invoke(language);
    }
}
