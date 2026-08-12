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

    [Header("Rail Prefab (Addressable, RailHoverOutline + RailPreviewController 포함)")]
    [SerializeField] private string _railSegmentAddress = "Rail_Segment";

    [Header("Preview Ghost")]
    [SerializeField, Range(0f, 1f)] private float _ghostAlpha = 0.4f;

    // 격자 좌표 -> 큐브 정보 (O(1) 조회)
    private Dictionary<Vector2Int, CubeInfo> _cubeGrid = new Dictionary<Vector2Int, CubeInfo>();

    private float _tileSize = 1f;
    private float _gridOriginX;
    private float _gridOriginZ;

    private HashSet<Vector2Int> _installedCubes = new HashSet<Vector2Int>();

    private Vector2Int _hoveredGridIndex;
    private bool _isHoveredCube;
    private CubeInfo _hoveredCubeInfo;

    // 프리뷰용 - 미리 로드해서 계속 재사용
    private GameObject _previewInstance;
    private RailOutline _previewOutline;
    private RailPreviewController _previewController;

    private void Awake()
    {
        BuildCubeLookup();
        SpawnPreviewInstanceAsync().Forget();
    }

    private void Update()
    {
        if (_previewInstance == null)
        {
            return;
        }

        bool rotationChanged = _previewController.HandleRotationInput();

        UpdateHover();

        if (_isHoveredCube && rotationChanged)
        {
            _previewController.Show(_hoveredCubeInfo);
        }

        UpdateClickInput();
    }

    private void BuildCubeLookup()
    {
        _cubeGrid.Clear();

        if (Transform_MapRoot == null)
        {
            Debug.LogWarning("[RailManager] Transform_MapRoot가 연결 안 됨");
            return;
        }

        Renderer[] childRenderers = Transform_MapRoot.GetComponentsInChildren<Renderer>(true);

        List<CubeInfo> collected = new List<CubeInfo>();
        HashSet<string> seenNames = new HashSet<string>();
        float minX = float.MaxValue;
        float minZ = float.MaxValue;
        float tileSizeSum = 0f;
        int tileSizeCount = 0;

        for (int i = 0; i < childRenderers.Length; i++)
        {
            GameObject cubeObj = FindCubeRoot(childRenderers[i].transform);
            string cubeName = cubeObj.name;

            if (seenNames.Contains(cubeName))
            {
                continue;
            }
            seenNames.Add(cubeName);

            Bounds bounds = childRenderers[i].bounds;
            Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            CubeInfo info = new CubeInfo { Name = cubeName, Center = topCenter, Obj = cubeObj };
            collected.Add(info);

            if (topCenter.x < minX)
            {
                minX = topCenter.x;
            }
            if (topCenter.z < minZ)
            {
                minZ = topCenter.z;
            }

            tileSizeSum += bounds.size.x;
            tileSizeCount++;
        }

        if (collected.Count == 0)
        {
            Debug.LogWarning("[RailManager] 큐브를 하나도 찾지 못함");
            return;
        }

        _gridOriginX = minX;
        _gridOriginZ = minZ;
        _tileSize = tileSizeCount > 0 ? (tileSizeSum / tileSizeCount) : 1f;

        if (_tileSize <= 0f)
        {
            Debug.LogWarning("[RailManager] 타일 크기가 0 이하로 계산됨, 1로 보정");
            _tileSize = 1f;
        }

        for (int i = 0; i < collected.Count; i++)
        {
            Vector2Int gridIndex = WorldPointToGridIndex(collected[i].Center);

            if (_cubeGrid.ContainsKey(gridIndex))
            {
                Debug.LogWarning("[RailManager] 격자 좌표 충돌: " + gridIndex + " - " + collected[i].Name);
                continue;
            }

            _cubeGrid.Add(gridIndex, collected[i]);
        }

        Debug.Log("[RailManager] 큐브 " + _cubeGrid.Count + "개 인식됨 (타일 크기: " + _tileSize + ")");
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

    private Vector2Int WorldPointToGridIndex(Vector3 worldPoint)
    {
        int x = Mathf.RoundToInt((worldPoint.x - _gridOriginX) / _tileSize);
        int z = Mathf.RoundToInt((worldPoint.z - _gridOriginZ) / _tileSize);
        return new Vector2Int(x, z);
    }

    private async UniTask SpawnPreviewInstanceAsync()
    {
        if (string.IsNullOrEmpty(_railSegmentAddress))
        {
            Debug.LogWarning("[RailManager] Rail Segment Address가 비어있음");
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(_railSegmentAddress, Vector3.zero, Quaternion.identity, Transform_RailRoot);
        GameObject instance = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning("[RailManager] 프리뷰용 레일 로드 실패");
            return;
        }

        _previewOutline = instance.GetComponent<RailOutline>();
        _previewController = instance.GetComponent<RailPreviewController>();

        if (_previewOutline == null || _previewController == null)
        {
            Debug.LogWarning("[RailManager] 레일 프리팹에 RailHoverOutline/RailPreviewController가 없음");
        }
        else
        {
            _previewController.SetGhostAlpha(_ghostAlpha);
        }

        instance.SetActive(false);
        _previewInstance = instance;
    }

    private void UpdateHover()
    {
        Ray ray = Camera_Main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        bool hasHit = Physics.Raycast(ray, out hitInfo, 500f, _groundLayer);

        if (!hasHit)
        {
            ClearHover();
            return;
        }

        Vector2Int gridIndex = WorldPointToGridIndex(hitInfo.point);
        CubeInfo cubeInfo;
        bool found = _cubeGrid.TryGetValue(gridIndex, out cubeInfo);

        if (!found)
        {
            ClearHover();
            return;
        }

        if (!_isHoveredCube || !_hoveredGridIndex.Equals(gridIndex))
        {
            _hoveredGridIndex = gridIndex;
            _hoveredCubeInfo = cubeInfo;
            _isHoveredCube = true;

            _previewController.Show(cubeInfo);
            _previewOutline.Show(cubeInfo);
        }
    }

    private void ClearHover()
    {
        if (_isHoveredCube)
        {
            _previewOutline.Hide();
            _previewController.Hide();
        }

        _isHoveredCube = false;
    }

    private void UpdateClickInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!_isHoveredCube)
        {
            Debug.Log("[RailManager] 지형이 없음");
            return;
        }

        TryInstallRail(_hoveredGridIndex, _hoveredCubeInfo);
    }

    private void TryInstallRail(Vector2Int gridIndex, CubeInfo cubeInfo)
    {
        if (_installedCubes.Contains(gridIndex))
        {
            Debug.Log("[RailManager] 이미 레일 있음: " + cubeInfo.Name);
            return;
        }

        _installedCubes.Add(gridIndex);
        SpawnPlacedRailAsync(cubeInfo.Center, _previewController.CurrentRotation).Forget();
    }

    private async UniTask SpawnPlacedRailAsync(Vector3 worldPos, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(_railSegmentAddress))
        {
            Debug.LogWarning("[RailManager] Rail Segment Address가 비어있음");
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(_railSegmentAddress, worldPos, rotation, Transform_RailRoot);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning("[RailManager] 레일 어드레서블 로드 실패");
            return;
        }

        RailOutline outline = spawnedRail.GetComponent<RailOutline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        RailPreviewController controller = spawnedRail.GetComponent<RailPreviewController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Debug.Log("[RailManager] 레일 설치됨: " + spawnedRail.name);
    }
}