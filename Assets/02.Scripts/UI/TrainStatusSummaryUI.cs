using Enums;
using TMPro;
using UnityEngine;

public class TrainStatusSummaryUI : UIBase
{
    [Header("전투")]
    [SerializeField] private TextMeshProUGUI Text_Hp;
    [SerializeField] private TextMeshProUGUI Text_Defense;

    [Header("적재")]
    [SerializeField] private TextMeshProUGUI Text_Amount;
    [SerializeField] private TextMeshProUGUI Text_Resource;
    [SerializeField] private TextMeshProUGUI Text_Boarding;

    [Header("드론")]
    [SerializeField] private TextMeshProUGUI Text_CollectSpeed;
    [SerializeField] private TextMeshProUGUI Text_CollectEfficiency;
    [SerializeField] private TextMeshProUGUI Text_ActionSpeed;

    private const float POLL_INTERVAL = 0.5f;

    private void OnEnable()
    {
        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged += OnHpChanged;
        }

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged += OnResourceChanged;
            ResourceStatusEventHub.Instance.OnStoneChanged += OnResourceChanged;
        }

        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded += OnInGameUpgraded;
        }

        LocalizationEventHub.Instance.OnLanguageChanged += OnLanguageChanged_LocalizationEventHub;

        RefreshAll();
        InvokeRepeating(nameof(RefreshAll), POLL_INTERVAL, POLL_INTERVAL);
    }

    private void OnDisable()
    {
        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged -= OnHpChanged;
        }

        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged -= OnResourceChanged;
            ResourceStatusEventHub.Instance.OnStoneChanged -= OnResourceChanged;
        }

        if (UpgradeEventHub.Instance != null)
        {
            UpgradeEventHub.Instance.OnInGameUpgraded -= OnInGameUpgraded;
        }

        if (LocalizationEventHub.Instance != null)
        {
            LocalizationEventHub.Instance.OnLanguageChanged -= OnLanguageChanged_LocalizationEventHub;
        }

        CancelInvoke(nameof(RefreshAll));
    }

    private void OnLanguageChanged_LocalizationEventHub(LanguageType language)
    {
        RefreshAll();
    }

    private void OnHpChanged(float curHp, float maxHp)
    {
        if (Text_Hp != null)
        {
            Text_Hp.text = $"{LocalizationManager.Instance.GetText("Base_UI_RML_01")} : {Mathf.RoundToInt(curHp)} / {Mathf.RoundToInt(maxHp)}";
            Text_Hp.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RML_01");
        }
    }

    private void OnResourceChanged(int amount)
    {
        RefreshCargo();
    }

    private void OnInGameUpgraded(string slotDataId, int newLevel)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshBattle();
        RefreshCargo();
        RefreshDrone();
    }

    private void RefreshBattle()
    {
        if (TrainManager.Instance == null || TrainManager.Instance.ActiveTrain == null)
        {
            return;
        }

        Train activeTrain = TrainManager.Instance.ActiveTrain;

        if (Text_Hp != null)
        {
            Text_Hp.text = $"{LocalizationManager.Instance.GetText("Base_UI_RML_01")} : {Mathf.RoundToInt(activeTrain.CurrentHp)} / {Mathf.RoundToInt(activeTrain.MaxHp)}";
            Text_Hp.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RML_01");
        }

        if (Text_Defense != null)
        {
            Text_Defense.text = $"{LocalizationManager.Instance.GetText("Base_UI_RML_02")} : {Mathf.RoundToInt(activeTrain.Defense)}";
            Text_Defense.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RML_02");
        }
    }

    private void RefreshCargo()
    {
        if (Text_Amount != null && NetworkResourceService.Instance != null)
        {
            Text_Amount.text = $"{LocalizationManager.Instance.GetText("Base_UI_RMM_01")} : {NetworkResourceService.Instance.CurrentCargoLoad.ToString()}";
            Text_Amount.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RMM_01");
        }

        if (Text_Resource != null && NetworkResourceService.Instance != null)
        {
            Text_Resource.text = $"{LocalizationManager.Instance.GetText("Base_UI_RMM_02")} : {NetworkResourceService.Instance.CargoLimit.ToString()}";
            Text_Resource.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RMM_02");
        }

        if (Text_Boarding != null && NetworkTrainCargoService.Instance != null)
        {
            Text_Boarding.text = $"{LocalizationManager.Instance.GetText("Base_UI_RMM_03")} : {NetworkTrainCargoService.Instance.BoardedCitizenCount} / {NetworkTrainCargoService.Instance.BoardingLimit}";
            Text_Boarding.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RMM_03");
        }
    }

    private void RefreshDrone()
    {
        if (DroneManager.Instance == null)
        {
            return;
        }

        if (Text_CollectSpeed != null)
        {
            Text_CollectSpeed.text = $"{LocalizationManager.Instance.GetText("Base_UI_RMR_01")} : x{DroneManager.Instance.GatherSpeedMultiplier:0.00}";
            Text_CollectSpeed.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RMR_01");
        }

        if (Text_CollectEfficiency != null)
        {
            Text_CollectEfficiency.text = $"{LocalizationManager.Instance.GetText("Base_UI_RMR_02")} : +{DroneManager.Instance.YieldBonus}";
            Text_CollectEfficiency.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RMR_02");
        }

        if (Text_ActionSpeed != null)
        {
            Text_ActionSpeed.text = $"{LocalizationManager.Instance.GetText("Base_UI_RMR_03")} : x{DroneManager.Instance.MoveSpeedMultiplier:0.00}";
            Text_ActionSpeed.fontSize = LocalizationManager.Instance.GetFontSize("Base_UI_RMR_03");
        }
    }
}
