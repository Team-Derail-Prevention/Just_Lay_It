using UnityEngine;

public class RailBuildUI : UIBase
{
    [Header("제작 슬롯")]
    [SerializeField] private RailCraftSlotUI CraftSlot_Straight;
    [SerializeField] private RailCraftSlotUI CraftSlot_Curve;

    [Header("설치 슬롯")]
    [SerializeField] private RailPlaceSlotUI PlaceSlot_Straight;
    [SerializeField] private RailPlaceSlotUI PlaceSlot_Curve;

    private RailBuildViewModel _vm;

    private void OnEnable()
    {
        _vm = NetworkRailService.Instance.GetLocalRailBuildViewModel();

        if (CraftSlot_Straight != null)
        {
            CraftSlot_Straight.InitSlot(_vm.GetSlot(ERailType.Straight), OnClick_Craft);
        }

        if (CraftSlot_Curve != null)
        {
            CraftSlot_Curve.InitSlot(_vm.GetSlot(ERailType.Curve), OnClick_Craft);
        }

        if (PlaceSlot_Straight != null)
        {
            PlaceSlot_Straight.InitSlot(_vm.GetSlot(ERailType.Straight), OnClick_Place);
        }

        if (PlaceSlot_Curve != null)
        {
            PlaceSlot_Curve.InitSlot(_vm.GetSlot(ERailType.Curve), OnClick_Place);
        }
    }

    private void OnClick_Craft(ERailType railType)
    {
        NetworkRailService.Instance.RequestCraft(railType);
    }

    private void OnClick_Place(ERailType railType)
    {
        NetworkRailService.Instance.RequestStartPlacement(railType);
    }
}
