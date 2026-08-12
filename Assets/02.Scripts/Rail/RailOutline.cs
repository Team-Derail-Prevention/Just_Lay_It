using UnityEngine;

public class RailOutline : MonoBehaviour
{
    [SerializeField] private Material _outlineMaterial;
    [SerializeField] private float _outlineHeightOffset = 0.02f;
    [SerializeField] private float _outlineWidth = 0.06f;

    private LineRenderer _hoverLine;

    public void Show(CubeInfo cubeInfo)
    {
        if (_outlineMaterial == null)
        {
            return;
        }

        if (cubeInfo.Obj == null)
        {
            return;
        }

        Renderer targetRenderer = cubeInfo.Obj.GetComponentInChildren<Renderer>();

        if (targetRenderer == null)
        {
            return;
        }

        Bounds bounds = targetRenderer.bounds;
        float y = bounds.max.y + _outlineHeightOffset;

        Vector3 p0 = new Vector3(bounds.min.x, y, bounds.min.z);
        Vector3 p1 = new Vector3(bounds.max.x, y, bounds.min.z);
        Vector3 p2 = new Vector3(bounds.max.x, y, bounds.max.z);
        Vector3 p3 = new Vector3(bounds.min.x, y, bounds.max.z);

        EnsureLineCreated();

        _hoverLine.SetPosition(0, p0);
        _hoverLine.SetPosition(1, p1);
        _hoverLine.SetPosition(2, p2);
        _hoverLine.SetPosition(3, p3);
        _hoverLine.enabled = true;
    }

    public void Hide()
    {
        if (_hoverLine != null)
        {
            _hoverLine.enabled = false;
        }
    }

    private void EnsureLineCreated()
    {
        if (_hoverLine != null)
        {
            return;
        }

        GameObject lineObj = new GameObject("HoverOutlineLine");
        lineObj.transform.SetParent(transform, false);

        _hoverLine = lineObj.AddComponent<LineRenderer>();
        _hoverLine.material = _outlineMaterial;
        _hoverLine.loop = true;
        _hoverLine.useWorldSpace = true;
        _hoverLine.positionCount = 4;
        _hoverLine.widthMultiplier = _outlineWidth;
        _hoverLine.numCornerVertices = 2;
        _hoverLine.numCapVertices = 2;
        _hoverLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _hoverLine.receiveShadows = false;
    }
}