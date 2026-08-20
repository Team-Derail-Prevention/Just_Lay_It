using UnityEngine;

public class ResourceViewModel : ViewModelBase
{
    private int _currentWood;
    public int CurrentWood
    {
        get
        {
            return _currentWood;
        }
        set
        {
            if (_currentWood != value)
            {
                _currentWood = value;
                OnPropertyChanged(nameof(CurrentWood));
            }
        }
    }

    private int _currentStone;
    public int CurrentStone
    {
        get
        {
            return _currentStone;
        }
        set
        {
            if (_currentStone != value)
            {
                _currentStone = value;
                OnPropertyChanged(nameof(CurrentStone));
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
}
