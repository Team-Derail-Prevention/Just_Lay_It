using System.ComponentModel;
using TMPro;
using UnityEngine;

public class BaseArrivalUI : UIBase
{
    private const int REPAIR_HEAL_PERCENT = 20; // 임시 값 추후
    private const int REPAIR_STONE_COST = 40; // 임시 값 추후

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

    [Header("로비 나가기")]
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
        if (Button_Warehouse != null) Button_Warehouse.BindOnClickButtonEvent(OnClick_ReturnToLobby);

        BindButtonDescriptionEvents();
    }

    private void UnbindButtons()
    {
        if (Button_WeaponGacha != null) Button_WeaponGacha.UnBindOnClickButtonEvent(OnClick_WeaponGacha);
        if (Button_TrainStrengthening != null) Button_TrainStrengthening.UnBindOnClickButtonEvent(OnClick_TrainStrengthening);
        if (Button_Inventory != null) Button_Inventory.UnBindOnClickButtonEvent(OnClick_Inventory);
        if (Button_TrainRepair != null) Button_TrainRepair.UnBindOnClickButtonEvent(OnClick_TrainRepair);
        if (Button_TrainDeparture != null) Button_TrainDeparture.UnBindOnClickButtonEvent(OnClick_TrainDeparture);
        if (Button_Warehouse != null) Button_Warehouse.UnBindOnClickButtonEvent(OnClick_ReturnToLobby);

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

    private void OnButtonHoverEnter(string description, int fontSize)
    {
        if (Text_BK == null)
        {
            return;
        }

        Text_BK.text = description;

        if (fontSize > 0)
        {
            Text_BK.fontSize = fontSize;
        }
    }

    private void OnTrainRepairHoverEnter(string description, int fontSize)
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

        string template = LocalizationManager.Instance.GetText("Base_UI_LM_Button_hover_05");
        Text_BK.text = string.Format(template, curHpPercent, REPAIR_STONE_COST, REPAIR_HEAL_PERCENT);
        Text_BK.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_LM_Button_hover_05");
    }

    private void OnWeaponGachaHoverEnter(string description, int fontSize)
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
        string template = LocalizationManager.Instance.GetText("Base_UI_LM_Button_hover_01");
        Text_BK.text = string.Format(template, gachaCost);
        Text_BK.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_LM_Button_hover_01");
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
            SoundManager.Instance?.PlaySFX(SfxAddress.Ui.Denied);
            UIManager.Instance.OpenExitConfirmPopup(null, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_04"));
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
        bool isFreeAvailable = NetworkResourceService.Instance != null && NetworkResourceService.Instance.IsBaseRepairFreeAvailable;

        if (isFreeAvailable == false && TryValidateStoneCost(REPAIR_STONE_COST) == false)
        {
            return;
        }

        if (GameManager.Train != null && GameManager.Train.IsTrainHpFull())
        {
            SoundManager.Instance?.PlaySFX(SfxAddress.Ui.Denied);
            UIManager.Instance.OpenExitConfirmPopup(null, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_05"));
            return;
        }

        bool isSpent = NetworkResourceService.Instance.TrySpendStoneForBaseRepair(REPAIR_STONE_COST);
        if (isSpent == false)
        {
            Debug.LogWarning("[BaseArrivalUI] 재화 검증 이후 소모에 실패했습니다. 상태를 확인해주세요.");
            return;
        }

        GameManager.Train.HealActiveTrain(REPAIR_HEAL_PERCENT);

        SoundManager.Instance?.PlaySFX(SfxAddress.Train.Repair);

        Debug.Log($"[BaseArrivalUI] 수리 요청 완료 (최대 체력의 {REPAIR_HEAL_PERCENT}% 회복), 돌 {REPAIR_STONE_COST} 소모");
    }

    private void OnClick_TrainDeparture()
    {
        UIManager.Instance.OpenTrainDepartureUI();
    }

    private void OnClick_ReturnToLobby()
    {
        GameManager.Instance.GiveUpRun();
    }
}
