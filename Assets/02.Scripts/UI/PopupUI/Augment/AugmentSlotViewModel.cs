using UnityEngine;

public class AugmentSlotViewModel : ViewModelBase
{
    public long AugmentUniqueId { get; set; }
    public string AugmentDataId { get; set; }

    private string _displayName;
    public string DisplayName
    {
        get
        {
            return _displayName;
        }
        set
        {
            _displayName = value;
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    private string _gradeName;
    public string GradeName
    {
        get
        {
            return _gradeName;
        }
        set
        {
            _gradeName = value;
            OnPropertyChanged(nameof(GradeName));
        }
    }

    private string _iconPath;
    public string IconPath
    {
        get
        {
            return _iconPath;
        }
        set
        {
            _iconPath = value;
            OnPropertyChanged(nameof(IconPath));
        }
    }

    private string _description;
    public string Description
    {
        get
        {
            return _description;
        }
        set
        {
            _description = value;
            OnPropertyChanged(nameof(Description));
        }
    }

    public int Price { get; private set; }

    private bool _isStatReady;
    public bool IsStatReady
    {
        get
        {
            return _isStatReady;
        }
        private set
        {
            _isStatReady = value;
            OnPropertyChanged(nameof(IsStatReady));
        }
    }

    private int _atk;
    public int Atk
    {
        get
        {
            return _atk;
        }
        private set
        {
            _atk = value;
            OnPropertyChanged(nameof(Atk));
        }
    }

    private float _range;
    public float Range
    {
        get
        {
            return _range;
        }
        private set
        {
            _range = value;
            OnPropertyChanged(nameof(Range));
        }
    }

    private float _fireRate;
    public float FireRate
    {
        get
        {
            return _fireRate;
        }
        private set
        {
            _fireRate = value;
            OnPropertyChanged(nameof(FireRate));
        }
    }

    private float _reloadTime;
    public float ReloadTime
    {
        get
        {
            return _reloadTime;
        }
        private set
        {
            _reloadTime = value;
            OnPropertyChanged(nameof(ReloadTime));
        }
    }

    private int _magazineSize;
    public int MagazineSize
    {
        get
        {
            return _magazineSize;
        }
        private set
        {
            _magazineSize = value;
            OnPropertyChanged(nameof(MagazineSize));
        }
    }

    public void FillFromData(WeaponData data)
    {
        if (data == null)
        {
            return;
        }

        DisplayName = data.WeaponName;
        GradeName = data.GradeName;
        IconPath = data.IconPath;
        Description = data.Description;
        Price = data.Price;
    }

    public void SetStats(WeaponCurrentStats stats)
    {
        Atk = stats.Atk;
        Range = stats.Range;
        FireRate = stats.FireRate;
        ReloadTime = stats.ReloadTime;
        MagazineSize = stats.MagazineSize;
        IsStatReady = true;
    }
}
