using UnityEngine;

public abstract class BaseColliderTrigger : MonoBehaviour
{
    public enum TargetType
    {
        None = 0,
        Train,
        Drone,
        Monster,
    }

    [Header("상호작용 대상 설정")]
    [SerializeField] protected TargetType _targetType;
    protected bool _isTriggerCollider = false;

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_targetType.ToString()))
        {
            _isTriggerCollider = true;

            if (CanInteract(other))
            {
                HandleInteraction(other);
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_targetType.ToString()))
        {
            _isTriggerCollider = false;
            HandleExit(other);
        }
    }

    protected virtual bool CanInteract(Collider target)
    {
        return true;
    }

    protected abstract void HandleInteraction(Collider target);

    protected virtual void HandleExit(Collider target)
    {

    }
}