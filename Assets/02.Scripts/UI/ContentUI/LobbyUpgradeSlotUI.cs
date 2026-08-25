using UnityEngine;
using System;
using System.ComponentModel;
using TMPro;
using UnityEngine.UI;

public class LobbyUpgradeSlotUI : MonoBehaviour
{
    [Header("아이콘")]
    [SerializeField] private Image Image_Icon;

    [Header("업글 표시")]
    [SerializeField] private Image[] Image_LevelPipArray;

    [Header("가격 정보")]
    [SerializeField] private Image Image_CashIcon;
    [SerializeField] private TextMeshProUGUI Text_Price;

    [Header("선택 표시")]
    [SerializeField] private GameObject GameObject_Selected;

    [Header("클릭")]
    [SerializeField] private UIButton Button_SlotClick;

    private event Action<string> _onClickSlot;
    private UpgradeSlotViewModel _viewModel;

    public string GetSlotDataId()
    {
        return _viewModel?.SlotDataId;
    }

    private void OnEnable()
    {
        if (Button_SlotClick != null)
        {
            Button_SlotClick.BindOnClickButtonEvent(OnClick_Slot);
        }
    }

    private void OnDisable()
    {
        if (Button_SlotClick != null)
        {
            Button_SlotClick.UnBindOnClickButtonEvent(OnClick_Slot);
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnPropertyChanged_View;
        }

        _onClickSlot = null;
    }

    public void OnClick_Slot()
    {
        _onClickSlot?.Invoke(_viewModel?.SlotDataId);
    }

    public void InitSlot(UpgradeSlotViewModel viewModel, Action<string> onClickCallback)
    {
        _viewModel = viewModel;
        _onClickSlot = onClickCallback;
        _viewModel.PropertyChanged += OnPropertyChanged_View;

        // 추후 내용 정해지면 수정

        RefreshAll();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UpgradeSlotViewModel.CurrentLevel):
                SetLevel(_viewModel.CurrentLevel);
                break;
            case nameof(UpgradeSlotViewModel.NextCost):
                SetPrice(_viewModel.NextCost);
                break;
        }
    }

    private void RefreshAll()
    {
        SetLevel(_viewModel.CurrentLevel);
        SetPrice(_viewModel.NextCost);
    }

    private void SetLevel(int curLevel)
    {
        if (Image_LevelPipArray == null)
        {
            return;
        }

        for (int i = 0; i < Image_LevelPipArray.Length; i++)
        {
            bool isFilled = (i < curLevel);
            Image_LevelPipArray[i].gameObject.SetActive(isFilled);
        }
    }

    private void SetPrice(int price)
    {
        if (Text_Price != null)
        {
            Text_Price.text = price.ToString();
        }
    }

    public void SetSelectedUI(bool isSelected)
    {
        if (GameObject_Selected != null)
        {
            GameObject_Selected.SetActive(isSelected);
        }
    }
}
