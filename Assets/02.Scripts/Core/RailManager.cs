using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RailManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera Camera_Main;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Transform Transform_MapRoot;
    [SerializeField] private Transform Transform_RailRoot;

    [Header("Outline Setting")]
    [SerializeField] private Material _outlineMaterial;
    [SerializeField] private float _outlineHeightOffset = 0.02f;
    [SerializeField] private float _outlineWidth = 0.06f;

    [Header("Rail Prefab")]
    [SerializeField] private string _railSegmentAddress = "Rail_Segment";

    private Dictionary<string, Vector3> _cubeCenters = new Dictionary<string, Vector3>();
    private Dictionary<string, GameObject> _cubeObjects = new Dictionary<string, GameObject>();

    private HashSet<string> _installedCubes = new HashSet<string>();

    private string _hoveredCubeName;
    private Vector3 _hoveredCubeCenter;
    private bool _hasHoveredCube;

    private LineRenderer _hoverLine;

    private void Awake()
    {
        BuildCubeLookup();
    }

    private void Update()
    {
        UpdateHover();
        UpdateClickInput();
    }

    private void BuildCubeLookup()
    {
        _cubeCenters.Clear();
        _cubeObjects.Clear();

        if (Transform_MapRoot == null)
        {
            Debug.LogWarning("[RailManager] Transform_MapRoot가 연결 안 됨");
            return;
        }

        Renderer[] childRenderers = Transform_MapRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < childRenderers.Length; i++)
        {
            GameObject cubeObj = FindCubeRoot(childRenderers[i].transform);
            string cubeName = cubeObj.name;

            if (!_cubeCenters.ContainsKey(cubeName))
            {
                Bounds bounds = childRenderers[i].bounds;
                Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

                _cubeCenters.Add(cubeName, topCenter);
                _cubeObjects.Add(cubeName, cubeObj);
            }
        }

        Debug.Log("[RailManager] 큐브 " + _cubeCenters.Count + "개 인식됨");
    }

    private GameObject FindCubeRoot(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (current.name.StartsWith("Cube_"))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return start.gameObject;
    }

    private bool TryFindNearestCube(Vector3 worldPoint, out string cubeName, out Vector3 cubeCenter)
    {
        cubeName = null;
        cubeCenter = Vector3.zero;

        float bestDistSqr = Mathf.Infinity;
        bool found = false;

        foreach (KeyValuePair<string, Vector3> entry in _cubeCenters)
        {
            float distSqr = (entry.Value - worldPoint).sqrMagnitude;

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                cubeName = entry.Key;
                cubeCenter = entry.Value;
                found = true;
            }
        }

        return found;
    }

    private void UpdateHover()
    {
        Ray ray = Camera_Main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        bool hasHit = Physics.Raycast(ray, out hitInfo, 500f, _groundLayer);

        if (!hasHit)
        {
            ClearHoverOutline();
            return;
        }

        string cubeName;
        Vector3 cubeCenter;
        bool found = TryFindNearestCube(hitInfo.point, out cubeName, out cubeCenter);

        if (!found)
        {
            ClearHoverOutline();
            return;
        }

        if (_hoveredCubeName != cubeName)
        {
            _hoveredCubeName = cubeName;
            _hoveredCubeCenter = cubeCenter;
            _hasHoveredCube = true;
            ApplyOutline(cubeName);
        }
    }

    private void ApplyOutline(string cubeName)
    {
        if (_outlineMaterial == null) return;
        if (!_cubeObjects.TryGetValue(cubeName, out GameObject cubeObj)) return;

        Renderer renderer = cubeObj.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        Bounds bounds = renderer.bounds;
        float y = bounds.max.y + _outlineHeightOffset;

        Vector3 p0 = new Vector3(bounds.min.x, y, bounds.min.z);
        Vector3 p1 = new Vector3(bounds.max.x, y, bounds.min.z);
        Vector3 p2 = new Vector3(bounds.max.x, y, bounds.max.z);
        Vector3 p3 = new Vector3(bounds.min.x, y, bounds.max.z);

        EnsureHoverLineCreated();

        _hoverLine.SetPosition(0, p0);
        _hoverLine.SetPosition(1, p1);
        _hoverLine.SetPosition(2, p2);
        _hoverLine.SetPosition(3, p3);
        _hoverLine.enabled = true;
    }

    private void EnsureHoverLineCreated()
    {
        if (_hoverLine != null) return;

        GameObject lineObj = new GameObject("HoverOutlineLine");
        lineObj.transform.SetParent(Transform_RailRoot != null ? Transform_RailRoot : transform, false);

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

    private void ClearHoverOutline()
    {
        if (_hoverLine != null)
        {
            _hoverLine.enabled = false;
        }

        _hoveredCubeName = null;
        _hoveredCubeCenter = Vector3.zero;
        _hasHoveredCube = false;
    }

    private void UpdateClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (!_hasHoveredCube)
        {
            Debug.Log("[RailManager] 지형이 없음");
            return;
        }

        TryInstallRail(_hoveredCubeName, _hoveredCubeCenter);
    }

    private void TryInstallRail(string cubeName, Vector3 cubeCenter)
    {
        if (_installedCubes.Contains(cubeName))
        {
            Debug.Log("[RailManager] 이미 레일 있음: " + cubeName);
            return;
        }

        _installedCubes.Add(cubeName);
        SpawnRailAddressableAsync(worldPos: cubeCenter).Forget();
    }

    private async UniTask SpawnRailAddressableAsync(Vector3 worldPos)
    {
        if (string.IsNullOrEmpty(_railSegmentAddress))
        {
            Debug.LogWarning("[RailManager] Rail Segment Address가 비어있음");
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(_railSegmentAddress, worldPos, Quaternion.identity, Transform_RailRoot);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning("[RailManager] 레일 어드레서블 로드 실패");
            return;
        }

        Debug.Log("[RailManager] 레일 설치됨: " + spawnedRail.name);
    }
}