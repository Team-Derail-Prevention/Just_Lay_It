using UnityEngine;

public abstract class DebuffMonsterBase : MonoBehaviour
{
    [Header("Debuff Settings")]
    [SerializeField] protected float _debuffDuration = 5f;
    [SerializeField] protected float _debuffPower = 10f;

    protected Transform _targetTrain;

    public virtual void Initialize(Transform targetTrain)
    {
        _targetTrain = targetTrain;
    }

    public abstract void ApplyDebuff();

    protected void TriggerDebuffEvent()
    {
        Debug.Log($"[{gameObject.name}] 특수 능력 발동");
        ApplyDebuff();
    }
}
