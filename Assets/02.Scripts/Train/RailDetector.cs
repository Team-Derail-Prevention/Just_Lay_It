using System;
using UnityEngine;

public class RailDetector : MonoBehaviour
{
    [SerializeField] private string _railTag = "Rail";
    [SerializeField] private string _stationTag = "Station";

    public event Action<Transform> OnRailDetected;
    public event Action<GameObject> OnStationDetected;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_railTag))
        {
            if (!other.enabled)
            {
                return;
            }

            OnRailDetected?.Invoke(other.transform);
        }
        else if (other.CompareTag(_stationTag))
        {
            OnStationDetected?.Invoke(other.gameObject);
        }
    }
}
