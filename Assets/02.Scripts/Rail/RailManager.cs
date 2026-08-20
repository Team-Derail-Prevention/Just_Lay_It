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

public class RailManager : SingletonBase<RailManager>
{
    [Header("Refs")]
    [SerializeField] private Camera Camera_Main;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _blockedLayer; // 오브젝트가 올라간 바닥(Default) 레이어
    [SerializeField] private Transform Transform_MapRoot;
    [SerializeField] private Transform Transform_RailRoot;
    [SerializeField] private MapManager MapManager_Ref;

    [Header("Rail Prefabs (Addressable)")]
    [SerializeField] private string _straightRailAddress = "Rail_Straight";
    [SerializeField] private string _cornerRailAddress = "Rail_Corner";

    [Header("Preview Ghost")]
    [SerializeField, Range(0f, 1f)] private float _ghostAlpha = 0.4f;

    public float GhostAlpha { get { return _ghostAlpha; } }

    private RailType _currentRailType = RailType.Straight;

    private float _tileSize = 1f;
    private float _gridOriginX;
    private float _gridOriginZ;
    private float _groundPlaneY;

    private HashSet<Vector2Int> _installedCubes = new HashSet<Vector2Int>();

    private struct PlacedRailInfo
    {
        public GameObject Obj;
        public RailType Type;
    }
    private Dictionary<Vector2Int, PlacedRailInfo> _placedRails = new Dictionary<Vector2Int, PlacedRailInfo>();

    private Vector2Int _hoveredGridIndex;
    private bool _isHoveredCube;
    private CubeInfo _hoveredCubeInfo;

    private GameObject _previewInstance;
    private RailOutline _previewOutline;
    private RailPreviewController _previewController;

    private Dictionary<Vector2Int, CubeInfo> _cubeGrid = new Dictionary<Vector2Int, CubeInfo>();

    private bool _isPlaceModeActive = false;
    private bool _isConfirmPopupOpen = false;

    private Vector2Int _pendingGridIndex;
    private CubeInfo _pendingCubeInfo;

    [Header("Blocked Tile Recheck")]
    [SerializeField] private float _blockedTileRecheckInterval = 1f;
    private float _blockedTileRecheckTimer;

    private string CurrentRailAddress
    {
        get => _currentRailType == RailType.Corner ? _cornerRailAddress : _straightRailAddress;
    }

    private void Awake()
    {
        base.Init();

        if (MapManager_Ref != null)
        {
            MapManager_Ref.OnMapGenerated += HandleMapGenerated;
        }
    }

    private void Start()
    {
        if (NetworkRailService.Instance != null)
        {
            NetworkRailService.Instance.OnRequestPlaceMode += EnterPlaceMode;
        }
        else
        {
            Debug.LogWarning("[RailManager] NetworkRailService.Instance가 Start() 시점에도 null입니다. 씬에 NetworkRailService가 있는지 확인하세요.");
        }
    }

    private void OnDestroy()
    {
        if (NetworkRailService.Instance != null)
        {
            NetworkRailService.Instance.OnRequestPlaceMode -= EnterPlaceMode;
        }

        if (MapManager_Ref != null)
        {
            MapManager_Ref.OnMapGenerated -= HandleMapGenerated;
        }

        ExitPlaceMode(clearPlacedRails: false);

        if (_previewInstance != null)
        {
            Addressables.ReleaseInstance(_previewInstance);
        }
    }

