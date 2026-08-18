using System.ComponentModel;
using System.Data.SqlTypes;
using TMPro;
using UnityEngine;

public class HudResourceUI : UIBase
{
    [Header("인게임 재화")]
    [SerializeField] private TextMeshProUGUI Text_Money;

    [Header("로비 업그레이드 재화")]
    [SerializeField] private TextMeshProUGUI Text_Cash;

    [Header("나무")]
    [SerializeField] private TextMeshProUGUI Text_Wood;

    [Header("돌")]
    [SerializeField] private TextMeshProUGUI Text_Stone;

    [Header("구출한 사람 수")]
    [SerializeField] private TextMeshProUGUI Text_RescuedHuman;

    private UpgradeViewModel _upgradeVm;

    private void OnEnable()
    {
        if (ResourceStatusEventHub.Instance == null)
        {
            Debug.LogError("[HudResourceUI] ResourceStatusEventHub.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
            return;
        }
        else
        {
            ResourceStatusEventHub.Instance.OnMoneyChanged += SetMoney;
            ResourceStatusEventHub.Instance.OnWoodChanged += SetWood;
            ResourceStatusEventHub.Instance.OnStoneChanged += SetStone;
            ResourceStatusEventHub.Instance.OnRescuedHumanChanged += SetRescuedHuman;
        }

        if (NetworkUpgradeService.Instance != null)
        {
            _upgradeVm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
            _upgradeVm.PropertyChanged += OnUpgradeViewModelPropertyChanged;
            SetCash(_upgradeVm.CurrentGold);
        }
    }

    private void OnDisable()
    {
        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnMoneyChanged -= SetMoney;
            ResourceStatusEventHub.Instance.OnWoodChanged -= SetWood;
            ResourceStatusEventHub.Instance.OnStoneChanged -= SetStone;
            ResourceStatusEventHub.Instance.OnRescuedHumanChanged -= SetRescuedHuman;
        }

        if (_upgradeVm != null)
        {
            _upgradeVm.PropertyChanged -= OnUpgradeViewModelPropertyChanged;
        }
    }

    private void OnUpgradeViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpgradeViewModel.CurrentGold))
        {
            SetCash(_upgradeVm.CurrentGold);
        }
    }

    public void SetMoney(int curMoneyCount)
    {
        if (Text_Money != null)
        {
            Text_Money.text = curMoneyCount.ToString();
        }
    }

    public void SetCash(int curCashCount)
    {
        if (Text_Cash != null)
        {
            Text_Cash.text = curCashCount.ToString();
        }
    }

    public void SetWood(int curWoodCount)
    {
        if (Text_Wood != null)
        {
            Text_Wood.text = curWoodCount.ToString();
        }
    }

    public void SetStone(int curStoneCount)
    {
        if (Text_Stone != null)
        {
            Text_Stone.text = curStoneCount.ToString();
        }
    }

    public void SetRescuedHuman(int curRescuedCount)
    {
        if (Text_RescuedHuman != null)
        {
            Text_RescuedHuman.text = curRescuedCount.ToString();
        }
    }
}
