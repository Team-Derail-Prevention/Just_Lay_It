using Enums;
using UnityEngine;

public class AugmentEquipUI : MonoBehaviour
{
    [System.Serializable]
    private class SectionRow
    {
        public TrainCarSection Section;
        public Transform Transform_SlotRoot;
    }

    [SerializeField] private GameObject Prefab_Slot;
    [SerializeField] private SectionRow[] _sectionRowList;

    private void OnEnable()
    {
        CreateAllRows();
    }

    private void CreateAllRows()
    {
        if (_sectionRowList == null)
        {
            return;
        }

        for (int i = 0; i < _sectionRowList.Length; i++)
        {
            CreateRow(_sectionRowList[i]);
        }
    }

    private void CreateRow(SectionRow row)
    {
        if (row == null || row.Transform_SlotRoot == null)
        {
            return;
        }

        ClearExistingChildren(row.Transform_SlotRoot);

        AugmentEquipViewModel vm = NetworkAugmentService.Instance.GetLocalWeaponEquipViewModel(row.Section);

        int slotCount = vm.GetSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            AugmentSlotState slotState = vm.GetSlot(i);
            CreateSlot(row.Transform_SlotRoot, slotState, vm);
        }
    }

    private void ClearExistingChildren(Transform slotRoot)
    {
        for (int i = slotRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(slotRoot.GetChild(i).gameObject);
        }
    }

    private void CreateSlot(Transform slotRoot, AugmentSlotState slotState, AugmentEquipViewModel vm)
    {
        var gObj = Instantiate(Prefab_Slot, slotRoot);
        if (gObj == null)
        {
            return;
        }

        var slotComponent = gObj.GetComponent<AugmentSlotUI>();
        if (slotComponent == null)
        {
            return;
        }

        slotComponent.InitSlot(slotState, vm);
    }
}
