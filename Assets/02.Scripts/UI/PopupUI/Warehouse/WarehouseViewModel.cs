using UnityEngine;

public class WarehouseViewModel : ViewModelBase
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
}
