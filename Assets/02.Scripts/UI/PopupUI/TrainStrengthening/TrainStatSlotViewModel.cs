using UnityEngine;
using Enums;

public class TrainStatSlotViewModel : ViewModelBase
{
    public string SlotDataId { get; private set; }
    public TrainStatCategory Category { get; private set; }
    public string IconPath { get; private set; }
    public int MaxLevel { get; private set; }

    private readonly int _baseCost;
    private readonly int _costIncreasePerLevel;
    private int _currentLevel;
    private readonly string _displayNameKo;
    private readonly string _displayNameEn;
    private readonly int _nameFontSizeKo;
    private readonly int _nameFontSizeEn;

    public int CurrentLevel
    {
        get
        {
            return _currentLevel;
        }
        private set
        {
            if (_currentLevel != value)
            {
                _currentLevel = value;
                OnPropertyChanged(nameof(CurrentLevel));
            }
        }
    }

    private int _nextCost;

    public int NextCost
    {
        get
        {
            return _nextCost;
        }
        private set
        {
            if (_nextCost != value)
            {
                _nextCost = value;
                OnPropertyChanged(nameof(NextCost));
            }
        }
    }

    public bool IsMaxLevel
    {
        get
        {
            return CurrentLevel >= MaxLevel;
        }
    }

    public int CurrentLevelCost
    {
        get
        {
            if (CurrentLevel <= 0)
            {
                return 0;
            }

            return _baseCost + ((CurrentLevel - 1) * _costIncreasePerLevel);
        }
    }

    public string DisplayName
    {
        get
        {
            return LocalizationManager.Instance.CurrentLanguage == LanguageType.Korean ? _displayNameKo : _displayNameEn;
        }
    }

    public int NameFontSize
    {
        get
        {
            return LocalizationManager.Instance.CurrentLanguage == LanguageType.Korean ? _nameFontSizeKo : _nameFontSizeEn;
        }
    }

    public TrainStatSlotViewModel(string slotDataId, TrainStatCategory category, string displayNameKo, string displayNameEn, int nameFontSizeKo, int nameFontSizeEn, string iconPath, int maxLevel, int baseCost, int costIncreasePerLevel)
    {
        SlotDataId = slotDataId;
        Category = category;
        _displayNameKo = displayNameKo;
        _displayNameEn = displayNameEn;
        _nameFontSizeKo = nameFontSizeKo;
        _nameFontSizeEn = nameFontSizeEn;
        IconPath = iconPath;
        MaxLevel = maxLevel;
        _baseCost = baseCost;
        _costIncreasePerLevel = costIncreasePerLevel;

        LocalizationEventHub.Instance.OnLanguageChanged += OnLanguageChanged_LocalizationEventHub;

        RecalculateNextCost();
    }

    private void OnLanguageChanged_LocalizationEventHub(LanguageType language)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(NameFontSize));
    }

    public void LevelUp()
    {
        if (IsMaxLevel == true)
        {
            return;
        }

        CurrentLevel++;
        RecalculateNextCost();
    }

    private void RecalculateNextCost()
    {
        NextCost = _baseCost + (CurrentLevel * _costIncreasePerLevel);
    }
}
