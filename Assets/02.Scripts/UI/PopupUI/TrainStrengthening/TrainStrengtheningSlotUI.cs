using UnityEngine;
using System;
using System.ComponentModel;
using TMPro;
using UnityEngine.UI;

public class TrainStrengtheningSlotUI : MonoBehaviour
{
    [SerializeField] private Image Image_Icon;
    [SerializeField] private TextMeshProUGUI Text_Name;
    [SerializeField] private Image[] Image_LevelPipArray;
    [SerializeField] private TextMeshProUGUI Text_Level;
    [SerializeField] private TextMeshProUGUI Text_Cost;
    [SerializeField] private GameObject GameObject_Selected;
    [SerializeField] private UIButton Button_Select;

    private TrainStatSlotViewModel _viewModel;
    private Action<string> _onClickSelect;

    public string GetSlotDataId()
    {
        return _viewModel?.SlotDataId;
    }


    private void OnEnable()
    {
        if (Button_Select != null)
        {
            Button_Select.BindOnClickButtonEvent(OnClick_Select);
        }
    }

    private void OnDisable()
    {
        if (Button_Select != null)
        {
            Button_Select.UnBindOnClickButtonEvent(OnClick_Select);
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnPropertyChanged_View;
        }
    }

    public void InitSlot(TrainStatSlotViewModel viewModel, Action<string> onClickSelect)
    {
        _viewModel = viewModel;
        _onClickSelect = onClickSelect;
        _viewModel.PropertyChanged += OnPropertyChanged_View;

        RefreshAll();
    }

    private void OnPropertyChanged_View(object sender, PropertyChangedEventArgs e)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (Text_Name != null)
        {
            Text_Name.text = _viewModel.DisplayName;
        }

        if (Text_Level != null)
        {
            Text_Level.text = $"Lv.{_viewModel.CurrentLevel}";
        }

        if (Text_Cost != null)
        {
            Text_Cost.text = _viewModel.IsMaxLevel ? "MAX" : _viewModel.NextCost.ToString();
        }

        if (Image_LevelPipArray != null)
        {
            for (int i = 0; i < Image_LevelPipArray.Length; i++)
            {
                bool isFilled = (i < _viewModel.CurrentLevel);
                Image_LevelPipArray[i].gameObject.SetActive(isFilled);
            }
        }
    }
    
    public void SetSelected(bool isSelected)
    {
        if (GameObject_Selected != null)
        {
            GameObject_Selected.SetActive(isSelected);
        }
    }

    private void OnClick_Select()
    {
        if (_viewModel.IsMaxLevel == true)
        {
            return;
        }

        _onClickSelect?.Invoke(_viewModel.SlotDataId);
    }
}
