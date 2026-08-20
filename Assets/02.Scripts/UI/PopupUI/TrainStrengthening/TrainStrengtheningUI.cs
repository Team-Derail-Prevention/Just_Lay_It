using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

public class TrainStrengtheningUI :UIBase
{
    [Header("슬롯 프리팹")]
    [SerializeField] private GameObject Prefab_Slot;

    [Header("카테고리별 슬롯 루트")]
    [SerializeField] private Transform Transform_BattleRoot;
    [SerializeField] private Transform Transform_CargoRoot;
    [SerializeField] private Transform Transform_DroneRoot;

    [Header("구매 / 닫기")]
    [SerializeField] private UIButton Button_Purchase;
    [SerializeField] private UIButton Button_Close;

    private TrainStrengtheningViewModel _vm;
    private readonly Dictionary<string, TrainStrengtheningSlotUI> _createdSlotList = new Dictionary<string, TrainStrengtheningSlotUI>();
    private string _selectedSlotDataId;

    private void OnEnable()
    {
        if (Button_Close != null)
        {
            Button_Close.BindOnClickButtonEvent(OnClick_Close);
        }

        if (Button_Purchase != null)
        {
            Button_Purchase.BindOnClickButtonEvent(OnClick_Purchase); ;
        }

        _vm = NetworkTrainStrengtheningService.Instance.GetLocalTrainStrengtheningViewModel();
        _vm.PropertyChanged += OnPropertyChanged_View;

        _selectedSlotDataId = null;
        CreateAllSlots();
    }

    private void OnDisable()
    {
        if (Button_Close != null)
        {
            Button_Close.UnBindOnClickButtonEvent(OnClick_Close);
        }

        if (Button_Purchase != null)
        {
            Button_Purchase.UnBindOnClickButtonEvent(OnClick_Purchase);
        }

        if (_vm != null)
        {
            _vm.PropertyChanged -= OnPropertyChanged_View;
        }

        DestroyAllSlots();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SlotListAdded")
        {
            CreateAllSlots();
        }
    }

    private void DestroyAllSlots()
    {
        foreach (var slotKv in _createdSlotList)
        {
            if (slotKv.Value != null)
            {
                Destroy(slotKv.Value.gameObject);
            }
        }

        _createdSlotList.Clear();
    }

    private void CreateAllSlots()
    {
        DestroyAllSlots();

        CreateSlotsForCategory(TrainStatCategory.Battle, Transform_BattleRoot);
        CreateSlotsForCategory(TrainStatCategory.Cargo, Transform_CargoRoot);
        CreateSlotsForCategory(TrainStatCategory.Drone, Transform_DroneRoot);
    }

    private void CreateSlotsForCategory(TrainStatCategory category, Transform root)
    {
        if (root == null || Prefab_Slot == null)
        {
            return;
        }

        var slotVmList = _vm.GetSlotsByCategory(category);

        foreach (var slotVm in slotVmList)
        {
            var gObj = Instantiate(Prefab_Slot, root);
            var slotComponent = gObj.GetComponent<TrainStrengtheningSlotUI>();
            if (slotComponent == null)
            {
                continue;
            }

            slotComponent.InitSlot(slotVm, OnClickSelectSlot);
            _createdSlotList.Add(slotVm.SlotDataId, slotComponent);
        }
    }

    private void OnClickSelectSlot(string slotDataId)
    {
        if (_selectedSlotDataId == slotDataId)
        {
            _selectedSlotDataId = null;
        }
        else
        {
            _selectedSlotDataId = slotDataId;
        }

        RefreshSelectedVisual();
    }

    private void RefreshSelectedVisual()
    {
        foreach (var slotKv in _createdSlotList)
        {
            slotKv.Value.SetSelected(slotKv.Key == _selectedSlotDataId);
        }
    }

    private void OnClick_Purchase()
    {
        if (string.IsNullOrEmpty(_selectedSlotDataId) == true)
        {
            Debug.LogWarning("[TrainStrengtheningUI] 강화할 항목을 먼저 선택해주세요");
            return;
        }

        NetworkTrainStrengtheningService.Instance.RequestPurchase(_selectedSlotDataId);
    }

    private void OnClick_Close()
    {
        UIManager.Instance.CloseTrainStrengtheningUI();
    }
}
