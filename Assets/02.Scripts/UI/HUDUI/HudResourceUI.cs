using System.ComponentModel;
using TMPro;
using UnityEngine;

public class HudResourceUI : UIBase
{
    [Header("로비 재화")]
    [SerializeField] private TextMeshProUGUI Text_Cash;

    [Header("나무")]
    [SerializeField] private TextMeshProUGUI Text_Wood;

    [Header("적재 제한량")]
    [SerializeField] private TextMeshProUGUI Text_StoneLimit;

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
            ResourceStatusEventHub.Instance.OnWoodChanged += SetWood;
            ResourceStatusEventHub.Instance.OnStoneChanged += SetStone;
            ResourceStatusEventHub.Instance.OnRescuedHumanChanged += SetRescuedHuman;
            ResourceStatusEventHub.Instance.OnCargoLimitChanged += SetStoneLimit;
        }

        if (NetworkResourceService.Instance != null)
        {
            var resourceVm = NetworkResourceService.Instance.GetLocalResourceViewModel();
            SetWood(resourceVm.CurrentWood);
            SetStone(resourceVm.CurrentStone);
            SetRescuedHuman(resourceVm.RescuedHumanCount);
            SetStoneLimit(NetworkResourceService.Instance.CargoLimit);
        }

        if (NetworkUpgradeService.Instance != null)
        {
            _upgradeVm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
            _upgradeVm.PropertyChanged += OnUpgradeViewModelPropertyChanged;
            SetCash(_upgradeVm.CurrentCash);
        }
    }

    private void OnDisable()
    {
        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged -= SetWood;
            ResourceStatusEventHub.Instance.OnStoneChanged -= SetStone;
            ResourceStatusEventHub.Instance.OnRescuedHumanChanged -= SetRescuedHuman;
            ResourceStatusEventHub.Instance.OnCargoLimitChanged -= SetStoneLimit;
        }

        if (_upgradeVm != null)
        {
            _upgradeVm.PropertyChanged -= OnUpgradeViewModelPropertyChanged;
        }
    }

    private void OnUpgradeViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpgradeViewModel.CurrentCash))
        {
            SetCash(_upgradeVm.CurrentCash);
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

    public void SetStoneLimit(int curCargoLimit)
    {
        if (Text_StoneLimit != null)
        {
            Text_StoneLimit.text = curCargoLimit.ToString();
        }
    }
}
