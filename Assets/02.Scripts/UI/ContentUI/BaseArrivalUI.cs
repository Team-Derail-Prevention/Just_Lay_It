using System.ComponentModel;
using TMPro;
using UnityEngine;

public class BaseArrivalUI : UIBase
{
    private const int REPAIR_HEAL_PERCENT = 20; // 임시 값 추후
    private const int REPAIR_STONE_COST = 40; // 임시 값 추후
    private const string InsufficientResourceMessage = "재화가 부족합니다.";

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

    [Header("버튼 설명 표시")]
    [SerializeField] private TextMeshProUGUI Text_BK;

    private UpgradeViewModel _upgradeVm;
    private float _currentTrainHp;
    private float _currentTrainMaxHp;

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

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged += OnTrainHpChanged;
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

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged -= OnTrainHpChanged;
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

        BindButtonDescriptionEvents();
    }

    private void UnbindButtons()
    {
        if (Button_WeaponGacha != null) Button_WeaponGacha.UnBindOnClickButtonEvent(OnClick_WeaponGacha);
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.UnBindOnClickButtonEvent(OnClick_TrainStrengthening);
        if (Button_Inventory != null) Button_Inventory.UnBindOnClickButtonEvent(OnClick_Inventory);
        if (Button_TrainRepair != null) Button_TrainRepair.UnBindOnClickButtonEvent(OnClick_TrainRepair);
        if (Button_TrainDeparture != null) Button_TrainDeparture.UnBindOnClickButtonEvent(OnClick_TrainDeparture);
        if (Button_Warehouse != null) Button_Warehouse.UnBindOnClickButtonEvent(OnClick_Warehouse);

        UnbindButtonDescriptionEvents();
    }

    private void BindButtonDescriptionEvents()
    {
        if (Button_WeaponGacha != null) Button_WeaponGacha.OnPointerEnterButton += OnWeaponGachaHoverEnter;
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.OnPointerEnterButton += OnButtonHoverEnter;
        if (Button_Inventory != null) Button_Inventory.OnPointerEnterButton += OnButtonHoverEnter;
        if (Button_TrainRepair != null) Button_TrainRepair.OnPointerEnterButton += OnTrainRepairHoverEnter;
        if (Button_TrainDeparture != null) Button_TrainDeparture.OnPointerEnterButton += OnButtonHoverEnter;

        if (Button_WeaponGacha != null) Button_WeaponGacha.OnPointerExitButton += OnButtonHoverExit;
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.OnPointerExitButton += OnButtonHoverExit;
        if (Button_Inventory != null) Button_Inventory.OnPointerExitButton += OnButtonHoverExit;
        if (Button_TrainRepair != null) Button_TrainRepair.OnPointerExitButton += OnButtonHoverExit;
        if (Button_TrainDeparture != null) Button_TrainDeparture.OnPointerExitButton += OnButtonHoverExit;
    }

    private void UnbindButtonDescriptionEvents()
    {
        if (Button_WeaponGacha != null) Button_WeaponGacha.OnPointerEnterButton -= OnWeaponGachaHoverEnter;
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.OnPointerEnterButton -= OnButtonHoverEnter;
        if (Button_Inventory != null) Button_Inventory.OnPointerEnterButton -= OnButtonHoverEnter;
        if (Button_TrainRepair != null) Button_TrainRepair.OnPointerEnterButton -= OnTrainRepairHoverEnter;
        if (Button_TrainDeparture != null) Button_TrainDeparture.OnPointerEnterButton -= OnButtonHoverEnter;

        if (Button_WeaponGacha != null) Button_WeaponGacha.OnPointerExitButton -= OnButtonHoverExit;
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.OnPointerExitButton -= OnButtonHoverExit;
        if (Button_Inventory != null) Button_Inventory.OnPointerExitButton -= OnButtonHoverExit;
        if (Button_TrainRepair != null) Button_TrainRepair.OnPointerExitButton -= OnButtonHoverExit;
        if (Button_TrainDeparture != null) Button_TrainDeparture.OnPointerExitButton -= OnButtonHoverExit;
    }

    private void OnButtonHoverEnter(string description)
    {
        if (Text_BK != null)
        {
            Text_BK.text = description;
        }
    }

    private void OnTrainRepairHoverEnter(string description)
    {
        if (Text_BK == null)
        {
            return;
        }

        int curHpPercent = 0;
        if (_currentTrainMaxHp > 0f)
        {
            curHpPercent = Mathf.RoundToInt(_currentTrainHp / _currentTrainMaxHp * 100f);
        }

        Text_BK.text = $"현재 열차의 체력은 {curHpPercent}% 입니다.\n열차 수리 한번당 가격 : 돌 {REPAIR_STONE_COST}개 이고 {REPAIR_HEAL_PERCENT}%의 체력을 회복합니다.";
    }

    private void OnWeaponGachaHoverEnter(string description)
    {
        if (Text_BK == null)
        {
            return;
        }

        if (NetworkGachaService.Instance == null)
        {
            Text_BK.text = description;
            return;
        }

        int gachaCost = NetworkGachaService.Instance.CurrentGachaCost;
        Text_BK.text = $"무기 가챠 1회 현재 가격 : 돌 {gachaCost}. \n열차에 장착할 수 있는 무기를 랜덤으로 3개 뽑고, 하나를 정해서 인벤토리로 가져갈 수 있습니다.\n무기 가챠 비용은 구매시마다 5씩 증가합니다.";
    }

    private void OnButtonHoverExit()
    {
        if (Text_BK != null)
        {
            Text_BK.text = string.Empty;
        }
    }

    private void OnTrainHpChanged(float curHp, float maxHp)
    {
        _currentTrainHp = curHp;
        _currentTrainMaxHp = maxHp;
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

    private bool TryValidateStoneCost(int cost)
    {
        if (NetworkResourceService.Instance == null)
        {
            return false;
        }

        if (NetworkResourceService.Instance.HasEnoughStone(cost) == false)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, InsufficientResourceMessage);
            return false;
        }

        return true;
    }

    private void OnClick_WeaponGacha()
    {
        int cost = NetworkGachaService.Instance.CurrentGachaCost;
        if (TryValidateStoneCost(cost) == false)
        {
            return;
        }

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
        if (TryValidateStoneCost(REPAIR_STONE_COST) == false)
        {
            return;
        }

        if (GameManager.Train != null && GameManager.Train.IsTrainHpFull())
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, "열차 체력이 이미 가득 찼습니다!");
            return;
        }

        bool isSpent = NetworkResourceService.Instance.TrySpendStone(REPAIR_STONE_COST);
        if (isSpent == false)
        {
            Debug.LogWarning("[BaseArrivalUI] 재화 검증 이후 소모에 실패했습니다. 상태를 확인해주세요.");
            return;
        }

        GameManager.Train.HealActiveTrain(REPAIR_HEAL_PERCENT);

        Debug.Log($"[BaseArrivalUI] 수리 요청 완료 (최대 체력의 {REPAIR_HEAL_PERCENT}% 회복), 돌 {REPAIR_STONE_COST} 소모");
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
