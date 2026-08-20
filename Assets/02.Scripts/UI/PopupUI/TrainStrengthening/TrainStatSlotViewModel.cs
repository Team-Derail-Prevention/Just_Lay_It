using UnityEngine;

public class TrainStatSlotViewModel : ViewModelBase
{
    public string SlotDataId { get; private set; }
    public TrainStatCategory Category { get; private set; }
    public string DisplayName { get; private set; }
    public string IconPath { get; private set; }
    public int MaxLevel { get; private set; }

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

    public TrainStatSlotViewModel(string slotDataId, TrainStatCategory category, string displayName, string iconPath, int maxLevel, int baseCost, int costIncreasePerLevel)
    {
        SlotDataId = slotDataId;
        Category = category;
        DisplayName = displayName;
        IconPath = iconPath;
        MaxLevel = maxLevel;
        _baseCost = baseCost;
        _costIncreasePerLevel = costIncreasePerLevel;

        RecalculateNextCost();
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
