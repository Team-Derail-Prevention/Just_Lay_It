using System.Collections.Generic;
using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance { get; private set; }

    [Header("Rail Detect Setting")]
    private string _railTag = "Rail";
    private float _detectRadius = 1.0f;

    [Header("Station Detect Setting")]
    private string _stationTag = "Station";
    private float _stationDetectRadius = 1.0f;

    [Header("Total Rail Path Data")]
    public List<Transform> pathList = new List<Transform>();

    public bool IsStation { get; private set; } = false;

    private HashSet<Transform> visitedNode = new HashSet<Transform>();
    private HashSet<Transform> visitedStation = new HashSet<Transform>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void DetectRail(Vector3 currentPos)
    {
        Collider[] hits = Physics.OverlapSphere(currentPos, _detectRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(_railTag))
            {
                Transform railTransform = hits[i].transform;

                if (!visitedNode.Contains(railTransform))
                {
                    visitedNode.Add(railTransform);
                    pathList.Add(railTransform);
                }
            }
        }
    }

    public Transform GetWaypoint(int index)
    {
        if (index >= 0 && index < pathList.Count)
        {
            return pathList[index];
        }
        return null;
    }

    public void DetectStation(Vector3 currentPos)
    {
        if (IsStation)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(currentPos, _stationDetectRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(_stationTag))
            {
                Transform stationTransform = hits[i].transform;
                if (!visitedStation.Contains(stationTransform))
                {
                    visitedStation.Add(stationTransform);
                    ArriveStation(stationTransform.gameObject);
                    break;
                }
            }
        }
    }

    public void ArriveStation(GameObject stationObj)
    {
        IsStation = true;
        Debug.Log("기차역 도착");
    }

    public void DepartStation(GameObject stationObj)
    {
        IsStation = false;
        Debug.Log("기차역 출발");
    }

}
