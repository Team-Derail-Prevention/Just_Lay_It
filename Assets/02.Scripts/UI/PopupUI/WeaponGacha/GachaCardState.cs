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

    public void FillFromData(WeaponData data)
    {
        if (data == null)
        {
            return;
        }

        WeaponDataId = data.Id;
        DisplayName = data.WeaponName;
        GradeName = data.GradeName;
        IconPath = data.IconPath;
        Dps = data.Atk;
        Description = data.Description;

        var statList = new List<string>();
        statList.Add($"{data.Atk}");
        statList.Add($"{data.Range}");
        statList.Add($"{data.FireRate}");
        statList.Add($"{data.ReloadTime}");
        statList.Add($"{data.MagazineSize}");

        StatTextList = statList;
        IsSelected = false;
    } 
}
