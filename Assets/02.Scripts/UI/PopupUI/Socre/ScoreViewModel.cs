using UnityEngine;

public class ScoreViewModel : ViewModelBase
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

    private int _collectedResourceCount;
    public int CollectedResourceCount
    {
        get
        {
            return _collectedResourceCount;
        }
        set
        {
            if (_collectedResourceCount != value)
            {
                _collectedResourceCount = value;
                OnPropertyChanged(nameof(CollectedResourceCount));
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

    private ScoreResultType _resultType;
    public ScoreResultType ResultType
    {
        get
        {
            return _resultType;
        }
        set
        {
            if (_resultType != value)
            {
                _resultType = value;
                OnPropertyChanged(nameof(ResultType));
            }
        }
    }
}
