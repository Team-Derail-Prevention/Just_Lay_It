using UnityEngine;

public interface IAgentMover
{
    Vector3 CurrentVelocity { get; }
    Vector3 CurrentAcceleration { get; }

    void Move(Vector3 direction, float speed);

    void Warp(Vector3 position);
}
