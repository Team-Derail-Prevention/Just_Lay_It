using System.Collections.Generic;
using UnityEngine;

public class RailBuildViewModel : ViewModelBase
{
    private readonly Dictionary<ERailType, RailSlotViewModel> _slotDic = new Dictionary<ERailType, RailSlotViewModel>();

    public RailBuildViewModel()
    {
        _slotDic.Add(ERailType.Straight, new RailSlotViewModel { RailType = ERailType.Straight });
        _slotDic.Add(ERailType.Curve, new RailSlotViewModel { RailType = ERailType.Curve });
    }

    public RailSlotViewModel GetSlot(ERailType railType)
    {
        _slotDic.TryGetValue(railType, out var slotViewModel);
        return slotViewModel;
    }
}
