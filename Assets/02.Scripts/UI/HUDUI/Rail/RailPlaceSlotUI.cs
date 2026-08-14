using UnityEngine;
using System;
using System.ComponentModel;
using TMPro;
using UnityEngine.UI;

public class RailPlaceSlotUI : MonoBehaviour
{
    [SerializeField] private Image Image_Icon;
    [SerializeField] private TextMeshProUGUI Text_OwnedCount;
    [SerializeField] private UIButton Button_Place;

    private RailSlotViewModel _viewModel;
    private Action<ERailType> _onClickPlace;

    private void OnEnable()
    {
        if (Button_Place != null)
        {
            Button_Place.BindOnClickButtonEvent(OnClick_Place);
        }
    }

    private void OnDisable()
    {
        if (Button_Place != null)
        {
            Button_Place.UnBindOnClickButtonEvent(OnClick_Place);
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnPropertyChanged_View;
        }
    }

    public void InitSlot(RailSlotViewModel viewModel, Action<ERailType> onClickPlace)
    {
        _viewModel = viewModel;
        _onClickPlace = onClickPlace;
        _viewModel.PropertyChanged += OnPropertyChanged_View;

        RefreshOwnedCount();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RailSlotViewModel.OwnedCount))
        {
            RefreshOwnedCount();
        }
    }

    private void RefreshOwnedCount()
    {
        if (Text_OwnedCount != null)
        {
            Text_OwnedCount.text = _viewModel.OwnedCount.ToString();
        }
    }

    private void OnClick_Place()
    {
        _onClickPlace?.Invoke(_viewModel.RailType);
    }
}
