using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DroneRailPreloader
{
    private readonly List<IDroneWorker> _workers;
    private readonly string _railAddress;

    private GameObject _railPrefab;
    private bool _isPrefabRequested;

    public DroneRailPreloader(List<IDroneWorker> workers, string railAddress)
    {
        _workers = workers;
        _railAddress = railAddress;
    }

    public void Tick()
    {
        if (NetworkRailService.Instance == null)
        {
            return;
        }

        int ownedCount = GetOwnedRailCount();
        int heldCount = CountHeldRails();

        if (heldCount < ownedCount)
        {
            GiveOne();

            return;
        }

        if (heldCount > ownedCount)
        {
            TakeOneBack();
        }
    }

    public void TopUp(DroneDeliveryWorker carrier)
    {
        if (carrier == null)
        {
            return;
        }

        if (_railPrefab == null)
        {
            RequestPrefab();

            return;
        }

        while (carrier.CanPreload && CountHeldRails() < GetOwnedRailCount() + 1)
        {
            if (TryGiveOne(carrier) == false)
            {
                return;
            }
        }
    }

    private int GetOwnedRailCount()
    {
        if (NetworkRailService.Instance == null)
        {
            return 0;
        }

        RailBuildViewModel inventory = NetworkRailService.Instance.GetLocalRailBuildViewModel();

        return inventory.GetSlot(RailType.Straight).OwnedCount + inventory.GetSlot(RailType.Corner).OwnedCount;
    }

    private int CountHeldRails()
    {
        int count = 0;

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneDeliveryWorker carrier = _workers[i] as DroneDeliveryWorker;

            if (carrier != null)
            {
                count += carrier.PreloadCount;
            }
        }

        return count;
    }

    private void GiveOne()
    {
        if (_railPrefab == null)
        {
            RequestPrefab();

            return;
        }

        DroneDeliveryWorker carrier = FindPreloadableCarrier();

        if (carrier == null)
        {
            return;
        }

        TryGiveOne(carrier);
    }

    private bool TryGiveOne(DroneDeliveryWorker carrier)
    {
        GameObject rail = Object.Instantiate(_railPrefab);

        PrepareVisual(rail);

        if (carrier.TryPreload(rail))
        {
            return true;
        }

        Object.Destroy(rail);

        return false;
    }

    private DroneDeliveryWorker FindPreloadableCarrier()
    {
        DroneDeliveryWorker best = null;
        int bestCount = int.MaxValue;

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneDeliveryWorker carrier = _workers[i] as DroneDeliveryWorker;

            if (carrier == null || carrier.CanPreload == false)
            {
                continue;
            }

            if (carrier.PreloadCount >= bestCount)
            {
                continue;
            }

            best = carrier;
            bestCount = carrier.PreloadCount;
        }

        return best;
    }

    private void TakeOneBack()
    {
        DroneDeliveryWorker fullest = null;
        int fullestCount = 0;

        for (int i = 0; i < _workers.Count; i++)
        {
            DroneDeliveryWorker carrier = _workers[i] as DroneDeliveryWorker;

            if (carrier == null || carrier.PreloadCount <= fullestCount)
            {
                continue;
            }

            fullest = carrier;
            fullestCount = carrier.PreloadCount;
        }

        if (fullest == null)
        {
            return;
        }

        GameObject rail = fullest.TakePreload();

        if (rail != null)
        {
            Object.Destroy(rail);
        }
    }

    private void PrepareVisual(GameObject rail)
    {
        RailOutline outline = rail.GetComponent<RailOutline>();

        if (outline != null)
        {
            outline.enabled = false;
        }

        RailPreviewController preview = rail.GetComponent<RailPreviewController>();

        if (preview != null)
        {
            preview.enabled = false;
        }

        DroneManager.SetPayloadCollision(rail, false);
    }

    private void RequestPrefab()
    {
        if (_isPrefabRequested)
        {
            return;
        }

        if (string.IsNullOrEmpty(_railAddress) || ResourceManager.Instance == null)
        {
            return;
        }

        _isPrefabRequested = true;

        LoadPrefabAsync().Forget();
    }

    private async UniTask LoadPrefabAsync()
    {
        _railPrefab = await ResourceManager.Instance.LoadAsset<GameObject>(_railAddress);

        if (_railPrefab == null)
        {
            Debug.LogWarning($"[DroneRailPreloader] 사전 적재용 레일 프리팹 로드 실패: {_railAddress}");
        }
    }
}
