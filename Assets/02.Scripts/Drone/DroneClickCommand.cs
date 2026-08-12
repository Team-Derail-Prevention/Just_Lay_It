using UnityEngine;
using UnityEngine.InputSystem;

public class DroneClickCommand : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GridCursor _cursor;
    [SerializeField] private DroneStateMachine _stateMachine;

    [Header("입력")]
    [SerializeField] private Key _orderModeKey = Key.B;
    [SerializeField] private Key _recallKey = Key.R;

    public bool IsOrderMode { get { return _isOrderMode; } }

    private bool _isOrderMode;

    private void Update()
    {
        if (Mouse.current == null || _cursor == null || _stateMachine == null)
        {
            return;
        }

        HandleOrderModeToggle();
        HandleRecall();

        if (_isOrderMode == false)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame == false)
        {
            return;
        }

        if (_cursor.TryGetCell(out CellPos cell) == false)
        {
            return;
        }

        _stateMachine.Assign(cell);
    }

    private void HandleOrderModeToggle()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[_orderModeKey].wasPressedThisFrame == false)
        {
            return;
        }

        _isOrderMode = !_isOrderMode;
    }

    private void HandleRecall()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[_recallKey].wasPressedThisFrame == false)
        {
            return;
        }

        _stateMachine.Recall();
    }
}
