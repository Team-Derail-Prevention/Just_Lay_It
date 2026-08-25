using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine.UI;

public class LobbyUpgradeUI : UIBase
{
    [Header("상단 골드 정보")]
    [SerializeField] private Image Image_CashIcon;
    [SerializeField] private TextMeshProUGUI Text_CashAmount;

    [Header("슬롯 리스트 영역")]
    [SerializeField] private GameObject Prefab_Slot;
    [SerializeField] private Transform Transform_SlotRoot;

    [Header("하단 상세정보 패널")]
    [SerializeField] private Image Image_DetailIcon;
    [SerializeField] private TextMeshProUGUI Text_DetailTitle;
    [SerializeField] private TextMeshProUGUI Text_DetailDescription;

    [Header("하단 버튼")]
    [SerializeField] private UIButton Button_Back;
    [SerializeField] private UIButton Button_Buy;
    [SerializeField] private UIButton Button_Refund;
    [SerializeField] private UIButton Button_RefundAll;

    private UpgradeViewModel _vm;
    private Dictionary<string, LobbyUpgradeSlotUI> _slotList = new Dictionary<string, LobbyUpgradeSlotUI>();
    private string _curSelectedSlotId;

    private void OnEnable()
    {
        if (Button_Back != null) Button_Back.BindOnClickButtonEvent(OnClick_Back);
        if (Button_Buy != null) Button_Buy.BindOnClickButtonEvent(OnClick_Buy);
        if (Button_Refund != null) Button_Refund.BindOnClickButtonEvent(OnClick_Refund);
        if (Button_RefundAll != null) Button_RefundAll.BindOnClickButtonEvent(OnClick_RefundAll);

        _vm = NetworkUpgradeService.Instance.GetLocalUpgradeViewModel();
        _vm.PropertyChanged += OnPropertyChanged_View;

        RefreshCashText();
        CreateAllSlots();
    }

    private void OnDisable()
    {
        if (Button_Back != null) Button_Back.UnBindOnClickButtonEvent(OnClick_Back);
        if (Button_Buy != null) Button_Buy.UnBindOnClickButtonEvent(OnClick_Buy);
        if (Button_Refund != null) Button_Refund.UnBindOnClickButtonEvent(OnClick_Refund);
        if (Button_RefundAll != null) Button_RefundAll.UnBindOnClickButtonEvent(OnClick_RefundAll);

        if (_vm != null)
        {
            _vm.PropertyChanged -= OnPropertyChanged_View;
        }

        OnDestroyAndClearSlotList();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpgradeViewModel.CurrentCash))
        {
            RefreshCashText();
        }
        else if (e.PropertyName == "SlotListAdded")
        {
            CreateAllSlots();
        }
    }

    private void OnDestroyAndClearSlotList()
    {
        if (_slotList.Count <= 0)
        {
            return;
        }

        foreach (var slotKv in _slotList)
        {
            var slot = slotKv.Value;
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        _slotList.Clear();
        _curSelectedSlotId = null;
    }

    private void RefreshCashText()
    {
        if (Text_CashAmount != null)
        {
            Text_CashAmount.text = _vm.CurrentCash.ToString();
        }
    }

    private void CreateAllSlots()
    {
        OnDestroyAndClearSlotList();

        foreach (var slotKv in _vm.SlotDic)
        {
            CreateUpgradeSlot(slotKv.Value);
        }
    }

    private void CreateUpgradeSlot(UpgradeSlotViewModel slotViewModel)
    {
        var gObj = Instantiate(Prefab_Slot, Transform_SlotRoot);
        if (gObj == null)
        {
            return;
        }

        var slotComponent = gObj.GetComponent<LobbyUpgradeSlotUI>();
        if (slotComponent == null)
        {
            return;
        }

        slotComponent.InitSlot(slotViewModel, OnClickSlotSelected);
        _slotList.Add(slotViewModel.SlotDataId, slotComponent);
    }

    private void OnClickSlotSelected(string slotDataId)
    {
        _curSelectedSlotId = slotDataId;

        // 내용 정해지면 추후 수정

        foreach (var slotKv in _slotList)
        {
            var slot = slotKv.Value;
            slot.SetSelectedUI(slotKv.Key == _curSelectedSlotId);
        }
    }

    public void OnClick_Back()
    {
        UIManager.Instance.CloseLobbyUpgradeUI();
    }

    public void OnClick_Buy()
    {
        if (string.IsNullOrEmpty(_curSelectedSlotId))
        {
            return;
        }

        NetworkUpgradeService.Instance.RequestPurchase(_curSelectedSlotId);
    }

    public void OnClick_Refund()
    {
        if (string.IsNullOrEmpty(_curSelectedSlotId))
        {
            return;
        }

        NetworkUpgradeService.Instance.RequestRefund(_curSelectedSlotId);
    }

    public void OnClick_RefundAll()
    {
        NetworkUpgradeService.Instance.RequestRefundAll();
    }
}
