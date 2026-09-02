using UnityEngine;

public class AugmentInventoryUI : UIBase
{
    [SerializeField] private GameObject Prefab_Slot;
    [SerializeField] private Transform Transform_SlotRoot;
    [SerializeField] private UIButton Button_Close;

    private AugmentInventoryViewModel _vm;

    private void OnEnable()
    {
        if (Button_Close != null)
        {
            Button_Close.BindOnClickButtonEvent(OnClick_Close);
        }

        _vm = NetworkAugmentService.Instance.GetLocalAugmentInventoryViewModel();

        ClearExistingSlots();
        CreateAllSlots();
    }

    private void OnDisable()
    {
        if (Button_Close != null)
        {
            Button_Close.UnBindOnClickButtonEvent(OnClick_Close);
        }
    }

    private void ClearExistingSlots()
    {
        if (Transform_SlotRoot == null)
        {
            return;
        }

        for (int i = Transform_SlotRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(Transform_SlotRoot.GetChild(i).gameObject);
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