    private void Update()
    {
        _blockedTileRecheckTimer += Time.deltaTime;
        if (_blockedTileRecheckTimer >= _blockedTileRecheckInterval)
        {
            _blockedTileRecheckTimer = 0f;
            RecheckBlockedTiles();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            RemoveAllRail();
        }

        if (!_isPlaceModeActive)
        {
            HandlePlaceModeEntryInput();
            return;
        }

        if (_previewInstance == null)
        {
            return;
        }

        if (_isConfirmPopupOpen)
        {
            return;
        }

        HandleRailTypeInput();

        UpdateHover();
        UpdateClickInput();

        if (Input.GetKeyDown(KeyCode.R))
        {
            _previewController.RotateNext();

            if (_isHoveredCube)
            {
                _previewController.Show(_hoveredCubeInfo);
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ExitPlaceMode(clearPlacedRails: false);
        }
    }

    private void EnterPlaceMode(RailType railType)
    {
        if (_isPlaceModeActive)
        {
            Debug.Log("[RailManager] 이미 배치 모드가 활성화되어 있습니다.");
            return;
        }

        _currentRailType = railType;
        _isPlaceModeActive = true;
        _isConfirmPopupOpen = false;

        Debug.Log($"[RailManager] 배치 모드 진입: {_currentRailType}");
        SpawnPreviewInstanceAsync().Forget();
    }

    private void ExitPlaceMode(bool clearPlacedRails = false)
    {
        if (!_isPlaceModeActive) return;

        _isPlaceModeActive = false;
        _isConfirmPopupOpen = false;

        ClearHover();
        CloseConfirmPopup();

        if (_previewInstance != null)
        {
            Addressables.ReleaseInstance(_previewInstance);
            _previewInstance = null;
            _previewOutline = null;
            _previewController = null;
        }

        if (clearPlacedRails)
        {
            ClearAllPlacedRails();
        }

        Debug.Log("[RailManager] 배치 모드 종료");
    }

    private void HandleMapGenerated(Dictionary<Vector3Int, int> mapTypeData)
    {
        Transform_MapRoot = MapManager_Ref.MapRoot;
        ClearAllPlacedRails();
        BuildCubeLookup();
    }

    private void HandlePlaceModeEntryInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkRailService.Instance?.RequestStartPlacement(RailType.Straight);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NetworkRailService.Instance?.RequestStartPlacement(RailType.Corner);
        }
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
        float ySum = 0f;

        LayerMask scanMask = _groundLayer | _blockedLayer;

        for (int i = 0; i < childRenderers.Length; i++)
        {
            GameObject rendererObj = childRenderers[i].gameObject;

            bool isRelevantLayer = ((1 << rendererObj.layer) & scanMask.value) != 0;
            if (!isRelevantLayer) continue;

            if (seenObjects.Contains(rendererObj)) continue;
            seenObjects.Add(rendererObj);

            Bounds bounds = childRenderers[i].bounds;
            Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            MapTileInfo tileInfo = rendererObj.GetComponent<MapTileInfo>();
            bool isGroundLayer = IsInGroundLayer(rendererObj.layer);

            CubeInfo info = new CubeInfo
            {
                Name = rendererObj.name,
                Center = topCenter,
                Obj = rendererObj,
                TileScript = tileInfo,
                IsGroundLayer = isGroundLayer
            };

            collected.Add(info);

            if (topCenter.x < minX) minX = topCenter.x;
            if (topCenter.z < minZ) minZ = topCenter.z;

            tileSizeSum += bounds.size.x;
            tileSizeCount++;
            ySum += topCenter.y;
        }

        if (collected.Count == 0)
        {
            Debug.LogWarning("[RailManager] Ground 레이어인 타일을 하나도 찾지 못함.");
            return;
        }

        _gridOriginX = minX;
        _gridOriginZ = minZ;
        _tileSize = tileSizeCount > 0 ? (tileSizeSum / tileSizeCount) : 1f;
        _groundPlaneY = tileSizeCount > 0 ? (ySum / tileSizeCount) : 0f;
        if (_tileSize <= 0f) _tileSize = 1f;

