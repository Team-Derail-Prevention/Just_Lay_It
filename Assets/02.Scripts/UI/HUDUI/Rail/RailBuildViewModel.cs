using System.Collections.Generic;
using UnityEngine;

public class RailBuildViewModel : ViewModelBase
{
    private readonly Dictionary<RailType, RailSlotViewModel> _slotDic = new Dictionary<RailType, RailSlotViewModel>();

    public RailBuildViewModel()
    {
        _slotDic.Add(RailType.Straight, new RailSlotViewModel { RailType = RailType.Straight });
        _slotDic.Add(RailType.Corner, new RailSlotViewModel { RailType = RailType.Corner });
    }

    public RailSlotViewModel GetSlot(RailType railType)
    {
        _slotDic.TryGetValue(railType, out var slotViewModel);
        return slotViewModel;
    }
}
