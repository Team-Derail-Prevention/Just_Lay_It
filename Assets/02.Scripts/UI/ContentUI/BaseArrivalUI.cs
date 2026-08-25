using System.ComponentModel;
using TMPro;
using UnityEngine;

public class BaseArrivalUI : UIBase
{
    [Header("자원 표시")]
    [SerializeField] private TextMeshProUGUI Text_Wood;
    [SerializeField] private TextMeshProUGUI Text_Stone;

    [Header("캐쉬 표시")]
    [SerializeField] private TextMeshProUGUI Text_Cash;

    [Header("버튼")]
    [SerializeField] private UIButton Button_WeaponGacha;
    [SerializeField] private UIButton Button_TrainStrengthening;
    [SerializeField] private UIButton Button_Inventory;
    [SerializeField] private UIButton Button_TrainRepair;
    [SerializeField] private UIButton Button_TrainDeparture;

    [Header("자제 창고 버튼")]
    [SerializeField] private UIButton Button_Warehouse;

    private UpgradeViewModel _upgradeVm;

    private void OnEnable()
    {
        BindButtons();
        SubscribeResourceEvents();

        if (NetworkResourceService.Instance != null)
        {
            var resourceVm = NetworkResourceService.Instance.GetLocalResourceViewModel();
            SetWoodText(resourceVm.CurrentWood);
            SetStoneText(resourceVm.CurrentStone);
        }

        if (NetworkUpgradeService.Instance != null)
        {
            _upgradeVm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
            _upgradeVm.PropertyChanged += OnUpgradeViewModelPropertyChanged;
            SetCashText(_upgradeVm.CurrentCash);
        }
    }

    private void OnDisable()
    {
        UnbindButtons();
        UnsubscribeResourceEvents();

        if (_upgradeVm != null)
        {
            _upgradeVm.PropertyChanged -= OnUpgradeViewModelPropertyChanged;
        }
    }

    private void BindButtons()
    {
        if (Button_WeaponGacha != null) Button_WeaponGacha.BindOnClickButtonEvent(OnClick_WeaponGacha);
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.BindOnClickButtonEvent(OnClick_TrainStrengthening);
        if (Button_Inventory != null) Button_Inventory.BindOnClickButtonEvent(OnClick_Inventory);
        if (Button_TrainRepair != null) Button_TrainRepair.BindOnClickButtonEvent(OnClick_TrainRepair);
        if (Button_TrainDeparture != null) Button_TrainDeparture.BindOnClickButtonEvent(OnClick_TrainDeparture);
        if (Button_Warehouse != null) Button_Warehouse.BindOnClickButtonEvent(OnClick_Warehouse);
    }

    private void UnbindButtons()
    {
        if (Button_WeaponGacha != null) Button_WeaponGacha.UnBindOnClickButtonEvent(OnClick_WeaponGacha);
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.UnBindOnClickButtonEvent(OnClick_TrainStrengthening);
        if (Button_Inventory != null) Button_Inventory.UnBindOnClickButtonEvent(OnClick_Inventory);
        if (Button_TrainRepair != null) Button_TrainRepair.UnBindOnClickButtonEvent(OnClick_TrainRepair);
        if (Button_TrainDeparture != null) Button_TrainDeparture.UnBindOnClickButtonEvent(OnClick_TrainDeparture);
        if (Button_Warehouse != null) Button_Warehouse.UnBindOnClickButtonEvent(OnClick_Warehouse);
    }

    private void SubscribeResourceEvents()
    {
        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged += SetWoodText;
            ResourceStatusEventHub.Instance.OnStoneChanged += SetStoneText;
        }
    }

    private void UnsubscribeResourceEvents()
    {
        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged -= SetWoodText;
            ResourceStatusEventHub.Instance.OnStoneChanged -= SetStoneText;
        }
    }

    private void OnUpgradeViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpgradeViewModel.CurrentCash))
        {
            SetCashText(_upgradeVm.CurrentCash);
        }
    }

    private void SetWoodText(int curWoodCount)
    {
        if (Text_Wood != null) Text_Wood.text = curWoodCount.ToString();
    }

    private void SetStoneText(int curStoneCount)
    {
        if (Text_Stone != null) Text_Stone.text = curStoneCount.ToString();
    }

    private void SetCashText(int curCashCount)
    {
        if (Text_Cash != null) Text_Cash.text = curCashCount.ToString();
    }

    private void OnClick_WeaponGacha()
    {
        UIManager.Instance.OpenWeaponGachaUI();
    }

    private void OnClick_TrainStrengthening()
    {
        UIManager.Instance.OpenTrainStrengtheningUI();
    }

    private void OnClick_Inventory()
    {
        UIManager.Instance.OpenAugmentInventoryUI();
    }

    private void OnClick_TrainRepair()
    {
        // TODO : TrainRepair 로직 정해지면 연동
        Debug.Log("[BaseArrivalUI] TrainRepair 아직 미구현");
    }

    private void OnClick_TrainDeparture()
    {
        UIManager.Instance.OpenTrainDepartureUI();
    }

    private void OnClick_Warehouse()
    {
        UIManager.Instance.OpenWarehouseUI();
    }
}