        for (int i = 0; i < collected.Count; i++)
        {
            Vector2Int gridIndex = WorldPointToGridIndex(collected[i].Center);

            if (_cubeGrid.ContainsKey(gridIndex))
            {
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

    private bool IsInGroundLayer(int layer)
    {
        return ((1 << layer) & _groundLayer.value) != 0;
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

        if (!_isPlaceModeActive)
        {
            Addressables.ReleaseInstance(instance);
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

        Collider previewCollider = instance.GetComponentInChildren<Collider>();
        if (previewCollider != null)
        {
            previewCollider.enabled = false;
        }

        instance.SetActive(false);
        _previewInstance = instance;

        if (_isHoveredCube && _hoveredCubeInfo.Obj != null && _previewController != null && _previewOutline != null)
        {
            bool isValidPlacement = IsPlacementValid(_hoveredGridIndex, _hoveredCubeInfo);
            _previewController.Show(_hoveredCubeInfo);
            _previewOutline.Show(_hoveredCubeInfo, isValidPlacement);
        }
    }

    private void UpdateHover()
    {
        Ray ray = Camera_Main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, _groundPlaneY, 0f));

        if (!groundPlane.Raycast(ray, out float enter))
        {
            ClearHover();
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int gridIndex = WorldPointToGridIndex(hitPoint);

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

            bool isValidPlacement = IsPlacementValid(gridIndex, cubeInfo);

            _previewController.Show(cubeInfo);
            _previewOutline.Show(cubeInfo, isValidPlacement);
        }
    }

    private bool IsPlacementValid(Vector2Int gridIndex, CubeInfo cubeInfo)
    {
        if (!cubeInfo.IsGroundLayer) return false;
        if (_installedCubes.Contains(gridIndex)) return false;
        if (cubeInfo.TileScript != null && cubeInfo.TileScript.HasRail) return false;
        return true;
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
        if (!Input.GetMouseButtonDown(0)) return;

        if (!_isHoveredCube)
        {
            Debug.Log("[RailManager] 지형이 없음");
            return;
        }

        if (!IsPlacementValid(_hoveredGridIndex, _hoveredCubeInfo))
        {
            Debug.Log("[RailManager] 설치 불가능한 위치입니다: " + _hoveredCubeInfo.Name);
            return;
        }

        OpenConfirmPopup(_hoveredGridIndex, _hoveredCubeInfo);
    }

    private void OpenConfirmPopup(Vector2Int gridIndex, CubeInfo cubeInfo)
    {
        if (_isConfirmPopupOpen) return;

        if (UIManager.Instance == null)
        {
            Debug.LogError("[RailManager] UIManager.Instance가 없습니다.");
            return;
        }

        _isConfirmPopupOpen = true;
        _pendingGridIndex = gridIndex;
        _pendingCubeInfo = cubeInfo;

        _previewController?.Show(cubeInfo);
        _previewOutline?.Show(cubeInfo, isValid: true);

        UIManager.Instance.OpenRailPlaceConfirmPopup(
            onRotate: OnPopupRotate,
            onConfirm: OnPopupConfirm,
            onCancel: OnPopupCancel
        );
    }

    private void CloseConfirmPopup()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.CloseRailPlaceConfirmPopup();
    }

    private void OnPopupRotate()
    {
        if (_previewInstance == null) return;
        _previewController?.RotateNext();

        if (_isHoveredCube)
        {
            _previewController?.Show(_hoveredCubeInfo);
        }
    }

    private void OnPopupConfirm()
    {
        TryInstallRail(_pendingGridIndex, _pendingCubeInfo);
        _isConfirmPopupOpen = false;
        CloseConfirmPopup();
    }

    private void OnPopupCancel()
    {
        _isConfirmPopupOpen = false;
        CloseConfirmPopup();
    }

    private void TryInstallRail(Vector2Int gridIndex, CubeInfo cubeInfo)
    {
        if (!IsPlacementValid(gridIndex, cubeInfo))
        {
            Debug.Log("[RailManager] 설치 불가능한 위치입니다: " + cubeInfo.Name);
            return;
        }

        _installedCubes.Add(gridIndex);

        if (cubeInfo.TileScript != null)
        {
            cubeInfo.TileScript.HasRail = true;
        }

        RailType railTypeAtInstall = _currentRailType;

        NetworkRailService.Instance?.ConsumeRailOnPlaced(railTypeAtInstall);

        SpawnPlacedRailAsync(gridIndex, cubeInfo.Center, _previewInstance.transform.rotation, railTypeAtInstall).Forget();
    }

