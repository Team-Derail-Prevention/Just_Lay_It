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

    private bool _isBuildMode;
    private bool _hasResourceUnderCursor;
    private bool _wasPlaceModeActive;

    private void Update()
    {
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
        if (_isBuildMode == false)
        {
            return;
        }

        ExitBuildMode();
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
        if (NetworkRailService.Instance == null)
        {
            return;
        }

        NetworkRailService.Instance.RequestStartPlacement(_railType);
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
            ExitBuildMode();

            return;
        }

        EnterBuildMode();
    }

    private void EnterBuildMode()
    {
        _isBuildMode = true;
        _hasResourceUnderCursor = false;

        if (NetworkRailService.Instance != null)
        {
            NetworkRailService.Instance.RequestStartPlacement(_railType);
        }
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
