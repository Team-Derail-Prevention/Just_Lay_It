using UnityEngine;
using TMPro;

public class StationArrivalUI : UIBase
{
    private const int REPAIR_HEAL_PERCENT = 20;
    private const int REPAIR_STONE_COST = 40;

    private const string TAKE_STONE_LABEL_ID = "StationArrival_UI_ML_04";
    private const string CANCEL_STONE_LABEL_ID = "StationArrival_UI_ML_05";
    private const string BOARD_CITIZEN_LABEL_ID = "StationArrival_UI_MM_04";
    private const string CANCEL_CITIZEN_LABEL_ID = "StationArrival_UI_MM_05";

    [Header("좌상단 표시")]
    [SerializeField] private TextMeshProUGUI Text_MyStone;
    [SerializeField] private TextMeshProUGUI Text_Boarding;
    [SerializeField] private TextMeshProUGUI Text_CargoLoad;

    [Header("돌")]
    [SerializeField] private TextMeshProUGUI Text_StationStoneAmount;
    [SerializeField] private TextMeshProUGUI Text_TakeStoneAmount;
    [SerializeField] private UIButton Button_InputStone;

    [Header("시민")]
    [SerializeField] private TextMeshProUGUI Text_StationCitizenAmount;
    [SerializeField] private TextMeshProUGUI Text_BoardCitizenAmount;
    [SerializeField] private UIButton Button_InputCitizen;

    [Header("열차 수리")]
    [SerializeField] private TextMeshProUGUI Text_TrainHpPercent;
    [SerializeField] private TextMeshProUGUI Text_RepairCost;
    [SerializeField] private UIButton Button_Repair;

    [Header("출발")]
    [SerializeField] private UIButton Button_Departure;

    private StationObject _currentStation;
    private int _stationAvailableStone;
    private int _stationAvailableCitizen;
    private int _takeStoneAmount;
    private int _boardCitizenAmount;
    private bool _isStoneTakeSelected;
    private bool _isCitizenBoardSelected;
    private float _currentHpPercent = 100f;

    private void OnEnable()
    {
        if (Button_InputStone != null)
        {
            Button_InputStone.BindOnClickButtonEvent(OnClick_TakeStone);
        }

        if (Button_InputCitizen != null)
        {
            Button_InputCitizen.BindOnClickButtonEvent(OnClick_BoardCitizen);
        }

        if (Button_Repair != null)
        {
            Button_Repair.BindOnClickButtonEvent(OnClick_Repair);
        }

        if (Button_Departure != null)
        {
            Button_Departure.BindOnClickButtonEvent(OnClick_Departure);
        }

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged += OnHpChanged;
        }

        _takeStoneAmount = 0;
        _boardCitizenAmount = 0;
        _isStoneTakeSelected = false;
        _isCitizenBoardSelected = false;

