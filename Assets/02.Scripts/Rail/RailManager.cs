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

        UpdateHover();
        UpdateClickInput();

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

    // 코너는 더 이상 수동 선택 대상이 아님: 직선 레일 하나만 배치하면
    // ApplyAutoConnect가 주변 상황에 맞춰 코너 모양으로 자동 전환함 (마인크래프트 레일 방식)
    private void HandlePlaceModeEntryInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkRailService.Instance?.RequestStartPlacement(RailType.Straight);
        }
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

    // 상하좌우 인접 칸 중 이미 레일이 깔린 방향을 찾음 (0=북, 1=동, 2=남, 3=서)
    private static readonly Vector2Int[] _cardinalOffsets =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
    };

    private List<int> GetConnectedDirections(Vector2Int gridIndex)
    {
        List<int> connected = new List<int>();
        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int neighbor = gridIndex + _cardinalOffsets[dir];
            if (_installedCubes.Contains(neighbor))
            {
                connected.Add(dir);
            }
        }
        return connected;
    }

    // 인접 레일 방향에 맞춰 레일 타입/회전을 자동으로 결정해서 프리뷰에 적용
    private void ApplyAutoConnect(Vector2Int gridIndex)
    {
        List<int> connectedDirs = GetConnectedDirections(gridIndex);
        if (connectedDirs.Count == 0)
        {
            return; // 주변에 레일이 없으면 수동 선택(1/2키, R키)을 그대로 유지
        }

        RailType autoType;
        int autoRotationStep;
        DetermineAutoShape(connectedDirs, out autoType, out autoRotationStep);

        _lastRotationStep = autoRotationStep;

        if (autoType != _currentRailType)
        {
            SwapPreviewType(autoType); // 호버 상태(_isHoveredCube)는 유지한 채로 프리뷰 프리팹만 교체
            return;
        }

        if (_previewController != null)
        {
            _previewController.SetRotationStep(autoRotationStep);
        }
    }

    // ChangeRailType과 달리 ClearHover를 거치지 않아 호버 상태를 유지한 채로 프리뷰 프리팹만 새로 로드함
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
            rotationStep = (connectedDirs[0] % 2 == 0) ? 1 : 0; // 0=동서, 1=남북 (프리팹 기본 방향에 맞춰 반전)
            return;
        }

        int dirA = connectedDirs[0];
        int dirB = connectedDirs[1]; // 3개 이상 연결된 경우는 현재 에셋으로 표현 불가하여 앞의 2방향만 사용

        bool isOpposite = Mathf.Abs(dirA - dirB) == 2;
        if (isOpposite)
        {
            type = RailType.Straight;
            rotationStep = (dirA % 2 == 0) ? 1 : 0;
        }
        else
        {
            type = RailType.Corner;
            rotationStep = GetCornerRotationStep(dirA, dirB);
        }
    }

    // 코너 프리팹의 rotationStep=0이 "북+동" 연결이라고 가정했던 원래 매핑에서
    // 직선 레일과 동일하게 실제 프리팹 기본 방향과 90도(한 스텝) 어긋나 있어 보정함
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

        // 회전은 더 이상 수동으로 하지 않으므로 onRotate는 넘기지 않음(null).
        // UIManager 쪽 팝업 프리팹에서도 회전 버튼을 숨기거나 비활성화해줘야 함.
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
        int rotationStepAtInstall = _previewController != null ? _previewController.CurrentRotationStep : _lastRotationStep;

        NetworkRailService.Instance?.ConsumeRailOnPlaced(railTypeAtInstall);

        SpawnPlacedRailAsync(gridIndex, cubeInfo.Center, _previewInstance.transform.rotation, railTypeAtInstall, rotationStepAtInstall).Forget();
    }

    private async UniTask SpawnPlacedRailAsync(Vector2Int gridIndex, Vector3 worldPos, Quaternion rotation, RailType railType, int rotationStep)
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

        _placedRails[gridIndex] = new PlacedRailInfo { Obj = spawnedRail, Type = railType, RotationStep = rotationStep };
        Debug.Log($"[RailManager] {railType} 레일 설치됨: " + spawnedRail.name);

        DroneManager.Deliver(spawnedRail, worldPos, rotation, AddRailToPath);

        // 방금 설치한 레일 때문에 옆에 이미 깔려있던 레일의 모양(직선↔코너, 회전)이
        // 바뀌어야 하는지 재계산해서, 필요하면 그 레일을 다시 스폰함
        UpdateNeighborShapes(gridIndex);


        ExitPlaceMode(clearPlacedRails: false);
    }

    // 인접한 4칸 중 이미 설치된 레일들을 대상으로, 방금 생긴 연결 때문에
    // 모양(직선/코너)이나 회전이 달라져야 하는지 다시 계산해서 다르면 재생성함
    // 단, 이미 양쪽이 다 연결된 '중간' 칸은 새로 하나 더 붙어도 모양을 바꾸지 않음(끝 칸만 갱신)
    private void UpdateNeighborShapes(Vector2Int changedIndex)
    {
        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int neighbor = changedIndex + _cardinalOffsets[dir];

            if (!_installedCubes.Contains(neighbor)) continue;
            if (!_placedRails.TryGetValue(neighbor, out PlacedRailInfo currentInfo)) continue;

            List<int> connectedDirs = GetConnectedDirections(neighbor);
            if (connectedDirs.Count == 0) continue; // 방금 자기 자신이 연결됐으니 이론상 발생 안 함

            // neighbor 입장에서 changedIndex 쪽을 가리키는 방향(방금 새로 생긴 연결)을 제외하면
            // 원래 몇 개의 연결이 있었는지 계산
            int dirTowardChanged = OppositeDirection(dir);
            int priorConnectionCount = connectedDirs.Contains(dirTowardChanged)
                ? connectedDirs.Count - 1
                : connectedDirs.Count;

            if (priorConnectionCount >= 2)
            {
                // 이미 양 끝이 연결된 중간 레일 - 여기서 옆으로 더 이어붙여도
                // 기존 직선/코너 모양을 유지해야 경로가 끊기지 않음. 끝 칸에서만 모양이 바뀜.
                continue;
            }

            RailType desiredType;
            int desiredRotationStep;
            DetermineAutoShape(connectedDirs, out desiredType, out desiredRotationStep);

            if (desiredType == currentInfo.Type && desiredRotationStep == currentInfo.RotationStep)
            {
                continue; // 모양 변화 없음
            }

            RespawnPlacedRailAsync(neighbor, desiredType, desiredRotationStep).Forget();
        }
    }

    // dir(0=북,1=동,2=남,3=서)의 반대 방향을 반환
    private static int OppositeDirection(int dir)
    {
        return (dir + 2) % 4;
    }

    // 이미 깔린 레일 하나를 새 모양/회전으로 다시 스폰함 (기존 오브젝트는 제거)
    private async UniTask RespawnPlacedRailAsync(Vector2Int gridIndex, RailType newType, int newRotationStep)
    {
        if (!_placedRails.TryGetValue(gridIndex, out PlacedRailInfo oldInfo)) return;
        if (!_cubeGrid.TryGetValue(gridIndex, out CubeInfo cubeInfo)) return;

        string address = newType == RailType.Corner ? _cornerRailAddress : _straightRailAddress;
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogWarning($"[RailManager] {newType} Rail Address가 비어있음(모양 갱신 실패): " + gridIndex);
            return;
        }

        Quaternion newRotation = Quaternion.Euler(0f, newRotationStep * 90f, 0f);

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, cubeInfo.Center, newRotation, Transform_RailRoot);
        GameObject spawnedRail = await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[RailManager] {newType} 레일 모양 갱신용 로드 실패: " + gridIndex);
            return;
        }

        if (oldInfo.Obj != null)
        {
            DroneManager.TryReplaceDelivery(oldInfo.Obj, spawnedRail);

            int pathIndex = _installedRailPath.IndexOf(oldInfo.Obj.transform);
            if (pathIndex != -1)
            {
                _installedRailPath[pathIndex] = spawnedRail.transform;
            }
            Addressables.ReleaseInstance(oldInfo.Obj);
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

        _placedRails[gridIndex] = new PlacedRailInfo { Obj = spawnedRail, Type = newType, RotationStep = newRotationStep };
        Debug.Log($"[RailManager] 인접 설치로 레일 모양 갱신됨: {gridIndex} → {newType} (rotStep={newRotationStep})");
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
            _installedRailPath.Remove(placedInfo.Obj.transform);
            Addressables.ReleaseInstance(placedInfo.Obj);
        }

        _placedRails.Remove(gridIndex);
        _installedCubes.Remove(gridIndex);

        NetworkRailService.Instance?.ReturnRailToInventory(placedInfo.Type);

        // 이 칸이 사라졌으니 인접 레일들도 모양이 다시 바뀔 수 있음 (코너였던 게 직선으로 복귀 등)
        UpdateNeighborShapes(gridIndex);

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

        if (!_installedRailPath.Contains(placedRail))
        {
            return;
        }

        Vector3 placedPos = placedRail.position;

        // 주변 1M(1칸) 내의 레일 콜라이더 탐색
        Collider[] hits = Physics.OverlapSphere(placedPos, 1.0f);
        List<Transform> stationRailsToAppend = new List<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTrans = hits[i].transform;

            // 이미 경로에 추가된 레일이면 제외
            if (_installedRailPath.Contains(hitTrans)) continue;

            // MapManager가 생성한 기본 레일 이름 감지
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
            // 방금 플레이어가 설치한 레일과 가까운 순서대로 정렬 (역 입구 -> 역 안쪽 순서)
            stationRailsToAppend.Sort((a, b) =>
                Vector3.Distance(placedPos, a.position).CompareTo(Vector3.Distance(placedPos, b.position))
            );

            // 경로 리스트 맨 뒤에 차례대로 추가
            for (int i = 0; i < stationRailsToAppend.Count; i++)
            {
                _installedRailPath.Add(stationRailsToAppend[i]);
            }

            Debug.Log($"[RailManager] 기차역 진입 레일 {stationRailsToAppend.Count}개가 경로 끝에 연결되었습니다!");
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

        Debug.Log($"[RailManager] 시작 출구 레일이 기본 경로로 등록되었습니다.");
    }

    private void AddRailToPath(GameObject rail)
    {
        if (rail == null)
        {
            return;
        }

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

    //레일 총개수 확인용
    public int GetRailCount()
    {
        return _installedRailPath.Count;
    }

}