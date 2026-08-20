using UnityEngine;

public class AugmentEquipUI : MonoBehaviour
{
    [SerializeField] private GameObject Prefab_Slot;
    [SerializeField] private Transform Transform_SlotRoot;

    private AugmentEquipViewModel _vm;
    private bool _isSlotsCreated;

    private void OnEnable()
    {
        _vm = NetworkAugmentService.Instance.GetLocalAugmentEquipViewModel();

        if (_isSlotsCreated == false)
        {
            CreateAllSlots();
            _isSlotsCreated = true;
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
}
