using UnityEngine;
using Enums;


public class TrainContainer : MonoBehaviour
{
    [Header("Container Visual")]
    [SerializeField] private GameObject[] _containerLevelObject;

    [Header("Container Setting")]
    [SerializeField] private float _currentAmount = 0f;

    private float _maxCargo = 500f;
    private int _cachedWood = 0;
    private int _cachedStone = 0;
    private ContainerLevel _currentLevel = ContainerLevel.Empty;

    //테스트
    [ContextMenu("Test / Set Cargo - 0 (Empty)")]
    private void TestSetCargoEmpty() => SetCargoAmount(0f);

    [ContextMenu("Test / Set Cargo - 20 (Low)")]
    private void TestSetCargoLow() => SetCargoAmount(20f);

    [ContextMenu("Test / Set Cargo - 50 (Medium)")]
    private void TestSetCargoMedium() => SetCargoAmount(50f);

    [ContextMenu("Test / Set Cargo - 100 (Full)")]
    private void TestSetCargoFull() => SetCargoAmount(100f);
    //

    public float CurrentAmount
    {
        get { return _currentAmount; }
    }

    public ContainerLevel CurrentLevel
    {
        get { return _currentLevel; }
    }

    private void OnEnable()
    {
        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged += OnWoodChanged;
            ResourceStatusEventHub.Instance.OnStoneChanged += OnStoneChanged;
        }

        if (NetworkResourceService.Instance != null)
        {
            var resourceVm = NetworkResourceService.Instance.GetLocalResourceViewModel();
            if (resourceVm != null)
            {
                _cachedWood = resourceVm.CurrentWood;
                _cachedStone = resourceVm.CurrentStone;
                UpdateTotalCargoVisual();
            }
        }
        else
        {
            RefreshVisual();
        }
    }

    private void OnDisable()
    {
        if (ResourceStatusEventHub.Instance != null)
        {
            ResourceStatusEventHub.Instance.OnWoodChanged -= OnWoodChanged;
            ResourceStatusEventHub.Instance.OnStoneChanged -= OnStoneChanged;
        }
    }

    public void ContainerInit()
    {
        UpdateTotalCargoVisual();
    }

    private void OnWoodChanged(int newWoodCount)
    {
        _cachedWood = newWoodCount;
        UpdateTotalCargoVisual();

    }

    private void OnStoneChanged(int newStoneCount)
    {
        _cachedStone = newStoneCount;
        UpdateTotalCargoVisual();

    }

    private void RefreshVisual()
    {
        float ratio = Mathf.Clamp01(_currentAmount / _maxCargo);

        _currentLevel = CalculateContainerLevel(ratio);

        SetVisualLevel(_currentLevel);
    }

    private void UpdateTotalCargoVisual()
    {
        _currentAmount = Mathf.Clamp(_cachedWood + _cachedStone, 0f, _maxCargo);
        RefreshVisual();
    }

    private ContainerLevel CalculateContainerLevel(float ratio)
    {
        if (ratio <= 0f)
        {
            return ContainerLevel.Empty;
        }
        else if (ratio < 0.33f)
        {
            return ContainerLevel.Low;
        }
        else if (ratio < 0.66f)
        {
            return ContainerLevel.Medium;
        }
        else
        {
            return ContainerLevel.Full;
        }
    }

    private void SetVisualLevel(ContainerLevel level)
    {
        if (_containerLevelObject == null)
        {
            return;
        }

        int targetIndex = (int)level;

        for (int i = 0; i < _containerLevelObject.Length; i++)
        {
            if (_containerLevelObject[i] != null)
            {
                _containerLevelObject[i].SetActive(i == targetIndex);
            }
        }
    }

    public void AddCargo(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _currentAmount = Mathf.Clamp(_currentAmount + amount, 0f, _maxCargo);

        RefreshVisual();
    }

    public bool UseCargo(float amount)
    {
        if (amount <= 0f)
        {
            return false;
        }

        if (_currentAmount < amount)
        {
            Debug.LogWarning("[TrainContainer] 자재 수량이 부족하여 사용할 수 없습니다.");
            return false;
        }

        _currentAmount -= amount;

        RefreshVisual();

        return true;
    }

    public void UseSpecificCargo(string resourceType, float amount)
    {
        int stealAmount = Mathf.RoundToInt(amount);

        if (resourceType == "Wood")
        {
            NetworkResourceService.Instance.TrySpendWood(stealAmount);
            Debug.Log($"도둑 로봇이 나무를 {stealAmount}만큼 훔침");
        }
        else if (resourceType == "Stone")
        {
            NetworkResourceService.Instance.TrySpendStone(stealAmount);
            Debug.Log($"도둑 로봇이 돌을 {stealAmount}만큼 훔침");
        }
    }

    //자재수량 특정 값 강제설정때 사용 (정산, 세션초기화, 테스트용)
    public void SetCargoAmount(float amount)
    {
        _currentAmount = Mathf.Clamp(amount, 0f, _maxCargo);
        RefreshVisual();
    }
}
