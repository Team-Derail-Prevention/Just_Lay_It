using UnityEngine;

public abstract class BaseColliderTrigger : MonoBehaviour
{
    [Header("상호작용 대상 설정")]
    [SerializeField] protected string _targetTag = "Train";

    protected bool _isTriggerCollider = false;

    protected virtual void OnEnable()
    {

    }

    protected virtual void OnDisable()
    {

    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_targetTag))
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
        if (other.CompareTag(_targetTag))
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