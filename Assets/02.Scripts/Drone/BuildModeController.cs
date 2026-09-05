using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class BuildModeController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DroneTargetCursor _cursor;
    [SerializeField] private DroneStateMachine _stateMachine;

    [Header("입력")]
    [SerializeField] private Key _buildModeKey = Key.W;
    [SerializeField] private Key _exitKey = Key.E;

    [Header("레일")]
    [SerializeField] private RailType _railType = RailType.Straight;
    [SerializeField] private bool _keepModeAfterPlace = true;

    public bool IsBuildMode { get { return _isBuildMode; } }
    public bool HasResourceUnderCursor { get { return _hasResourceUnderCursor; } }

    private const float DENIED_SFX_INTERVAL = 1f;

    private RailSlotViewModel _subscribedSlot;
    private bool _isBuildMode;
    private bool _hasResourceUnderCursor;
    private bool _wasPlaceModeActive;

    private void Update()
    {
        SyncRailSlotSubscription();
        SyncRailPlaceMode();
        HandleToggle();

        if (_isBuildMode == false)
        {
            return;
        }

        UpdateArbitration();
        HandleMiningClick();
    }

    private void OnDisable()
    {
        UnsubscribeRailSlot();

        if (_isBuildMode == false)
        {
            return;
        }

        ExitBuildMode();
    }

    private void SyncRailSlotSubscription()
    {
        RailSlotViewModel slot = GetRailSlot();

        if (slot == _subscribedSlot)
        {
            return;
        }

        UnsubscribeRailSlot();

        _subscribedSlot = slot;

        if (_subscribedSlot == null)
        {
            return;
        }

        _subscribedSlot.PropertyChanged += HandleRailSlotChanged;
    }

    private void UnsubscribeRailSlot()
    {
        if (_subscribedSlot == null)
        {
            return;
        }

        _subscribedSlot.PropertyChanged -= HandleRailSlotChanged;
        _subscribedSlot = null;
    }

    private RailSlotViewModel GetRailSlot()
    {
        if (NetworkRailService.Instance == null)
        {
            return null;
        }

        return NetworkRailService.Instance.GetLocalRailBuildViewModel().GetSlot(_railType);
    }

    private void HandleRailSlotChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RailSlotViewModel.OwnedCount))
        {
            return;
        }

        if (_isBuildMode == false)
        {
            return;
        }

        TryResumePlacement();

        if (RailManager.Instance != null)
        {
            RailManager.Instance.RefreshHoverOutline();
        }
    }

    private void SyncRailPlaceMode()
    {
        if (RailManager.Instance == null)
        {
            return;
        }

        bool isPlaceModeActive = RailManager.Instance.IsPlaceModeActive;

        if (_isBuildMode == false)
        {
            if (isPlaceModeActive == true)
            {
                _isBuildMode = true;
            }

            _wasPlaceModeActive = isPlaceModeActive;

            return;
        }

        bool endedByRail = (_wasPlaceModeActive == true && isPlaceModeActive == false);

        if (endedByRail == true)
        {
            if (_keepModeAfterPlace == true)
            {
                ReloadRail();
            }
            else
            {
                _isBuildMode = false;
                _hasResourceUnderCursor = false;
            }
        }

        _wasPlaceModeActive = RailManager.Instance.IsPlaceModeActive;
    }

    private void ReloadRail()
    {
        TryStartPlacement();
    }

    private bool TryStartPlacement()
    {
        if (RailManager.Instance == null)
        {
            return false;
        }

        RailManager.Instance.EnterPlaceModeExternal(_railType);

        return RailManager.Instance.IsPlaceModeActive;
    }

    private void HandleToggle()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (_isBuildMode == true && Keyboard.current[_exitKey].wasPressedThisFrame == true)
        {
            ExitBuildMode();

            return;
        }

        if (Keyboard.current[_buildModeKey].wasPressedThisFrame == false)
        {
            return;
        }

        if (_isBuildMode == true)
        {
            if (TryResumePlacement() == true)
            {
                return;
            }

            ExitBuildMode();

            return;
        }

        EnterBuildMode();
    }

    private bool TryResumePlacement()
    {
        if (RailManager.Instance == null || RailManager.Instance.IsPlaceModeActive == true)
        {
            return false;
        }

        return TryStartPlacement();
    }

    private void EnterBuildMode()
    {
        _isBuildMode = true;
        _hasResourceUnderCursor = false;

        if (TryStartPlacement() == true)
        {
            return;
        }

        SoundManager.Instance?.PlaySFXThrottled(SfxAddress.Ui.Denied, DENIED_SFX_INTERVAL);
    }

    private void ExitBuildMode()
    {
        _isBuildMode = false;
        _hasResourceUnderCursor = false;
        _wasPlaceModeActive = false;

        if (RailManager.Instance != null)
        {
            RailManager.Instance.SetHoverSuppressed(false);
            RailManager.Instance.ExitPlaceModeExternal();
        }
    }

    private void UpdateArbitration()
    {
        _hasResourceUnderCursor = false;

        if (_cursor != null)
        {
            _hasResourceUnderCursor = _cursor.TryGetCell(out Vector3 _, out MaterialObject _);
        }

        bool isPointerOverUI = false;

        if (EventSystem.current != null)
        {
            isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }

        if (RailManager.Instance != null)
        {
            RailManager.Instance.SetHoverSuppressed(_hasResourceUnderCursor || isPointerOverUI);
        }
    }

    private void HandleMiningClick()
    {
        if (Mouse.current == null || _cursor == null)
        {
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame == false)
        {
            return;
        }

        if (_cursor.TryGetCell(out Vector3 _, out MaterialObject target) == false)
        {
            return;
        }

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.TryAssignMining(target);

            return;
        }

        if (_stateMachine != null)
        {
            _stateMachine.Assign(target);
        }
    }
}
