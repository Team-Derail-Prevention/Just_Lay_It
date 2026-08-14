using UnityEngine;

public class RailSlotViewModel : ViewModelBase
{
    public RailType RailType { get; set; }

    private int _ownedCount;
    public int OwnedCount
    {
        get
        {
            return _ownedCount;
        }
        set
        {
            if (_ownedCount != value)
            {
                _ownedCount = value;
                OnPropertyChanged(nameof(OwnedCount));
            }
        }
    }

    private int _craftQueueCount;
    public int CraftQueueCount
    {
        get
        {
            return _craftQueueCount;
        }
        set
        {
            if (_craftQueueCount != value)
            {
                _craftQueueCount = value;
                OnPropertyChanged(nameof(CraftQueueCount));
            }
        }
    }

    private float _craftProgress01;
    public float CraftProgress01
    {
        get
        {
            return _craftProgress01;
        }
        set
        {
            if (_craftProgress01 != value)
            {
                _craftProgress01 = value;
                OnPropertyChanged(nameof(CraftProgress01));
            }
        }
    }
}
