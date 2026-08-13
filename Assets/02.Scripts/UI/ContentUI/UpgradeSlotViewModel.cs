using UnityEngine;

public class UpgradeSlotViewModel : ViewModelBase
{
    public string SlotDataId { get; set; }

    public int MaxLevel { get; set; } = 4;

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

    public UpgradeSlotViewModel()
    {
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
        NextCost = (CurrentLevel + 1) * 100;
    }
}
