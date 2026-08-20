using System.Collections.Generic;
using UnityEngine;

public class TrainStrengtheningViewModel : ViewModelBase
{
    private readonly Dictionary<string, TrainStatSlotViewModel> _slotDic = new Dictionary<string, TrainStatSlotViewModel>();

    public IReadOnlyDictionary<string, TrainStatSlotViewModel> SlotDic
    {
        get
        {
            return _slotDic;
        }
    }

    public void AddSlot(TrainStatSlotViewModel slotVm)
    {
        if (_slotDic.ContainsKey(slotVm.SlotDataId) == true)
        {
            return;
        }

        _slotDic.Add(slotVm.SlotDataId, slotVm);
        OnPropertyChanged("SlotListAdded");
    }

    public TrainStatSlotViewModel GetSlot(string slotDataId)
    {
        _slotDic.TryGetValue(slotDataId, out var slotVm);
        return slotVm;
    }

    public List<TrainStatSlotViewModel> GetSlotsByCategory(TrainStatCategory category)
    {
        List<TrainStatSlotViewModel> result = new List<TrainStatSlotViewModel>();

        foreach (var slotKv in _slotDic)
        {
            if (slotKv.Value.Category == category)
            {
                result.Add(slotKv.Value);
            }
        }

        return result;
    }
}
