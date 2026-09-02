using UnityEngine;
using UnityEngine.InputSystem;
using Enums;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugTestPanel : MonoBehaviour
{
    private void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current == null)
        {
            return;
        }

        if(Keyboard.current.digit1Key.wasPressedThisFrame == true)
        {
            NetworkResourceService.Instance.AddWood(10);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame == true)
        {
            NetworkResourceService.Instance.AddStone(10);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame == true)
        {
            NetworkUpgradeService.Instance.GainCash(100);
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame == true)
        {
            GameManager.Instance.Debug_ForceGameClearInTime();
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame == true)
        {
            GameManager.Instance.Debug_ForceGameClearOverTime();
        }

        if (Keyboard.current.f11Key.wasPressedThisFrame == true)
        {
            SaveManager.Instance.Debug_ResetFirstPlayNotice();
        }

        if (Keyboard.current.f12Key.wasPressedThisFrame == true)
        {
            SaveManager.Instance.Debug_ResetAllProgressData();
        }

        HandleStageCheatKeys();
    }

    private void HandleStageCheatKeys()
    {
        if (GameManager.Instance.CurrentGameState != GameState.Ready)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            GameManager.Instance.SetGameStageForCheat(GameStage.Stage1);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            GameManager.Instance.SetGameStageForCheat(GameStage.Stage2);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            GameManager.Instance.SetGameStageForCheat(GameStage.Stage3);
        }
    }
#endif
}
