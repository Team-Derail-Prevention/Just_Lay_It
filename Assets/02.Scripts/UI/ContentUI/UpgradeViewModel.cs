using UnityEngine;
using System.Collections.Generic;

public class UpgradeViewModel : ViewModelBase
{
    private readonly Dictionary<string, UpgradeSlotViewModel> _slotDic = new Dictionary<string, UpgradeSlotViewModel>();
    
    public IReadOnlyDictionary<string, UpgradeSlotViewModel> SlotDic
    {
        get
        {
            return _slotDic;
        }
    }

    private int _currentCash;
    public int CurrentCash
    {
        get
        {
            return _currentCash;
        }
        set
        {
            if (_currentCash != value)
            {
                _currentCash = value;
                OnPropertyChanged(nameof(CurrentCash));
            }
        }
    }

    public void AddSlot(UpgradeSlotViewModel slotViewModel)
    {
        if (_slotDic.ContainsKey(slotViewModel.SlotDataId) == true)
        {
            return;
        }

        _slotDic.Add(slotViewModel.SlotDataId, slotViewModel);
        OnPropertyChanged("SlotListAdded");
    }

    public UpgradeSlotViewModel GetSlot(string slotDataId)
    {
        _slotDic.TryGetValue(slotDataId, out var slotViewModel);
        return slotViewModel;
    }

    public bool TrySpendCash(int amount)
    {
        if (CurrentCash < amount)
        {
            return false;
        }

        CurrentCash -= amount;
        return true;
    }

    public void GainCash(int amount)
    {
        CurrentCash += amount;
    }
}
