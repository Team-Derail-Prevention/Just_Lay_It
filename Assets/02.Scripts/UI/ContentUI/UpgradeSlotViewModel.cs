using UnityEngine;
using Enums;

public class UpgradeSlotViewModel : ViewModelBase
{
    public string SlotDataId { get; set; }
    public string IconPath { get; private set; }
    public int MaxLevel { get; set; } = 4;

    private readonly string _displayNameKo;
    private readonly string _displayNameEn;
    private readonly string _descriptionKo;
    private readonly string _descriptionEn;
    private readonly int _nameFontSizeKo;
    private readonly int _nameFontSizeEn;
    private readonly int _baseCost;
    private readonly int _costIncreasePerLevel;

    public string DisplayName => LocalizationManager.Instance.CurrentLanguage == LanguageType.Korean ? _displayNameKo : _displayNameEn;
    public string Description => LocalizationManager.Instance.CurrentLanguage == LanguageType.Korean ? _descriptionKo : _descriptionEn;
    public int NameFontSize => LocalizationManager.Instance.CurrentLanguage == LanguageType.Korean ? _nameFontSizeKo : _nameFontSizeEn;

    private int _currentLevel;

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

    public UpgradeSlotViewModel(string slotDataId, string displayNameKo, string displayNameEn, string iconPath, string descriptionKo, string descriptionEn, int nameFontSizeKo, int nameFontSizeEn, int maxLevel, int baseCost, int costIncreasePerLevel)
    {
        SlotDataId = slotDataId;
        _displayNameKo = displayNameKo;
        _displayNameEn = displayNameEn;
        IconPath = iconPath;
        _descriptionKo = descriptionKo;
        _descriptionEn = descriptionEn;
        _nameFontSizeKo = nameFontSizeKo;
        _nameFontSizeEn = nameFontSizeEn;
        MaxLevel = maxLevel;
        _baseCost = baseCost;
        _costIncreasePerLevel = costIncreasePerLevel;

        RecalculateNextCost();
    }

    public void LevelUp()
    {
        if (IsMaxLevel)
        {
            return;
        }

        CurrentLevel++;
        RecalculateNextCost();
    }

    public void LevelDown()
    {
        if (CurrentLevel <= 0)
        {
            return;
        }

        CurrentLevel--;
        RecalculateNextCost();
    }

    private void RecalculateNextCost()
    {
        NextCost = _baseCost + (CurrentLevel * _costIncreasePerLevel);
    }
}
