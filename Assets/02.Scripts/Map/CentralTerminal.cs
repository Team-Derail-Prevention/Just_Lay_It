using System;
using UnityEngine;

public class CentralTerminal : BaseColliderTrigger
{
    public static event Action<CentralTerminal> OnCentralTerminalEntered;
    public static event Action<int> OnExitDirectionSelected;

    [Header("중앙역 창고")]
    [SerializeField] private int _storedMaterialCount = 0;

    public int StoredMaterialCount => _storedMaterialCount;

    protected override void HandleInteraction(Collider target)
    {
        if (!CanInteract(target))
        {
            return;
        }

        OnCentralTerminalEntered?.Invoke(this);
        Debug.Log($"[CentralTerminal] 중앙역에 진입했습니다. 현재 창고에는 {_storedMaterialCount}개의 화물이 있습니다.");
    }

    public void StoreCargo(int amount)
    {
        _storedMaterialCount += amount;
        Debug.Log($"[CentralTerminal] 창고에 {amount}개의 화물이 저장되었습니다." +
            $"현재 창고에는 {_storedMaterialCount}개의 화물이 있습니다.");
    }

    public bool ConsumeRecoveryTrain(int amount)
    {
        if (_storedMaterialCount >= amount)
        {
            _storedMaterialCount -= amount;
            return true;
        }

        return false;
    }

    public void SelectExitGate(int directionIndex)
    {
        OnExitDirectionSelected?.Invoke(directionIndex);
    }
}
