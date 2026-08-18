using System;
using System.Collections.Generic;
using UnityEngine;

public class DroneManager : MonoBehaviour
{
    public static DroneManager Instance { get; private set; }

    private readonly List<IDroneWorker> _workers = new List<IDroneWorker>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DroneManager] 이미 다른 DroneManager가 있어 이 컴포넌트를 제거합니다.");

            Destroy(this);

            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Register(IDroneWorker worker)
    {
        if (worker == null)
        {
            return;
        }

        if (_workers.Contains(worker))
        {
            return;
        }

        _workers.Add(worker);
    }

    public void Unregister(IDroneWorker worker)
    {
        if (worker == null)
        {
            return;
        }

        _workers.Remove(worker);
    }

    public bool TryAssignMining(MaterialObject target)
    {
        if (target == null)
        {
            return false;
        }

        DroneStateMachine miner = FindNearestIdleMiner(target.transform.position, target);

        if (miner == null)
        {
            Debug.Log("[DroneManager] 명령을 받을 수 있는 채집 드론이 없습니다.");

            return false;
        }

        return miner.Assign(target);
    }

    public bool CanAssignMining(MaterialObject target)
    {
        if (target == null)
        {
            return false;
        }

        return FindNearestIdleMiner(target.transform.position, target) != null;
    }

    // 드론은 물건을 기차 적재칸에서 목적지로 옮기기만 합니다.
    // 무엇을 옮기는지, 도착 후 무엇을 할지는 payload와 onDelivered를 넘긴 쪽 몫입니다.
    public bool RequestDelivery(GameObject payload, Vector3 target, Quaternion rotation, Action onDelivered, Action onCancelled = null)
    {
        if (payload == null)
        {
            Debug.Log("[DroneManager] 배달 요청 거절 — 옮길 물건이 없습니다");

            return false;
        }

        DroneDeliveryWorker carrier = FindNearestIdleCarrier(target);

        if (carrier == null)
        {
            Debug.Log("[DroneManager] 배달 요청 거절 — 명령을 받을 수 있는 운반 드론이 없습니다");

            return false;
        }

        DeliveryOrder order = new DeliveryOrder(payload, target, rotation);

        if (onDelivered != null)
        {
            order.OnDelivered += onDelivered;
        }

        if (onCancelled != null)
        {
            order.OnCancelled += onCancelled;
        }

        return carrier.Assign(order);
    }

    public bool HasIdleCarrier()
    {
        return FindNearestIdleCarrier(Vector3.zero) != null;
    }

    public void RecallAll()
    {
        for (int i = 0; i < _workers.Count; i++)
        {
            if (_workers[i] == null)
            {
                continue;
            }

            _workers[i].Recall();
        }
    }

    private DroneStateMachine FindNearestIdleMiner(Vector3 near, MaterialObject target)
    {
        DroneStateMachine best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneStateMachine miner = _workers[i] as DroneStateMachine;

            if (miner == null)
            {
                continue;
            }

            if (miner.CanAssign(target) == false)
            {
                continue;
            }

            float distance = (miner.Transform.position - near).sqrMagnitude;

            if (distance >= bestDistance)
            {
                continue;
            }

            best = miner;
            bestDistance = distance;
        }

        return best;
    }

    private DroneDeliveryWorker FindNearestIdleCarrier(Vector3 near)
    {
        DroneDeliveryWorker best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneDeliveryWorker carrier = _workers[i] as DroneDeliveryWorker;

            if (carrier == null)
            {
                continue;
            }

            if (carrier.CanAcceptWork == false)
            {
                continue;
            }

            float distance = (carrier.Transform.position - near).sqrMagnitude;

            if (distance >= bestDistance)
            {
                continue;
            }

            best = carrier;
            bestDistance = distance;
        }

        return best;
    }
}
