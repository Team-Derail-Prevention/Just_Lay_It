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

    [Header("Installed Rail Order (Train Path)")]
    [SerializeField] private List<Transform> _installedRailPath = new List<Transform>();

    public float GhostAlpha { get { return _ghostAlpha; } }

    public bool IsPlaceModeActive { get { return _isPlaceModeActive; } }

    private RailType _currentRailType = RailType.Straight;

    private float _tileSize = 1f;
    private float _gridOriginX;
    private float _gridOriginZ;
    private float _groundPlaneY;

    private HashSet<Vector2Int> _installedCubes = new HashSet<Vector2Int>();
    private HashSet<Transform> _registeredRails = new HashSet<Transform>();

    private struct PlacedRailInfo
    {
        public GameObject Obj;
        public RailType Type;
        public int RotationStep;
        public bool IsFixed;
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
    private bool _isHoverSuppressed = false;

    private Vector2Int _pendingGridIndex;
    private CubeInfo _pendingCubeInfo;

    [Header("Blocked Tile Recheck")]
    [SerializeField] private float _blockedTileRecheckInterval = 1f;
    private float _blockedTileRecheckTimer;

    private int _lastRotationStep = 0;

    private string CurrentRailAddress
    {
        get => _currentRailType == RailType.Corner ? _cornerRailAddress : _straightRailAddress;
    }

    protected override void Init()
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

        if (_isHoverSuppressed)
        {
            return;
        }

        UpdateHover();
        UpdateClickInput();
    }

    public void ExitPlaceModeExternal()
    {
        ExitPlaceMode(clearPlacedRails: false);
    }

    public void SetHoverSuppressed(bool suppressed)
    {
        if (_isHoverSuppressed == suppressed)
        {
            return;
        }

        _isHoverSuppressed = suppressed;

        if (_isHoverSuppressed)
        {
            ClearHover();
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
        _isHoverSuppressed = false;

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
        RegisterFixedRails();
    }

    private void RegisterFixedRails()
    {
        if (Transform_MapRoot == null)
        {
            return;
        }

        Transform[] allChildren = Transform_MapRoot.GetComponentsInChildren<Transform>(true);
        int registeredCount = 0;

        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform railTrans = allChildren[i];
            if (!railTrans.name.Contains("AutoSpawned"))
            {
                continue;
            }

            Vector2Int gridIndex = WorldPointToGridIndex(railTrans.position);

            if (_installedCubes.Contains(gridIndex))
            {
                continue;
            }

            int rotationStep = Mathf.RoundToInt(railTrans.eulerAngles.y / 90f) % 4;

            _installedCubes.Add(gridIndex);
            _placedRails[gridIndex] = new PlacedRailInfo
            {
                Obj = railTrans.gameObject,
                Type = RailType.Straight,
                RotationStep = rotationStep,
                IsFixed = true
            };

            if (_cubeGrid.TryGetValue(gridIndex, out CubeInfo cubeInfo) && cubeInfo.TileScript != null)
            {
                cubeInfo.TileScript.HasRail = true;
            }

            registeredCount++;
        }

        Debug.Log($"[RailManager] 고정 레일 {registeredCount}개 격자 등록 완료");
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
            _previewController.SetRotationStep(_lastRotationStep);
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

            ApplyAutoConnect(gridIndex);

            if (_previewController == null || _previewOutline == null)
            {
                return; // 자동 연결로 타입이 바뀌어 프리팹을 새로 로드하는 중 - 로드 완료되면 자동으로 표시됨
            }

            bool isValidPlacement = IsPlacementValid(gridIndex, cubeInfo);

            _previewController.Show(cubeInfo);
            _previewOutline.Show(cubeInfo, isValidPlacement);
        }
    }

    private static readonly Vector2Int[] _cardinalOffsets =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
    };

    private List<int> GetRailPorts(RailType type, int rotStep)
    {
        List<int> ports = new List<int>();
        if (type == RailType.Straight)
        {
            if (rotStep % 2 == 0) { ports.Add(1); ports.Add(3); } // 동, 서 (가로)
            else { ports.Add(0); ports.Add(2); } // 북, 남 (세로)
        }
        else if (type == RailType.Corner)
        {
            // [교정완료] 실제 프리팹의 시각적 회전값에 맞게 포트 매핑 완전 수정
            if (rotStep == 0) { ports.Add(0); ports.Add(3); } // 북, 서 (0도)
            else if (rotStep == 1) { ports.Add(0); ports.Add(1); } // 북, 동 (90도 회전)
            else if (rotStep == 2) { ports.Add(1); ports.Add(2); } // 동, 남 (180도 회전)
            else if (rotStep == 3) { ports.Add(2); ports.Add(3); } // 남, 서 (270도 회전)
        }
        return ports;
    }

    private int GetActiveConnectionCount(Vector2Int target)
    {
        if (!_placedRails.TryGetValue(target, out PlacedRailInfo info)) return 0;

        List<int> ports = GetRailPorts(info.Type, info.RotationStep);
        int count = 0;

        foreach (int port in ports)
        {
            Vector2Int neighborPos = target + _cardinalOffsets[port];

            if (_placedRails.TryGetValue(neighborPos, out PlacedRailInfo neighborInfo))
            {
                int dirFromNeighborToMe = OppositeDirection(port);
                List<int> neighborPorts = GetRailPorts(neighborInfo.Type, neighborInfo.RotationStep);

                if (neighborPorts.Contains(dirFromNeighborToMe))
                {
                    count++;
                }
            }
        }
        return count;
    }

    // [핵심 수정] 상대방 구멍이 나를 향하고 있거나, 아직 미완성된 레일일 때만 스마트하게 연결을 시도합니다.
    private List<int> GetConnectedDirections(Vector2Int gridIndex)
    {
        List<int> connected = new List<int>();
        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int neighbor = gridIndex + _cardinalOffsets[dir];

            if (_placedRails.TryGetValue(neighbor, out PlacedRailInfo info))
            {
                int dirFromNeighborToMe = OppositeDirection(dir);
                List<int> neighborPorts = GetRailPorts(info.Type, info.RotationStep);

                // 상대방 구멍이 나를 향하고 있거나 || 상대방이 아직 완성 안 된 끝부분(연결 1개 이하)일 때만 연결!
                if (neighborPorts.Contains(dirFromNeighborToMe) || GetActiveConnectionCount(neighbor) < 2)
                {
                    connected.Add(dir);
                }
            }
        }
        return connected;
    }

    private void ApplyAutoConnect(Vector2Int gridIndex)
    {
        List<int> connectedDirs = GetConnectedDirections(gridIndex);
        if (connectedDirs.Count == 0)
        {
            _lastRotationStep = 0;

            if (_currentRailType != RailType.Straight)
            {
                SwapPreviewType(RailType.Straight);
            }
            else if (_previewController != null)
            {
                _previewController.SetRotationStep(0);
            }

            return;
        }

        RailType autoType;
        int autoRotationStep;
        DetermineAutoShape(connectedDirs, out autoType, out autoRotationStep);

        _lastRotationStep = autoRotationStep;

        if (autoType != _currentRailType)
        {
            SwapPreviewType(autoType);
            return;
        }

        if (_previewController != null)
        {
            _previewController.SetRotationStep(autoRotationStep);
        }
    }

    private void SwapPreviewType(RailType newType)
    {
        _currentRailType = newType;

        if (_previewInstance != null)
        {
            Addressables.ReleaseInstance(_previewInstance);
            _previewInstance = null;
            _previewOutline = null;
            _previewController = null;
        }

        SpawnPreviewInstanceAsync().Forget();
    }

    private void DetermineAutoShape(List<int> connectedDirs, out RailType type, out int rotationStep)
    {
        if (connectedDirs.Count == 1)
        {
            type = RailType.Straight;
            rotationStep = (connectedDirs[0] % 2 == 0) ? 1 : 0;
            return;
        }

        int oppositeAxisDir = FindOppositeAxisDirection(connectedDirs);
        if (oppositeAxisDir != -1)
        {
            type = RailType.Straight;
            rotationStep = (oppositeAxisDir % 2 == 0) ? 1 : 0;
            return;
        }

        int dirA = connectedDirs[0];
        int dirB = connectedDirs[1];

        type = RailType.Corner;
        rotationStep = GetCornerRotationStep(dirA, dirB);
    }

    private int FindOppositeAxisDirection(List<int> connectedDirs)
    {
        if (connectedDirs.Contains(0) && connectedDirs.Contains(2)) return 0;
        if (connectedDirs.Contains(1) && connectedDirs.Contains(3)) return 1;
        return -1;
    }

    private int GetCornerRotationStep(int dirA, int dirB)
    {
        int min = Mathf.Min(dirA, dirB);
        int max = Mathf.Max(dirA, dirB);

        if (min == 0 && max == 1) return 1; // 북+동 
        if (min == 1 && max == 2) return 2; // 동+남 
        if (min == 2 && max == 3) return 3; // 남+서 
        if (min == 0 && max == 3) return 0; // 서+북 

        return 0;
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

        if (!_isHoveredCube) return;
        if (!IsPlacementValid(_hoveredGridIndex, _hoveredCubeInfo)) return;

        OpenConfirmPopup(_hoveredGridIndex, _hoveredCubeInfo);
    }

    private void OpenConfirmPopup(Vector2Int gridIndex, CubeInfo cubeInfo)
    {
        if (_isConfirmPopupOpen) return;
        if (UIManager.Instance == null) return;

        _isConfirmPopupOpen = true;
        _pendingGridIndex = gridIndex;
        _pendingCubeInfo = cubeInfo;

        _previewController?.Show(cubeInfo);
        _previewOutline?.Show(cubeInfo, isValid: true);

        UIManager.Instance.OpenRailPlaceConfirmPopup(
            onConfirm: OnPopupConfirm,
            onCancel: OnPopupCancel
        );
    }

    private void CloseConfirmPopup()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.CloseRailPlaceConfirmPopup();
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
        if (!IsPlacementValid(gridIndex, cubeInfo)) return;

        _installedCubes.Add(gridIndex);

        if (cubeInfo.TileScript != null)
        {
            cubeInfo.TileScript.HasRail = true;
        }

        RailType railTypeAtInstall = _currentRailType;
        int rotationStepAtInstall = _previewController != null ? _previewController.CurrentRotationStep : _lastRotationStep;

        NetworkRailService.Instance?.ConsumeRailOnPlaced(railTypeAtInstall);

        SpawnPlacedRailAsync(gridIndex, cubeInfo.Center, _previewInstance.transform.rotation, railTypeAtInstall, rotationStepAtInstall).Forget();
    }

    private async UniTask SpawnPlacedRailAsync(Vector2Int gridIndex, Vector3 worldPos, Quaternion rotation, RailType railType, int rotationStep)
    {
        string address = railType == RailType.Corner ? _cornerRailAddress : _straightRailAddress;
        if (string.IsNullOrEmpty(address)) return;

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, worldPos, rotation, Transform_RailRoot);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded) return;

        RailOutline outline = spawnedRail.GetComponent<RailOutline>();
        if (outline != null) outline.enabled = false;

        RailPreviewController controller = spawnedRail.GetComponent<RailPreviewController>();
        if (controller != null) controller.enabled = false;

        Collider placedCollider = spawnedRail.GetComponentInChildren<Collider>();
        if (placedCollider != null) placedCollider.enabled = true;

        _placedRails[gridIndex] = new PlacedRailInfo { Obj = spawnedRail, Type = railType, RotationStep = rotationStep };

        DroneManager.Deliver(spawnedRail, worldPos, rotation, AddRailToPath);

        UpdateNeighborShapes(gridIndex);

        ExitPlaceMode(clearPlacedRails: false);
    }

    private void UpdateNeighborShapes(Vector2Int changedIndex)
    {
        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int neighbor = changedIndex + _cardinalOffsets[dir];

            if (!_installedCubes.Contains(neighbor)) continue;
            if (!_placedRails.TryGetValue(neighbor, out PlacedRailInfo currentInfo)) continue;

            List<int> connectedDirs = GetConnectedDirections(neighbor);
            if (connectedDirs.Count == 0) continue;

            if (GetActiveConnectionCount(neighbor) >= 2)
            {
                continue;
            }

            RailType desiredType;
            int desiredRotationStep;
            DetermineAutoShape(connectedDirs, out desiredType, out desiredRotationStep);

            if (desiredType == currentInfo.Type && desiredRotationStep == currentInfo.RotationStep)
            {
                continue;
            }

            RespawnPlacedRailAsync(neighbor, desiredType, desiredRotationStep).Forget();
        }
    }

    private static int OppositeDirection(int dir)
    {
        return (dir + 2) % 4;
    }

    private async UniTask RespawnPlacedRailAsync(Vector2Int gridIndex, RailType newType, int newRotationStep)
    {
        if (!_placedRails.TryGetValue(gridIndex, out PlacedRailInfo oldInfo)) return;
        if (!_cubeGrid.TryGetValue(gridIndex, out CubeInfo cubeInfo)) return;

        string address = newType == RailType.Corner ? _cornerRailAddress : _straightRailAddress;
        if (string.IsNullOrEmpty(address)) return;

        Quaternion newRotation = Quaternion.Euler(0f, newRotationStep * 90f, 0f);

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, cubeInfo.Center, newRotation, Transform_RailRoot);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded) return;

        if (oldInfo.Obj != null)
        {
            DroneManager.TryReplaceDelivery(oldInfo.Obj, spawnedRail);

            int pathIndex = _installedRailPath.IndexOf(oldInfo.Obj.transform);
            if (pathIndex != -1)
            {
                _installedRailPath[pathIndex] = spawnedRail.transform;
            }

            if (oldInfo.IsFixed)
            {
                Destroy(oldInfo.Obj);
            }
            else
            {
                Addressables.ReleaseInstance(oldInfo.Obj);
            }
        }

        RailOutline outline = spawnedRail.GetComponent<RailOutline>();
        if (outline != null) outline.enabled = false;

        RailPreviewController controller = spawnedRail.GetComponent<RailPreviewController>();
        if (controller != null) controller.enabled = false;

        Collider placedCollider = spawnedRail.GetComponentInChildren<Collider>();
        if (placedCollider != null) placedCollider.enabled = true;

        _placedRails[gridIndex] = new PlacedRailInfo
        {
            Obj = spawnedRail,
            Type = newType,
            RotationStep = newRotationStep,
            IsFixed = oldInfo.IsFixed
        };
    }

    private void RemoveRail(Vector2Int gridIndex, bool updateNeighbors = true)
    {
        if (!_installedCubes.Contains(gridIndex)) return;

        if (!_placedRails.TryGetValue(gridIndex, out PlacedRailInfo placedInfo))
        {
            _installedCubes.Remove(gridIndex);
            return;
        }

        if (placedInfo.IsFixed)
        {
            Debug.LogWarning($"[RailManager] {gridIndex}는 고정 레일이라 제거할 수 없습니다.");
            return;
        }

        if (_cubeGrid.TryGetValue(gridIndex, out CubeInfo cubeInfo) && cubeInfo.TileScript != null)
        {
            cubeInfo.TileScript.HasRail = false;
        }

        if (placedInfo.Obj != null)
        {
            _installedRailPath.Remove(placedInfo.Obj.transform);
            Addressables.ReleaseInstance(placedInfo.Obj);
        }

        _placedRails.Remove(gridIndex);
        _installedCubes.Remove(gridIndex);

        NetworkRailService.Instance?.ReturnRailToInventory(placedInfo.Type);

        if (updateNeighbors)
        {
            UpdateNeighborShapes(gridIndex);
        }

        Debug.Log($"[RailManager] {placedInfo.Type} 레일 회수됨: " + gridIndex);
    }

    public void RemoveAllRail()
    {
        if (_placedRails.Count == 0) return;

        List<Vector2Int> gridIndices = new List<Vector2Int>(_placedRails.Keys);
        foreach (Vector2Int gridIndex in gridIndices)
        {
            RemoveRail(gridIndex, false);
        }

        Debug.Log("[RailManager] 설치된 레일 전체 회수 완료");
    }

    private void RecheckBlockedTiles()
    {
        if (_cubeGrid.Count == 0) return;

        List<Vector2Int> keys = new List<Vector2Int>(_cubeGrid.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Vector2Int key = keys[i];
            CubeInfo info = _cubeGrid[key];

            if (info.IsGroundLayer) continue;
            if (info.Obj == null) continue;

            bool isGroundLayerNow = IsInGroundLayer(info.Obj.layer);
            if (isGroundLayerNow != info.IsGroundLayer)
            {
                info.IsGroundLayer = isGroundLayerNow;
                _cubeGrid[key] = info;
            }
        }
    }

    private void ClearAllPlacedRails()
    {
        _installedCubes.Clear();
        _installedRailPath.Clear();

        foreach (KeyValuePair<Vector2Int, PlacedRailInfo> kvp in _placedRails)
        {
            if (kvp.Value.Obj != null)
            {
                Addressables.ReleaseInstance(kvp.Value.Obj);
            }
        }
        _placedRails.Clear();
    }

    private void ConnectStationRails(Transform placedRail)
    {
        if (placedRail == null) return;
        if (!_installedRailPath.Contains(placedRail)) return;

        Vector3 placedPos = placedRail.position;

        Collider[] hits = Physics.OverlapSphere(placedPos, 1.0f);
        List<Transform> stationRailsToAppend = new List<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTrans = hits[i].transform;

            if (_installedRailPath.Contains(hitTrans)) continue;

            if (hits[i].name.Contains("AutoSpawned"))
            {
                Transform dirRoot = hitTrans.parent;
                if (dirRoot != null)
                {
                    for (int c = 0; c < dirRoot.childCount; c++)
                    {
                        Transform rail = dirRoot.GetChild(c);
                        if (!_installedRailPath.Contains(rail))
                        {
                            stationRailsToAppend.Add(rail);
                        }
                    }
                    break;
                }
            }
        }

        if (stationRailsToAppend.Count > 0)
        {
            stationRailsToAppend.Sort((a, b) =>
                Vector3.Distance(placedPos, a.position).CompareTo(Vector3.Distance(placedPos, b.position))
            );

            for (int i = 0; i < stationRailsToAppend.Count; i++)
            {
                _installedRailPath.Add(stationRailsToAppend[i]);
            }
        }
    }

    public void InitStartingRailPath(Transform dirRoot)
    {
        _installedRailPath.Clear();
        if (dirRoot == null) return;

        for (int i = 0; i < dirRoot.childCount; i++)
        {
            Transform rail = dirRoot.GetChild(i);
            _installedRailPath.Add(rail);
        }
    }

    private void AddRailToPath(GameObject rail)
    {
        if (rail == null) return;

        Transform railTrans = rail.transform;
        _installedRailPath.Add(rail.transform);
        ConnectStationRails(railTrans);
    }

    public Transform GetRailNode(int index)
    {
        if (index >= 0 && index < _installedRailPath.Count)
        {
            return _installedRailPath[index];
        }
        return null;
    }

    public int GetRailCount()
    {
        return _installedRailPath.Count;
    }
}