using UnityEngine;
using UnityEngine.InputSystem;

public class DroneClickCommand : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GridCursor _cursor;
    [SerializeField] private DroneStateMachine _stateMachine;

    [Header("입력")]
    [SerializeField] private Key _recallKey = Key.R;

    private void Update()
    {
        if (Mouse.current == null || _cursor == null || _stateMachine == null)
        {
            return;
        }

        HandleRecall();

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
