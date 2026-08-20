using UnityEngine;

public class AugmentInventoryUI : UIBase
{
    [SerializeField] private GameObject Prefab_Slot;
    [SerializeField] private Transform Transform_SlotRoot;
    [SerializeField] private UIButton Button_Close;

    private AugmentInventoryViewModel _vm;
    private bool _isSlotsCreated;

    private void OnEnable()
    {
        if (Button_Close != null)
        {
            Button_Close.BindOnClickButtonEvent(OnClick_Close);
        }

        _vm = NetworkAugmentService.Instance.GetLocalAugmentInventoryViewModel();

        if (_isSlotsCreated == false)
        {
            CreateAllSlots();
            _isSlotsCreated = true;
        }
    }

    private void OnDisable()
    {
        if (Button_Close != null)
        {
            Button_Close.UnBindOnClickButtonEvent(OnClick_Close);
        }
    }

    private void CreateAllSlots()
    {
        int slotCount = _vm.GetSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            var slotState = _vm.GetSlot(i);
            CreateSlot(slotState);
        }
    }

    private void CreateSlot(AugmentSlotState slotState)
    {
        var gObj = Instantiate(Prefab_Slot, Transform_SlotRoot);
        if (gObj == null)
        {
            return;
        }

        var slotComponent = gObj.GetComponent<AugmentSlotUI>();
        if (slotComponent == null)
        {
            return;
        }

        slotComponent.InitSlot(slotState, _vm);
    }

    private void OnClick_Close()
    {
        UIManager.Instance.CloseAugmentInventoryUI();
    }
}
