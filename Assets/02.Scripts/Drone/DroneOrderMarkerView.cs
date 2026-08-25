using System.Collections.Generic;
using UnityEngine;

public class DroneOrderMarkerView
{
    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    private readonly Transform _parent;
    private readonly Material _material;
    private readonly Color _miningColor;
    private readonly Color _reservedColor;
    private readonly float _lastOrderAlphaScale;
    private readonly float _heightOffset;
    private readonly float _size;
    private readonly int _maxOrders;

    private readonly List<Renderer> _markers = new List<Renderer>();
    private MaterialPropertyBlock _block;

    public DroneOrderMarkerView(Transform parent, Material material, Color miningColor, Color reservedColor, float lastOrderAlphaScale, float heightOffset, float size, int maxOrders)
    {
        _parent = parent;
        _material = material;
        _miningColor = miningColor;
        _reservedColor = reservedColor;
        _lastOrderAlphaScale = lastOrderAlphaScale;
        _heightOffset = heightOffset;
        _size = size;
        _maxOrders = maxOrders;
    }

    public void Refresh(List<IDroneWorker> workers, List<MaterialObject> pendingMining)
    {
        if (_material == null)
        {
            return;
        }

        int usedCount = 0;

        for (int i = 0; i < workers.Count; i++)
        {
            DroneStateMachine miner = workers[i] as DroneStateMachine;

            if (miner == null || miner.CurrentTarget == null)
            {
                continue;
            }

            Show(usedCount, miner.CurrentTarget, _miningColor);

            usedCount++;
        }

        for (int i = 0; i < pendingMining.Count; i++)
        {
            if (pendingMining[i] == null)
            {
                continue;
            }

            Color color = _reservedColor;
            color.a *= GetOrderAlpha(i);

            Show(usedCount, pendingMining[i], color);

            usedCount++;
        }

        for (int i = usedCount; i < _markers.Count; i++)
        {
            _markers[i].enabled = false;
        }
    }

    private float GetOrderAlpha(int orderIndex)
    {
        if (_maxOrders <= 1)
        {
            return 1f;
        }

        float t = (float)orderIndex / (_maxOrders - 1);

        return Mathf.Lerp(1f, _lastOrderAlphaScale, t);
    }

    private void Show(int index, MaterialObject target, Color color)
    {
        Renderer marker = GetOrCreate(index);

        Vector3 position = target.transform.position;
        position.y += _heightOffset;

        marker.transform.position = position;
        marker.transform.localScale = new Vector3(_size, _size, 1f);
        marker.enabled = true;

        if (_block == null)
        {
            _block = new MaterialPropertyBlock();
        }

        marker.GetPropertyBlock(_block);
        _block.SetColor(ColorId, color);
        marker.SetPropertyBlock(_block);
    }

    private Renderer GetOrCreate(int index)
    {
        while (_markers.Count <= index)
        {
            GameObject created = GameObject.CreatePrimitive(PrimitiveType.Quad);

            created.name = $"OrderMarker_{_markers.Count}";
            created.transform.SetParent(_parent, false);
            created.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Collider quadCollider = created.GetComponent<Collider>();

            if (quadCollider != null)
            {
                Object.Destroy(quadCollider);
            }

            MeshRenderer meshRenderer = created.GetComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = _material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;

            _markers.Add(meshRenderer);
        }

        return _markers[index];
    }
}
