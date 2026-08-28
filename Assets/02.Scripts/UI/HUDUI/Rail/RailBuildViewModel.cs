using System.Collections.Generic;
using UnityEngine;

public class RailBuildViewModel : ViewModelBase
{
    private readonly Dictionary<RailType, RailSlotViewModel> _slotDic = new Dictionary<RailType, RailSlotViewModel>();

    private const int INITIAL_STRAIGHT_RAIL_COUNT = 10;

    public RailBuildViewModel(int bonusStraightRailCount = 0)
    {
        int straightCount = INITIAL_STRAIGHT_RAIL_COUNT + bonusStraightRailCount;

        _slotDic.Add(RailType.Straight, new RailSlotViewModel { RailType = RailType.Straight, OwnedCount = INITIAL_STRAIGHT_RAIL_COUNT });
        _slotDic.Add(RailType.Corner, new RailSlotViewModel { RailType = RailType.Corner });
    }

    public RailSlotViewModel GetSlot(RailType railType)
    {
        _slotDic.TryGetValue(railType, out var slotViewModel);
        return slotViewModel;
    }
}
