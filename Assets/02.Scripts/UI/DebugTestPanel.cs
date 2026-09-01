using UnityEngine;
using UnityEngine.InputSystem;

public class DebugTestPanel : MonoBehaviour
{
    private void Update()
    {
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
    }
}
