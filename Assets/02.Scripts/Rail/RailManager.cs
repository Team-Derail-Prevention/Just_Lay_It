using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum RailType
{
    Straight,
    Corner
}

public class RailManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera Camera_Main;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Transform Transform_MapRoot;
    [SerializeField] private Transform Transform_RailRoot;
    [SerializeField] private MapManager MapManager_Ref;

    [Header("Rail Prefabs (Addressable)")]
    [SerializeField] private string _straightRailAddress = "Rail_Straight";
    [SerializeField] private string _cornerRailAddress = "Rail_Corner";

    [Header("Preview Ghost")]
    [SerializeField, Range(0f, 1f)] private float _ghostAlpha = 0.4f;

    private RailType _currentRailType = RailType.Straight;

    private float _tileSize = 1f;
    private float _gridOriginX;
    private float _gridOriginZ;

    private HashSet<Vector2Int> _installedCubes = new HashSet<Vector2Int>();
    private List<GameObject> _spawnedRailObjects = new List<GameObject>();

    private Vector2Int _hoveredGridIndex;
    private bool _isHoveredCube;
    private CubeInfo _hoveredCubeInfo;

    private GameObject _previewInstance;
    private RailOutline _previewOutline;
    private RailPreviewController _previewController;

    private Dictionary<Vector2Int, CubeInfo> _cubeGrid = new Dictionary<Vector2Int, CubeInfo>();

    private string CurrentRailAddress
    {
        get
        {
            return _currentRailType == RailType.Corner ? _cornerRailAddress : _straightRailAddress;
        }
    }

    private void Awake()
    {
        SpawnPreviewInstanceAsync().Forget();

        if (MapManager_Ref != null)
        {
            MapManager_Ref.OnMapGenerated += HandleMapGenerated;
        }
    }

    private void OnDestroy()
    {
        if (MapManager_Ref != null)
        {
            MapManager_Ref.OnMapGenerated -= HandleMapGenerated;
        }

        if (_previewInstance != null)
        {
            Addressables.ReleaseInstance(_previewInstance);
        }

        ClearAllPlacedRails();
    }

    private void HandleMapGenerated(Dictionary<Vector3Int, int> mapTypeData)
    {
         Transform_MapRoot = MapManager_Ref.MapRoot;
        ClearAllPlacedRails();
        BuildCubeLookup();
    }

    private void Update()
    {
        if (_previewInstance == null)
        {
            return;
        }

        HandleRailTypeInput();

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

    private void HandleRailTypeInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && _currentRailType != RailType.Straight)
        {
            ChangeRailType(RailType.Straight);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && _currentRailType != RailType.Corner)
        {
            ChangeRailType(RailType.Corner);
        }
    }

    private void ChangeRailType(RailType newType)
    {
        _currentRailType = newType;
        Debug.Log($"[RailManager] 레일 타입 변경: {_currentRailType}");

        ClearHover();

        if (_previewInstance != null)
        {
            Addressables.ReleaseInstance(_previewInstance);
            _previewInstance = null;
            _previewOutline = null;
            _previewController = null;
        }

        SpawnPreviewInstanceAsync().Forget();
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
        HashSet<GameObject> seenObjects = new HashSet<GameObject>();
        float minX = float.MaxValue;
        float minZ = float.MaxValue;
        float tileSizeSum = 0f;
        int tileSizeCount = 0;

        for (int i = 0; i < childRenderers.Length; i++)
        {
            GameObject rendererObj = childRenderers[i].gameObject;

            bool isGroundLayer = ((1 << rendererObj.layer) & _groundLayer.value) != 0;
            if (!isGroundLayer) continue;

            if (seenObjects.Contains(rendererObj)) continue;
            seenObjects.Add(rendererObj);

            Bounds bounds = childRenderers[i].bounds;
            Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            MapTileInfo tileInfo = rendererObj.GetComponent<MapTileInfo>();

            CubeInfo info = new CubeInfo
            {
                Name = rendererObj.name,
                Center = topCenter,
                Obj = rendererObj,
                TileScript = tileInfo
            };

            collected.Add(info);

            if (topCenter.x < minX) minX = topCenter.x;
            if (topCenter.z < minZ) minZ = topCenter.z;

            tileSizeSum += bounds.size.x;
            tileSizeCount++;
        }

        if (collected.Count == 0)
        {
            Debug.LogWarning("[RailManager] Ground 레이어인 타일을 하나도 찾지 못함.");
            return;
        }

        _gridOriginX = minX;
        _gridOriginZ = minZ;
        _tileSize = tileSizeCount > 0 ? (tileSizeSum / tileSizeCount) : 1f;

        if (_tileSize <= 0f) _tileSize = 1f;

        for (int i = 0; i < collected.Count; i++)
        {
            Vector2Int gridIndex = WorldPointToGridIndex(collected[i].Center);

            if (_cubeGrid.ContainsKey(gridIndex))
            {
                Debug.LogWarning("[RailManager] 격자 좌표 충돌: " + gridIndex + " - " + collected[i].Name);
                continue;
            }

            CubeInfo info = collected[i];
            info.GridIndex = gridIndex;

            _cubeGrid.Add(gridIndex, info);
        }

        Debug.Log("[RailManager] 타일 " + _cubeGrid.Count + "개 인식됨");
    }

    private Vector2Int WorldPointToGridIndex(Vector3 worldPoint)
    {
        int x = Mathf.RoundToInt((worldPoint.x - _gridOriginX) / _tileSize);
        int z = Mathf.RoundToInt((worldPoint.z - _gridOriginZ) / _tileSize);
        return new Vector2Int(x, z);
    }

    private async UniTask SpawnPreviewInstanceAsync()
    {
        string address = CurrentRailAddress;
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogWarning($"[RailManager] {_currentRailType} 레일 Address가 비어있음");
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, Vector3.zero, Quaternion.identity, Transform_RailRoot);
        GameObject instance = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[RailManager] 프리뷰용 {_currentRailType} 레일 로드 실패");
            return;
        }

        _previewOutline = instance.GetComponent<RailOutline>();
        _previewController = instance.GetComponent<RailPreviewController>();

        if (_previewOutline == null || _previewController == null)
        {
            Debug.LogWarning($"[RailManager] {_currentRailType} 레일 프리팹에 RailOutline/RailPreviewController가 없음");
        }
        else
        {
            _previewController.SetGhostAlpha(_ghostAlpha);
        }

        instance.SetActive(false);
        _previewInstance = instance;

        if (_isHoveredCube && _hoveredCubeInfo.Obj != null)
        {
            _previewController.Show(_hoveredCubeInfo);
            _previewOutline.Show(_hoveredCubeInfo);
        }
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

        if (!_cubeGrid.TryGetValue(gridIndex, out CubeInfo cubeInfo))
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
            _previewOutline?.Hide();
            _previewController?.Hide();
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
        if (_installedCubes.Contains(gridIndex) || (cubeInfo.TileScript != null && cubeInfo.TileScript.HasRail))
        {
            Debug.Log("[RailManager] 이미 레일이 설치된 위치입니다: " + cubeInfo.Name);
            return;
        }

        if (cubeInfo.TileScript != null)
        {
            if (!cubeInfo.TileScript.CanInstallRail)
            {
                Debug.Log($"[RailManager] 타일({cubeInfo.Name})의 CanInstallRail이 false이므로 설치 불가!");
                return;
            }
        }
        else
        {
            Debug.LogWarning($"[RailManager] {cubeInfo.Name}에 MapTileInfo 컴포넌트가 없습니다.");
            return;
        }

        _installedCubes.Add(gridIndex);

        if (cubeInfo.TileScript != null)
        {
            cubeInfo.TileScript.HasRail = true;
        }

        SpawnPlacedRailAsync(cubeInfo.Center, _previewController.CurrentRotation).Forget();
    }

    private async UniTask SpawnPlacedRailAsync(Vector3 worldPos, Quaternion rotation)
    {
        string address = CurrentRailAddress;
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogWarning($"[RailManager] {_currentRailType} Rail Address가 비어있음");
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, worldPos, rotation, Transform_RailRoot);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[RailManager] {_currentRailType} 레일 어드레서블 로드 실패");
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

        _spawnedRailObjects.Add(spawnedRail);
        Debug.Log($"[RailManager] {_currentRailType} 레일 설치됨: " + spawnedRail.name);
    }

    private void ClearAllPlacedRails()
    {
        _installedCubes.Clear();

        for (int i = 0; i < _spawnedRailObjects.Count; i++)
        {
            if (_spawnedRailObjects[i] != null)
            {
                Addressables.ReleaseInstance(_spawnedRailObjects[i]);
            }
        }
        _spawnedRailObjects.Clear();
    }
}