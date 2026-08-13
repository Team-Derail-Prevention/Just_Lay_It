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

    private int _currentGold;
    public int CurrentGold
    {
        get
        {
            return _currentGold;
        }
        set
        {
            if (_currentGold != value)
            {
                _currentGold = value;
                OnPropertyChanged(nameof(CurrentGold));
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

    public bool TrySpendGold(int amount)
    {
        if (CurrentGold < amount)
        {
            return false;
        }

        CurrentGold -= amount;
        return true;
    }

    public void GainGold(int amount)
    {
        CurrentGold += amount;
    }
}