        RefreshRepairCostText();
        SyncHpFromActiveTrain();
        RefreshTopIndicators();
        RefreshTakeAmountTexts();
        RefreshInputButtonTexts();
    }

    private void OnDisable()
    {
        if (Button_InputStone != null)
        {
            Button_InputStone.UnBindOnClickButtonEvent(OnClick_TakeStone);
        }

        if (Button_InputCitizen != null)
        {
            Button_InputCitizen.UnBindOnClickButtonEvent(OnClick_BoardCitizen);
        }

        if (Button_Repair != null)
        {
            Button_Repair.UnBindOnClickButtonEvent(OnClick_Repair);
        }

        if (Button_Departure != null)
        {
            Button_Departure.UnBindOnClickButtonEvent(OnClick_Departure);
        }

        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged -= OnHpChanged;
        }
    }

    public void Init(StationObject station, int availableStone, int availableCitizen)
    {
        _currentStation = station;
        _stationAvailableStone = availableStone;
        _stationAvailableCitizen = availableCitizen;

        if (Text_StationStoneAmount != null)
        {
            Text_StationStoneAmount.text = _stationAvailableStone.ToString();
        }

        if (Text_StationCitizenAmount != null)
        {
            Text_StationCitizenAmount.text = _stationAvailableCitizen.ToString();
        }
    }

    private void SyncHpFromActiveTrain()
    {
        if (TrainStatusEventHub.Instance == null)
        {
            return;
        }

        OnHpChanged(TrainStatusEventHub.Instance.CurrentHp, TrainStatusEventHub.Instance.CurrentMaxHp);
    }

    private void OnHpChanged(float curHp, float maxHp)
    {
        if (maxHp > 0f)
        {
            _currentHpPercent = curHp / maxHp * 100f;
        }
        else
        {
            _currentHpPercent = 0f;
        }

        RefreshHpText();
    }

    private void RefreshHpText()
    {
        if (Text_TrainHpPercent != null)
        {
            Text_TrainHpPercent.text = $"{Mathf.RoundToInt(_currentHpPercent)}%";
        }
    }

    private void RefreshRepairCostText()
    {
        if (Text_RepairCost == null)
        {
            return;
        }

        bool isFreeAvailable = NetworkResourceService.Instance != null && NetworkResourceService.Instance.IsStationRepairFreeAvailable;
        Text_RepairCost.text = isFreeAvailable ? LocalizationManager.Instance.GetText("StationArrival_UI_MR_05") : REPAIR_STONE_COST.ToString();
    }

    private void RefreshTopIndicators()
    {
        var resourceVm = NetworkResourceService.Instance.GetLocalResourceViewModel();

        if (Text_MyStone != null)
        {
            Text_MyStone.text = resourceVm.CurrentStone.ToString();
        }

        if (Text_Boarding != null)
        {
            Text_Boarding.text = $"{NetworkTrainCargoService.Instance.BoardedCitizenCount}/{NetworkTrainCargoService.Instance.BoardingLimit}";
        }

        if (Text_CargoLoad != null)
        {
            Text_CargoLoad.text = $"{NetworkResourceService.Instance.CurrentCargoLoad}/{NetworkResourceService.Instance.CargoLimit}";
        }

        RefreshHpText();
    }

    private void RefreshTakeAmountTexts()
    {
        if (Text_TakeStoneAmount != null)
        {
            Text_TakeStoneAmount.text = _takeStoneAmount.ToString();
        }

        if (Text_BoardCitizenAmount != null)
        {
            Text_BoardCitizenAmount.text = _boardCitizenAmount.ToString();
        }
    }

    private void RefreshInputButtonTexts()
    {
        if (Button_InputStone != null)
        {
            string stoneLabelId = _isStoneTakeSelected ? CANCEL_STONE_LABEL_ID : TAKE_STONE_LABEL_ID;
            Button_InputStone.ChangeButtonText(LocalizationManager.Instance.GetText(stoneLabelId));
        }

        if (Button_InputCitizen != null)
        {
            string citizenLabelId = _isCitizenBoardSelected ? CANCEL_CITIZEN_LABEL_ID : BOARD_CITIZEN_LABEL_ID;
            Button_InputCitizen.ChangeButtonText(LocalizationManager.Instance.GetText(citizenLabelId));
        }
    }

    private int GetStoneTakeLimit()
    {
        return Mathf.Min(_stationAvailableStone, NetworkResourceService.Instance.RemainingCargoCapacity);
    }

    private int GetCitizenBoardLimit()
    {
        return Mathf.Min(_stationAvailableCitizen, NetworkTrainCargoService.Instance.RemainingBoardingCapacity);
    }

    private void OnClick_TakeStone()
    {
        if (_isStoneTakeSelected)
        {
            _isStoneTakeSelected = false;
            _takeStoneAmount = 0;
            RefreshTakeAmountTexts();
            RefreshInputButtonTexts();
            return;
        }

        int takeLimit = GetStoneTakeLimit();
        if (takeLimit <= 0)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_06"));
            return;
        }

        _isStoneTakeSelected = true;
        _takeStoneAmount = takeLimit;
        RefreshTakeAmountTexts();
        RefreshInputButtonTexts();
    }

    private void OnClick_BoardCitizen()
    {
        if (_isCitizenBoardSelected)
        {
            _isCitizenBoardSelected = false;
            _boardCitizenAmount = 0;
            RefreshTakeAmountTexts();
            RefreshInputButtonTexts();
            return;
        }

        int boardLimit = GetCitizenBoardLimit();
        if (boardLimit <= 0)
        {
            UIManager.Instance.OpenExitConfirmPopup(null, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_07"));
            return;
        }

        _isCitizenBoardSelected = true;
        _boardCitizenAmount = boardLimit;
        RefreshTakeAmountTexts();
        RefreshInputButtonTexts();
    }

    private void RefreshSelectedStoneAmount()
    {
        if (_isStoneTakeSelected == false)
        {
            return;
        }

        int takeLimit = GetStoneTakeLimit();
        if (takeLimit <= 0)
        {
            _isStoneTakeSelected = false;
            _takeStoneAmount = 0;
            RefreshInputButtonTexts();
        }
        else
        {
            _takeStoneAmount = takeLimit;
        }

        RefreshTakeAmountTexts();
    }

    private void OnClick_Repair()
    {
        if (GameManager.Train != null && GameManager.Train.IsTrainHpFull())
        {
            SoundManager.Instance?.PlaySFX(SfxAddress.Ui.Denied);
            UIManager.Instance.OpenExitConfirmPopup(null, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_05"));
            return;
        }

        bool isFreeAvailable = NetworkResourceService.Instance != null && NetworkResourceService.Instance.IsStationRepairFreeAvailable;

        bool isSpent = NetworkResourceService.Instance.TrySpendStoneForStationRepair(REPAIR_STONE_COST);
        if (isSpent == false)
        {
            SoundManager.Instance?.PlaySFX(SfxAddress.Ui.Denied);
            UIManager.Instance.OpenExitConfirmPopup(null, null, LocalizationManager.Instance.GetText("ExitConfirm_PopUp_UI_02"));
            return;
        }

        RefreshTopIndicators();
        RefreshRepairCostText();
        RefreshSelectedStoneAmount();

        GameManager.Train.HealActiveTrain(REPAIR_HEAL_PERCENT);

        SoundManager.Instance?.PlaySFX(SfxAddress.Train.Repair);

        Debug.Log($"[StationArrivalUI] 수리 요청 완료 (최대 체력의 {REPAIR_HEAL_PERCENT}% 회복), 돌 {REPAIR_STONE_COST} 소모");
    }

    private void OnClick_Departure()
    {
        int actualTakenStone = NetworkResourceService.Instance.AddStone(_takeStoneAmount);
        int actualBoarded = NetworkTrainCargoService.Instance.BoardCitizens(_boardCitizenAmount);

        if (_currentStation != null)
        {
            GameManager.Instance.CompleteStation(actualTakenStone, actualBoarded);
        }

        UIManager.Instance.CloseStationArrivalUI();
    }
}
