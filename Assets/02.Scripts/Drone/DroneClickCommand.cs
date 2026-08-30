using UnityEngine;
using UnityEngine.InputSystem;

public class DroneClickCommand : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DroneStateMachine _stateMachine;

    [Header("입력")]
    [SerializeField] private Key _recallKey = Key.V;

    private void Update()
    {
        if (_stateMachine == null && DroneManager.Instance == null)
        {
            return;
        }

        HandleRecall();
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

        if (DroneManager.Instance != null)
        {
            DroneManager.Instance.RecallAll();

            return;
        }

        _stateMachine.Recall();
    }
}
