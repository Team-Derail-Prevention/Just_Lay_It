using System;
using UnityEngine;

public class DeliveryOrder
{
    public GameObject Payload { get; private set; }
    public Vector3 Target { get; private set; }
    public Quaternion Rotation { get; private set; }

    public event Action OnDelivered;
    public event Action OnCancelled;

    public DeliveryOrder(GameObject payload, Vector3 target, Quaternion rotation)
    {
        Payload = payload;
        Target = target;
        Rotation = rotation;
    }

    public void Complete()
    {
        OnDelivered?.Invoke();
    }

    public void Cancel()
    {
        OnCancelled?.Invoke();
    }
}