    private async UniTask SpawnPlacedRailAsync(Vector2Int gridIndex, Vector3 worldPos, Quaternion rotation, RailType railType)
    {
        string address = railType == RailType.Corner ? _cornerRailAddress : _straightRailAddress;
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogWarning($"[RailManager] {railType} Rail Address가 비어있음");
            return;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, worldPos, rotation, Transform_RailRoot);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[RailManager] {railType} 레일 어드레서블 로드 실패");
            return;
        }

        RailOutline outline = spawnedRail.GetComponent<RailOutline>();
        if (outline != null) outline.enabled = false;

        RailPreviewController controller = spawnedRail.GetComponent<RailPreviewController>();
        if (controller != null) controller.enabled = false;

        Collider placedCollider = spawnedRail.GetComponentInChildren<Collider>();
        if (placedCollider != null)
        {
            placedCollider.enabled = true;
        }

        _placedRails[gridIndex] = new PlacedRailInfo { Obj = spawnedRail, Type = railType };
        Debug.Log($"[RailManager] {railType} 레일 설치됨: " + spawnedRail.name);
        DroneManager.Instance?.RequestDelivery(spawnedRail, worldPos, rotation);

        ExitPlaceMode(clearPlacedRails: false);
    }

    private void RemoveRail(Vector2Int gridIndex)
    {
        if (!_installedCubes.Contains(gridIndex))
        {
            Debug.Log("[RailManager] 회수할 레일이 없습니다: " + gridIndex);
            return;
        }

        if (!_placedRails.TryGetValue(gridIndex, out PlacedRailInfo placedInfo))
        {
            Debug.LogWarning("[RailManager] _installedCubes엔 있는데 _placedRails엔 없는 좌표입니다(데이터 불일치): " + gridIndex);
            _installedCubes.Remove(gridIndex);
            return;
        }

        if (_cubeGrid.TryGetValue(gridIndex, out CubeInfo cubeInfo) && cubeInfo.TileScript != null)
        {
            cubeInfo.TileScript.HasRail = false;
        }

        if (placedInfo.Obj != null)
        {
            Addressables.ReleaseInstance(placedInfo.Obj);
        }

        _placedRails.Remove(gridIndex);
        _installedCubes.Remove(gridIndex);

        NetworkRailService.Instance?.ReturnRailToInventory(placedInfo.Type);

        Debug.Log($"[RailManager] {placedInfo.Type} 레일 회수됨: " + gridIndex);
    }

    public void RemoveAllRail()
    {
        if (_placedRails.Count == 0)
        {
            Debug.Log("[RailManager] 회수할 레일이 없습니다.");
            return;
        }

        List<Vector2Int> gridIndices = new List<Vector2Int>(_placedRails.Keys);
        foreach (Vector2Int gridIndex in gridIndices)
        {
            RemoveRail(gridIndex);
        }

        Debug.Log("[RailManager] 설치된 레일 전체 회수 완료");
    }

    // 막혀있다고 기록된 타일들만 주기적으로 다시 확인해서, 오브젝트가 사라져 레이어가 바뀌었으면 캐시를 갱신
    private void RecheckBlockedTiles()
    {
        if (_cubeGrid.Count == 0) return;

        List<Vector2Int> keys = new List<Vector2Int>(_cubeGrid.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Vector2Int key = keys[i];
            CubeInfo info = _cubeGrid[key];

            if (info.IsGroundLayer) continue; // 이미 설치 가능한 칸이면 재검사 불필요
            if (info.Obj == null) continue;

            bool isGroundLayerNow = IsInGroundLayer(info.Obj.layer);
            if (isGroundLayerNow != info.IsGroundLayer)
            {
                info.IsGroundLayer = isGroundLayerNow;
                _cubeGrid[key] = info;
                Debug.Log($"[RailManager] 재검사로 타일 상태 변경 감지: {info.Name} → IsGroundLayer={isGroundLayerNow}");
            }
        }
    }

    private void ClearAllPlacedRails()
    {
        _installedCubes.Clear();

        foreach (KeyValuePair<Vector2Int, PlacedRailInfo> kvp in _placedRails)
        {
            if (kvp.Value.Obj != null)
            {
                Addressables.ReleaseInstance(kvp.Value.Obj);
            }
        }
        _placedRails.Clear();
    }
}