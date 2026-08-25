using UnityEngine;

public class DebugTestPanel : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            UIManager.Instance.OpenBaseArrivalUI();
        }

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
    }
}
