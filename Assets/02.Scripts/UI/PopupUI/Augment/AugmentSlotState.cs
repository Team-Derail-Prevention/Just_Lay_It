using UnityEngine;

public class AugmentSlotState : ViewModelBase
{
    public int SlotIndex { get; set; }

    private bool _isLocked;
    public bool IsLocked
    {
        get
        {
            return _isLocked;
        }
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                OnPropertyChanged(nameof(IsLocked));
            }
        }
    }

    private AugmentSlotViewModel _augment;
    public AugmentSlotViewModel Augment
    {
        get
        {
            return _augment;
        }
        set
        {
            if (ReferenceEquals(_augment, value) == true)
            {
                return;
            }

            _augment = value;
            OnPropertyChanged(nameof(Augment));
        }
    }
}
