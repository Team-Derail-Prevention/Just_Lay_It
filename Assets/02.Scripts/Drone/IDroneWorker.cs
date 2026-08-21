using UnityEngine;

public interface IDroneWorker
{
    DroneState State { get; }
    Transform Transform { get; }

    bool TryGetWorkTopY(out float topY);
    void Recall();
}
