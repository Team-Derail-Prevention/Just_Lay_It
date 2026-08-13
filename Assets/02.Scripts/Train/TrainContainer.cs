using UnityEngine;



public enum ContainerLevel
{
    Empty = -1,
    Low = 0,
    Medium = 1,
    Full = 2
}

public class TrainContainer : MonoBehaviour
{
    [Header("Container Visual")]
    [SerializeField] private GameObject[] _containerLevelObject;

    [Header("Container Setting")]
    [SerializeField] private float _maxCargo = 100f;
    [SerializeField] private float _currentAmount = 0f;

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

    public float MaxCargo
    {
        get { return _maxCargo; }
    }

    public ContainerLevel CurrentLevel
    {
        get { return _currentLevel; }
    }



    private void Start()
    {
        RefreshVisual();
    }

 
 
    private void RefreshVisual()
    {
        float ratio = Mathf.Clamp01(_currentAmount / _maxCargo);

        _currentLevel = CalculateContainerLevel(ratio);

        SetVisualLevel(_currentLevel);
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

    //자재수량 특정 값 강제설정때 사용 (정산, 세션초기화, 테스트용)
    public void SetCargoAmount(float amount)
    {
        _currentAmount = Mathf.Clamp(amount, 0f, _maxCargo);
        RefreshVisual();
    }
}
