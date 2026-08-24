using System;
using UnityEngine;

public class CentralTerminal : BaseColliderTrigger
{
    public static event Action<CentralTerminal> OnCentralTerminalEntered;
    public static event Action<int> OnExitDirectionSelected;

    [Header("중앙역 창고")]
    [SerializeField] private int _storedMaterialCount = 0;

    [System.Serializable]
    public struct RailSpawnInfo
    {
        [Tooltip("레일의 월드 좌표 위치")]
        public Vector3 position;
        [Tooltip("레일의 회전값")]
        public Quaternion rotation;
    }

    [Header("스폰 포인트 (인게임 자동 등록 및 확인용)")]
    [SerializeField] private RailSpawnInfo[] _startPoints = new RailSpawnInfo[4];

    public int StoredMaterialCount => _storedMaterialCount;
    public Transform[] ExitDirRoots = new Transform[4];

    public void RegisterStartPoint(int index, Vector3 pos, Quaternion rot)
    {
        if (index >= 0 && index < _startPoints.Length)
        {
            _startPoints[index].position = pos;
            _startPoints[index].rotation = rot;
        }
    }

    public RailSpawnInfo GetStartPoint(int index)
    {
        return _startPoints[index];
    }

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

        RailSpawnInfo selectedRailInfo = GetStartPoint(directionIndex);
        Debug.Log($"[CentralTerminal] 출구 방향 {directionIndex} 선택됨! 스폰 위치: {selectedRailInfo.position}, 회전: {selectedRailInfo.rotation.eulerAngles}");

    }

    public void RegisterExitDirRoot(int dirIndex, Transform dirRoot)
    {
        if (dirIndex >= 0 && dirIndex < ExitDirRoots.Length)
        {
            ExitDirRoots[dirIndex] = dirRoot;
        }
    }
}