using UnityEngine;

public class UpgradeSlotViewModel : ViewModelBase
{
    public string SlotDataId { get; set; }
    public string DisplayName { get; private set; }
    public string IconPath { get; private set; }
    public string Description { get; private set; }
    public int MaxLevel { get; set; } = 4;

    private readonly int _baseCost;
    private readonly int _costIncreasePerLevel;

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

    public UpgradeSlotViewModel(string slotDataId, string displayName, string iconPath, string description, int maxLevel, int baseCost, int costIncreasePerLevel)
    {
        SlotDataId = slotDataId;
        DisplayName = displayName;
        IconPath = iconPath;
        Description = description;
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
