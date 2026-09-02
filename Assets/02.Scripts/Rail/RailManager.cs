using Cysharp.Threading.Tasks;
using System.Collections.Generic;
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


    //카메라 자동 등록
    private Camera _mainCamera;
    public Camera Camera_Main
    {
        get
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }
    }

    private RailType _currentRailType = RailType.Straight;

    private float _tileSize = 1f;
    private float _gridOriginX;
    private float _gridOriginZ;
    private float _groundPlaneY;

    //출발한 역/터미널의 루트를 기억 (출발지 역 전체의 오감지 방지)
    private Transform _departureStationRoot;
    private Transform _currentDepartureGateRoot;

    private HashSet<Vector2Int> _installedCubes = new HashSet<Vector2Int>();

    private struct PlacedRailInfo
    {
        public GameObject Obj;
        public RailType Type;
        public int RotationStep;
        public bool IsFixed;
        public bool IsDelivered;
    }
    private Dictionary<Vector2Int, PlacedRailInfo> _placedRails = new Dictionary<Vector2Int, PlacedRailInfo>();
    private bool _hasAnnouncedStationArrival;

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += StopPlaceMode;
        }
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged += StopPlaceMode;
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

    private void StopPlaceMode(GameState gameState)
    {
        if (gameState != GameState.Playing)
        {
            ExitPlaceMode();
        }
    }

    private void EnterPlaceMode(RailType railType)
    {
        if (_isPlaceModeActive)
        {
            Debug.Log("[RailManager] 이미 배치 모드가 활성화되어 있습니다.");
            return;
        }

        if (GameManager.Instance.CurrentGameState != GameState.Playing)
        {
            Debug.LogWarning("[RailManager] 게임 플레이 중에만 레일을 설치할 수 있습니다.");
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
                IsFixed = true,
                IsDelivered = true
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
        //카메라 자동 등록
        if (Camera_Main == null) return;

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
            // 실제 프리팹의 시각적 회전값에 맞게 포트 매핑 수정
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

    // [수정] 상대방 구멍이 나를 향하고 있거나, 아직 미완성된 레일일 때만 연결을 시도합니다.
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
        if (placedCollider != null) placedCollider.enabled = false;

        _placedRails[gridIndex] = new PlacedRailInfo { Obj = spawnedRail, Type = railType, RotationStep = rotationStep, IsDelivered = false };

        // [수정] AddRailToPath 대신, 배달 완료 시 그래프 전체를 재구성하는 OnRailDelivered 콜백으로 교체
        DroneManager.Deliver(spawnedRail, worldPos, rotation, OnRailDelivered);

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

        Transform parentForNewRail = (oldInfo.IsFixed && oldInfo.Obj != null)
            ? oldInfo.Obj.transform.parent
            : Transform_RailRoot;

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, cubeInfo.Center, newRotation, parentForNewRail);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded) return;

        if (oldInfo.IsFixed)
        {
            spawnedRail.name = "AutoSpawned_" + newType.ToString() + "Rail";
        }

        if (oldInfo.Obj != null)
        {
            DroneManager.TryReplaceDelivery(oldInfo.Obj, spawnedRail);

            // [수정] _installedRailPath 안의 옛 Transform을 수동으로 찾아 바꾸던 코드 제거
            // -> RebuildInstalledRailPath()가 _placedRails에서 항상 최신 Transform을 읽어오므로 불필요해짐

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
            IsFixed = oldInfo.IsFixed,
            IsDelivered = oldInfo.IsDelivered
        };

        // [추가] 모양이 바뀐 뒤 경로를 그래프 스냅샷 기준으로 다시 계산
        RebuildInstalledRailPath();
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
            // [수정] _installedRailPath.Remove(...) 수동 제거 코드 삭제
            // -> RebuildInstalledRailPath()가 _placedRails 기준으로 리스트를 통째로 새로 만들므로 불필요해짐
            Addressables.ReleaseInstance(placedInfo.Obj);
        }

        _placedRails.Remove(gridIndex);
        _installedCubes.Remove(gridIndex);

        NetworkRailService.Instance?.ReturnRailToInventory(placedInfo.Type);

        if (updateNeighbors)
        {
            UpdateNeighborShapes(gridIndex);
        }

        // [추가] 레일 회수 후 경로 재구성
        RebuildInstalledRailPath();

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
        if (_installedRailPath.Count == 0) return;
        if (_installedRailPath[_installedRailPath.Count - 1] != placedRail) return;

        Vector3 placedPos = placedRail.position;
        Vector2Int placedGrid = WorldPointToGridIndex(placedPos);

        Vector3 forwardDir = placedRail.forward;
        if (_installedRailPath.Count >= 2)
        {
            Vector3 prevPos = _installedRailPath[_installedRailPath.Count - 2].position;
            forwardDir = (placedPos - prevPos).normalized;
        }
        forwardDir.y = 0f;

        Collider[] hits = Physics.OverlapSphere(placedPos, 1.2f);
        List<Transform> stationRailsToAppend = new List<Transform>();
        HashSet<Transform> processedRoots = new HashSet<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTrans = hits[i].transform;

            if (_installedRailPath.Contains(hitTrans)) continue;

            bool isAutoSpawned = hitTrans.name.Contains("AutoSpawned") || (hitTrans.parent != null && hitTrans.parent.name.Contains("AutoSpawned"));

            if (isAutoSpawned)
            {
                Transform touchedRail = hitTrans.name.Contains("AutoSpawned") ? hitTrans : hitTrans.parent;
                Transform dirRoot = touchedRail.parent;

                if (_currentDepartureGateRoot != null && (dirRoot == _currentDepartureGateRoot || touchedRail.IsChildOf(_currentDepartureGateRoot)))
                {
                    continue;
                }

                if (dirRoot == null && _departureStationRoot != null && touchedRail.IsChildOf(_departureStationRoot))
                {
                    int startRailCount = _currentDepartureGateRoot != null ? _currentDepartureGateRoot.childCount : 0;
                    int playerRailCount = _installedRailPath.Count - startRailCount;
                    if (playerRailCount < 3)
                    {
                        continue;
                    }
                }

                Vector3 toTargetDir = (touchedRail.position - placedPos).normalized;
                toTargetDir.y = 0f;
                if (Vector3.Dot(forwardDir, toTargetDir) < -0.1f)
                {
                    continue;
                }

                Vector2Int touchedGrid = WorldPointToGridIndex(touchedRail.position);
                int diffX = Mathf.Abs(touchedGrid.x - placedGrid.x);
                int diffY = Mathf.Abs(touchedGrid.y - placedGrid.y);

                float worldDist = Vector3.Distance(placedPos, touchedRail.position);
                if (diffX + diffY != 1 && worldDist > 1.2f)
                {
                    continue;
                }


                if (dirRoot != null && !processedRoots.Contains(dirRoot))
                {
                    processedRoots.Add(dirRoot);

                    for (int c = 0; c < dirRoot.childCount; c++)
                    {
                        Transform rail = dirRoot.GetChild(c);
                        if (!_installedRailPath.Contains(rail))
                        {
                            stationRailsToAppend.Add(rail);
                        }
                    }
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
            if (_hasAnnouncedStationArrival == false)
            {
                _hasAnnouncedStationArrival = true;

                Debug.Log("[RailManager] 기차역 레일 연결성공");
                SoundManager.Instance?.PlaySFX(SfxAddress.Train.Arrive);
                DroneManager.Instance?.RecallAllAndSuspendMining();
            }

            //레일 전체 연결 완료 -> 기차 속도 부스트 적용
            TrainManager.Instance?.SetTrainSpeedBoost(true);
        }
    }

    public void InitStartingRailPath(Transform dirRoot)
    {
        _currentDepartureGateRoot = dirRoot;
        _hasAnnouncedStationArrival = false;
        if (dirRoot == null)
        {
            _installedRailPath.Clear();
            return;

        }

        StationObject stationObj = dirRoot.GetComponentInParent<StationObject>();
        CentralTerminal terminalObj = dirRoot.GetComponentInParent<CentralTerminal>();

        if (stationObj != null)
        {
            _departureStationRoot = stationObj.transform;
        }
        else if (terminalObj != null)
        {
            _departureStationRoot = terminalObj.transform;
        }
        else
        {
            _departureStationRoot = dirRoot.parent != null ? dirRoot.parent : dirRoot;
        }

        RebuildInstalledRailPath();
        Debug.Log($"[RailManager] 시작 출구 레일 {_installedRailPath.Count}개 등록 완료");
    }

    // [수정] AddRailToPath를 대체 - 배달 완료 시 콜라이더만 켜주고, 경로 갱신은 RebuildInstalledRailPath에 위임
    private void OnRailDelivered(GameObject rail)
    {
        if (rail == null) return;

        Collider placedCollider = rail.GetComponentInChildren<Collider>();
        if (placedCollider != null)
        {
            placedCollider.enabled = true;
        }

        MarkRailDelivered(rail);

        RebuildInstalledRailPath();
    }

    private void MarkRailDelivered(GameObject rail)
    {
        Vector2Int foundGrid = default;
        bool isFound = false;

        foreach (KeyValuePair<Vector2Int, PlacedRailInfo> kvp in _placedRails)
        {
            if (kvp.Value.Obj != rail)
            {
                continue;
            }

            foundGrid = kvp.Key;
            isFound = true;

            break;
        }

        if (isFound == false)
        {
            return;
        }

        PlacedRailInfo info = _placedRails[foundGrid];

        info.IsDelivered = true;

        _placedRails[foundGrid] = info;
    }

    // [추가] fromGrid에서 포트가 맞물리는 다음 레일을 찾음. visited에 있는 칸은 건너뜀.
    // 후보가 2개 이상이면(3방향 접합 등 설계상 없어야 할 분기) 경고 로그를 남기고 첫 번째만 사용.
    private bool FindNextConnectedRail(Vector2Int fromGrid, HashSet<Vector2Int> visited, out Transform nextRail, out Vector2Int nextGrid, out bool nextIsFixed)
    {
        nextRail = null;
        nextGrid = default;
        nextIsFixed = false;

        if (!_placedRails.TryGetValue(fromGrid, out PlacedRailInfo fromInfo)) return false;

        List<int> ports = GetRailPorts(fromInfo.Type, fromInfo.RotationStep);
        int matchCount = 0;

        for (int i = 0; i < ports.Count; i++)
        {
            int port = ports[i];
            Vector2Int candidateGrid = fromGrid + _cardinalOffsets[port];

            if (visited.Contains(candidateGrid)) continue;
            if (!_placedRails.TryGetValue(candidateGrid, out PlacedRailInfo candidateInfo)) continue;
            if (candidateInfo.Obj == null) continue;
            if (candidateInfo.IsDelivered == false) continue;

            int dirFromCandidateToMe = OppositeDirection(port);
            List<int> candidatePorts = GetRailPorts(candidateInfo.Type, candidateInfo.RotationStep);
            if (!candidatePorts.Contains(dirFromCandidateToMe)) continue; // 서로 포트가 맞물려야 인정

            matchCount++;
            if (matchCount == 1)
            {
                nextRail = candidateInfo.Obj.transform;
                nextGrid = candidateGrid;
                nextIsFixed = candidateInfo.IsFixed;
            }
        }

        if (matchCount > 1)
        {
            Debug.LogWarning($"[RailManager] {fromGrid}에서 분기(3방향 이상 연결) 감지됨 - 첫 번째 후보만 사용합니다.");
        }

        return matchCount > 0;
    }

    // [추가] 출발 게이트부터 _placedRails 그래프를 매번 다시 순회해 _installedRailPath를 재구성.
    // 드론 배달/배치 순서와 무관하게 항상 그 순간의 실제 연결 상태를 정확히 반영함.
    private void RebuildInstalledRailPath()
    {
        // 경로 재계산 전 일단 부스트 해제
        TrainManager.Instance?.SetTrainSpeedBoost(false);

        List<Transform> newPath = new List<Transform>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        if (_currentDepartureGateRoot == null)
        {
            _installedRailPath = newPath;
            return;
        }

        // 1. 출발 게이트 고정 레일들을 시작 구간으로 등록
        for (int i = 0; i < _currentDepartureGateRoot.childCount; i++)
        {
            Transform gateRail = _currentDepartureGateRoot.GetChild(i);
            newPath.Add(gateRail);
            visited.Add(WorldPointToGridIndex(gateRail.position));
        }

        if (newPath.Count == 0)
        {
            _installedRailPath = newPath;
            return;
        }

        Vector2Int currentGrid = WorldPointToGridIndex(newPath[newPath.Count - 1].position);

        // 2. 그래프를 따라 계속 다음(플레이어가 놓은) 레일을 찾아 이어붙임
        for (int step = 0; step < 500; step++)
        {
            if (!FindNextConnectedRail(currentGrid, visited, out Transform nextRail, out Vector2Int nextGrid, out bool nextIsFixed))
            {
                break;
            }

            if (nextIsFixed)
            {
                // [핵심] 다른 역/터미널의 고정 레일 영역에 도달함 - 그래프 인접성만으로
                // 그냥 들어가지 않고, ConnectStationRails의 물리 기반 검증
                // (방향 체크, 자기 출발지 재진입 방지, 유예 칸수 등)에 맡기고 순회 종료
                break;
            }

            newPath.Add(nextRail);
            visited.Add(nextGrid);
            currentGrid = nextGrid;
        }

        _installedRailPath = newPath;

        if (_installedRailPath.Count > 0)
        {
            ConnectStationRails(_installedRailPath[_installedRailPath.Count - 1]);
        }
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

    public void SyncGhostRailData()
    {
        // 1. 파괴된 레일(고스트) 캐시 정리
        List<Vector2Int> keysToRemove = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, PlacedRailInfo> kvp in _placedRails)
        {
            if (kvp.Value.Obj == null) // 실제 게임오브젝트가 이미 파괴되었다면
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            _placedRails.Remove(keysToRemove[i]);
            _installedCubes.Remove(keysToRemove[i]);
        }

        // 2. 타일 스크립트(HasRail) 강제 동기화
        foreach (KeyValuePair<Vector2Int, CubeInfo> kvp in _cubeGrid)
        {
            if (kvp.Value.TileScript != null)
            {
                // 매니저가 기억하는 레일이 없는데 HasRail이 true로 남아있다면 찌꺼기로 간주하고 강제 해제
                if (kvp.Value.TileScript.HasRail && !_installedCubes.Contains(kvp.Key))
                {
                    kvp.Value.TileScript.HasRail = false;
                }
            }
        }

        // 3. 바닥 레이어(IsGroundLayer) 즉시 갱신 및 경로 재구성
        RecheckBlockedTiles();
        RebuildInstalledRailPath();

        Debug.Log($"[RailManager] 고스트 레일 데이터 동기화 완료 (제거된 찌꺼기: {keysToRemove.Count}개)");
    }
}