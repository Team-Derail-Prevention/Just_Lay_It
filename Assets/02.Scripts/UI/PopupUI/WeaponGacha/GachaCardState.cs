using UnityEngine;
using System.Collections.Generic;

public class GachaCardState : ViewModelBase
{
    public int SlotIndex { get; set; }
    public string WeaponDataId { get; private set; }

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

    private int _dps;
    public int Dps
    {
        get
        {
            return _dps;
        }
        set
        {
            _dps = value;
            OnPropertyChanged(nameof(Dps));
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

    private List<string> _statTextList = new List<string>();
    public IReadOnlyList<string> StatTextList
    {
        get
        {
            return _statTextList;
        }
        set
        {
            _statTextList = new List<string>(value);
            OnPropertyChanged(nameof(StatTextList));
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get
        {
            return _isSelected;
        }
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public void FillFromData(GunGachaData data)
    {
        WeaponDataId = data.Id;
        DisplayName = data.DisplayName;
        GradeName = data.GradeName;
        IconPath = data.IconPath;
        Dps = data.Dps;
        Description = data.Description;
        StatTextList = data.StatTextList;
        IsSelected = false;
    }
}
