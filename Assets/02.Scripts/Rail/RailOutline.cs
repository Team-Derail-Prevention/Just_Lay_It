using UnityEngine;

public class RailOutline : MonoBehaviour
{
    [Header("Fill Materials")]
    [SerializeField] private Material _validFillMaterial;   // 설치 가능할 때 (초록, 반투명)
    [SerializeField] private Material _invalidFillMaterial; // 설치 불가할 때 (빨강, 반투명)
    [SerializeField] private float _heightOffset = 0.02f;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    public void Show(CubeInfo cubeInfo, bool isValid)
    {
        Material materialToUse = isValid ? _validFillMaterial : _invalidFillMaterial;
        if (materialToUse == null)
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
        float y = bounds.max.y + _heightOffset;
        Vector3 p0 = new Vector3(bounds.min.x, y, bounds.min.z);
        Vector3 p1 = new Vector3(bounds.max.x, y, bounds.min.z);
        Vector3 p2 = new Vector3(bounds.max.x, y, bounds.max.z);
        Vector3 p3 = new Vector3(bounds.min.x, y, bounds.max.z);
        EnsureQuadCreated();
        UpdateQuad(p0, p1, p2, p3);
        _meshRenderer.sharedMaterial = materialToUse;
        _meshRenderer.enabled = true;
    }

    public void Hide()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = false;
        }
    }

    private void EnsureQuadCreated()
    {
        if (_meshFilter != null)
        {
            return;
        }
        GameObject quadObj = new GameObject("HoverFillQuad");
        quadObj.transform.SetParent(transform, false);
        _meshFilter = quadObj.AddComponent<MeshFilter>();
        _meshRenderer = quadObj.AddComponent<MeshRenderer>();
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
        _mesh = new Mesh();
        _mesh.name = "HoverFillQuadMesh";
        _meshFilter.mesh = _mesh;
    }

    private void UpdateQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Transform quadTransform = _meshFilter.transform;
        Vector3[] vertices =
        {
            quadTransform.InverseTransformPoint(p0),
            quadTransform.InverseTransformPoint(p1),
            quadTransform.InverseTransformPoint(p2),
            quadTransform.InverseTransformPoint(p3)
        };

        int[] triangles =
        {
            0, 1, 2, 0, 2, 3, // 정방향
            0, 2, 1, 0, 3, 2  // 역방향
        };
        Vector2[] uvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}