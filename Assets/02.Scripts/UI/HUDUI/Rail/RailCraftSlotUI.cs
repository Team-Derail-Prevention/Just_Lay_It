using UnityEngine;
using System;
using System.ComponentModel;
using TMPro;
using UnityEngine.UI;

public class RailCraftSlotUI : MonoBehaviour
{
    [SerializeField] private Image Image_Icon;
    [SerializeField] private Image Image_RadialProgress;
    [SerializeField] private TextMeshProUGUI Text_QueueCount;
    [SerializeField] private UIButton Button_Craft;

    private RailSlotViewModel _viewModel;
    private Action<RailType> _onClickCraft;

    private void OnEnable()
    {
        if (Button_Craft != null)
        {
            Button_Craft.BindOnClickButtonEvent(OnClick_Craft);
        }
    }

    private void OnDisable()
    {
        if (Button_Craft != null)
        {
            Button_Craft.UnBindOnClickButtonEvent(OnClick_Craft);
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnPropertyChanged_View;
        }
    }

    public void InitSlot(RailSlotViewModel viewModel, Action<RailType> onClickCraft)
    {
        _viewModel = viewModel;
        _onClickCraft = onClickCraft;
        _viewModel.PropertyChanged += OnPropertyChanged_View;

        RefreshQueueCount();
        RefreshProgress();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RailSlotViewModel.CraftQueueCount))
        {
            RefreshQueueCount();
        }
        else if (e.PropertyName == nameof(RailSlotViewModel.CraftProgress01))
        {
            RefreshProgress();
        }
    }

    private void RefreshQueueCount()
    {
        if (Text_QueueCount != null)
        {
            Text_QueueCount.text = _viewModel.CraftQueueCount.ToString();
        }
    }

    private void RefreshProgress()
    {
        if (Image_RadialProgress != null)
        {
            Image_RadialProgress.fillAmount = _viewModel.CraftProgress01;
        }
    }

    private void OnClick_Craft()
    {
        _onClickCraft?.Invoke(_viewModel.RailType);
    }
}
