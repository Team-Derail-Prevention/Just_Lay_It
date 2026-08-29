using UnityEngine;

public class GameClearResultViewModel : ViewModelBase
{
    private float _totalDistance;
    public float TotalDistance
    {
        get
        {
            return _totalDistance;
        }
        set
        {
            if (_totalDistance != value)
            {
                _totalDistance = value;
                OnPropertyChanged(nameof(TotalDistance));
            }
        }
    }

    private float _playTimeSeconds;
    public float PlayTimeSeconds
    {
        get
        {
            return _playTimeSeconds;
        }
        set
        {
            if (_playTimeSeconds != value)
            {
                _playTimeSeconds = value;
                OnPropertyChanged(nameof(PlayTimeSeconds));
            }
        }
    }

    private int _rescuedHumanCount;
    public int RescuedHumanCount
    {
        get
        {
            return _rescuedHumanCount;
        }
        set
        {
            if (_rescuedHumanCount != value)
            {
                _rescuedHumanCount = value;
                OnPropertyChanged(nameof(RescuedHumanCount));
            }
        }
    }

    private int _collectedWoodCount;
    public int CollectedWoodCount
    {
        get
        {
            return _collectedWoodCount;
        }
        set
        {
            if (_collectedWoodCount != value)
            {
                _collectedWoodCount = value;
                OnPropertyChanged(nameof(CollectedWoodCount));
            }
        }
    }

    private int _collectedStoneCount;
    public int CollectedStoneCount
    {
        get
        {
            return _collectedStoneCount;
        }
        set
        {
            if (_collectedStoneCount != value)
            {
                _collectedStoneCount = value;
                OnPropertyChanged(nameof(CollectedStoneCount));
            }
        }
    }

    private int _killCount;
    public int KillCount
    {
        get
        {
            return _killCount;
        }
        set
        {
            if (_killCount != value)
            {
                _killCount = value;
                OnPropertyChanged(nameof(KillCount));
            }
        }
    }

    private int _railCraftedCount;
    public int RailCraftedCount
    {
        get
        {
            return _railCraftedCount;
        }
        set
        {
            if (_railCraftedCount != value)
            {
                _railCraftedCount = value;
                OnPropertyChanged(nameof(RailCraftedCount));
            }
        }
    }

    private int _railInstalledCount;
    public int RailInstalledCount
    {
        get
        {
            return _railInstalledCount;
        }
        set
        {
            if (_railInstalledCount != value)
            {
                _railInstalledCount = value;
                OnPropertyChanged(nameof(RailInstalledCount));
            }
        }
    }

    private int _earnedCashCount;
    public int EarnedCashCount
    {
        get
        {
            return _earnedCashCount;
        }
        set
        {
            if (_earnedCashCount != value)
            {
                _earnedCashCount = value;
                OnPropertyChanged(nameof(EarnedCashCount));
            }
        }
    }

    private int _totalPlayCount;
    public int TotalPlayCount
    {
        get
        {
            return _totalPlayCount;
        }
        set
        {
            if (_totalPlayCount != value)
            {
                _totalPlayCount = value;
                OnPropertyChanged(nameof(TotalPlayCount));
            }
        }
    }

    private string _titleMessage;
    public string TitleMessage
    {
        get
        {
            return _titleMessage;
        }
        set
        {
            if (_titleMessage != value)
            {
                _titleMessage = value;
                OnPropertyChanged(nameof(TitleMessage));
            }
        }
    }

    private string _nextStageNoticeMessage;
    public string NextStageNoticeMessage
    {
        get
        {
            return _nextStageNoticeMessage;
        }
        set
        {
            if (_nextStageNoticeMessage != value)
            {
                _nextStageNoticeMessage = value;
                OnPropertyChanged(nameof(NextStageNoticeMessage));
            }
        }
    }

    private bool _isFinalStage;
    public bool IsFinalStage
    {
        get
        {
            return _isFinalStage;
        }
        set
        {
            if (_isFinalStage != value)
            {
                _isFinalStage = value;
                OnPropertyChanged(nameof(IsFinalStage));
            }
        }
    }
}
