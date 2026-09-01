using UnityEngine;

public class DebugTestPanel : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkResourceService.Instance.AddWood(10);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NetworkResourceService.Instance.AddStone(10);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            NetworkUpgradeService.Instance.GainCash(100);
        }
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F9))
        {
            GameManager.Instance.Debug_ForceGameClearInTime();
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            GameManager.Instance.Debug_ForceGameClearOverTime();
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            SaveManager.Instance.Debug_ResetFirstPlayNotice();
        }
#endif
    }
}
