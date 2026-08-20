using UnityEngine;
using Enums;

public class TrainDepartureUI : UIBase
{
    [Header("방향 버튼")]
    [SerializeField] private DirectionButtonUI EastButtonUI;
    [SerializeField] private DirectionButtonUI WestButtonUI;
    [SerializeField] private DirectionButtonUI SouthButtonUI;
    [SerializeField] private DirectionButtonUI NorthButtonUI;

    [Header("출발 / 취소")]
    [SerializeField] private UIButton Button_Start;
    [SerializeField] private UIButton Button_Exit;

    private TrainExitDirection? _selectedDirection;

    private void OnEnable()
    {
        if (EastButtonUI != null)
        {
            EastButtonUI.InitButton(OnClick_East);
        }

        if (WestButtonUI != null)
        {
            WestButtonUI.InitButton(OnClick_West);
        }

        if (SouthButtonUI != null)
        {
            SouthButtonUI.InitButton(OnClick_South);
        }

        if (NorthButtonUI != null)
        {
            NorthButtonUI.InitButton(OnClick_North);
        }

        if (Button_Start != null)
        {
            Button_Start.BindOnClickButtonEvent(OnClick_Start);
        }

        if (Button_Exit != null)
        {
            Button_Exit.BindOnClickButtonEvent(OnClick_Exit);
        }

        _selectedDirection = null; 
        RefreshButtonStates();
    }

    private void OnDisable()
    {
        if (Button_Start != null)
        {
            Button_Start.UnBindOnClickButtonEvent(OnClick_Start);
        }

        if (Button_Exit != null)
        {
            Button_Exit.UnBindOnClickButtonEvent(OnClick_Exit);
        }
    }

    private void OnClick_East()
    {
        ToggleDirection(TrainExitDirection.East);
    }

    private void OnClick_West()
    {
        ToggleDirection(TrainExitDirection.West);
    }

    private void OnClick_South()
    {
        ToggleDirection(TrainExitDirection.South);
    }

    private void OnClick_North()
    {
        ToggleDirection(TrainExitDirection.North);
    }

    private void ToggleDirection(TrainExitDirection direction)
    {
        if (_selectedDirection == direction)
        {
            _selectedDirection = null;
        }
        else
        {
            _selectedDirection = direction;
        }

        RefreshButtonStates();
    }

    private void RefreshButtonStates()
    {
        if (EastButtonUI != null)
        {
            EastButtonUI.SetSelected(_selectedDirection == TrainExitDirection.East);
        }

        if (WestButtonUI != null)
        {
            WestButtonUI.SetSelected(_selectedDirection == TrainExitDirection.West);
        }

        if (SouthButtonUI != null)
        {
            SouthButtonUI.SetSelected(_selectedDirection == TrainExitDirection.South);
        }

        if (NorthButtonUI != null)
        {
            NorthButtonUI.SetSelected(_selectedDirection == TrainExitDirection.North);
        }
    }

    private void OnClick_Start()
    {
        if (_selectedDirection == null)
        {
            Debug.LogWarning("[TrainDepartureUI] 방향을 먼저 선택해주세요.");
            return;
        }

        int directionIndex = (int)_selectedDirection.Value;

        UIManager.Instance.CloseTrainDepartureUI();
        GameManager.Instance.SelectExitDirection(directionIndex);
    }

    private void OnClick_Exit()
    {
        UIManager.Instance.CloseTrainDepartureUI();
    }
}
