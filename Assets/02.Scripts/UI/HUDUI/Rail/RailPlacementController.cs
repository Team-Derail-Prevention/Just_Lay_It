using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

public class RailPlacementController : MonoBehaviour
{
    [Header("선로 프리팹")]
    [SerializeField] private GameObject _prefabStraightRail;
    [SerializeField] private GameObject _prefabCurveRail;

    [Header("배치 판정")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private float _overlapCheckRadius = 1f; // 선로 크기 맞게 조정
    [SerializeField] private float _gridCellSize = 2f; // 선로 길이 맞게 조정 (직선 선로 한 칸 크기)

    [Header("연출")]
    [SerializeField] private float _hoverHeight = 1.5f; // 배치 중 공중에 떠있는 높이
    [SerializeField] private float _dropDuration = 0.4f; // 확정 후 낙하 연출 시간

    [Header("설치 연출")]
    [SerializeField] private Material _validMaterial;   // 초록 (배치 가능)
    [SerializeField] private Material _invalidMaterial; // 빨강 (배치 불가)
    [SerializeField] private Material _placedMaterial;  // 반투명 (배치 완료, 실체화 대기)

    [Header("바닥 타일 표시")]
    [SerializeField] private float _footprintYOffset = 0.05f; // 바닥과의 Z-fighting 방지용 살짝 띄우는 높이

    [Header("배치 취소 가능")]
    [SerializeField] private LayerMask _placedRailLayerMask;

    private GameObject _curGhostObject;
    private GameObject _footprintIndicator;
    private ERailType _curRailType;
    private bool _isValidPlacement;
    private bool _isWaitingConfirm;
    private Vector2Int _curGridCell;

    private readonly HashSet<Vector2Int> _occupiedGridCells = new HashSet<Vector2Int>();

    private void Start()
    {
        if (NetworkRailService.Instance == null)
        {
            Debug.LogError("[RailPlacementController] NetworkRailService.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
            return;
        }

        NetworkRailService.Instance.OnRequestPlaceMode += OnRequestPlaceMode;
    }

    private void OnDisable()
    {
        if (NetworkRailService.Instance != null)
        {
            NetworkRailService.Instance.OnRequestPlaceMode -= OnRequestPlaceMode;
        }
    }

    private void OnRequestPlaceMode(ERailType railType)
    {
        if (_curGhostObject != null)
        {
            CancelPlacement();
        }

        GameObject prefab = (railType == ERailType.Straight) ? _prefabStraightRail : _prefabCurveRail;
        if (prefab == null)
        {
            Debug.LogWarning($"[RailPlacementController] {railType} 프리팹이 연결되지 않았습니다.");
            return;
        }

        _curRailType = railType;
        _curGhostObject = Instantiate(prefab);
        SetGhostCollidersEnabled(false);
        _isWaitingConfirm = false;

        CreateFootprintIndicator();
    }

    private void CreateFootprintIndicator()
    {
        if (_footprintIndicator != null)
        {
            Destroy(_footprintIndicator);
        }

        _footprintIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _footprintIndicator.name = "FootprintIndicator";
        Destroy(_footprintIndicator.GetComponent<Collider>());

        _footprintIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _footprintIndicator.transform.localScale = new Vector3(_gridCellSize, _gridCellSize, 1f);
    }

    private void Update()
    {
        if (_curGhostObject == null)
        {
            CheckCancelPlacedRailInput();
            return;
        }

        if (_isWaitingConfirm == true)
        {
            return;
        }

        UpdateFollowMouse();
        CheckRotateInput();
        CheckConfirmInput();
    }

    private void CheckCancelPlacedRailInput()
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        bool isLeftClickPressed = Mouse.current.leftButton.wasPressedThisFrame;
        if (isLeftClickPressed == false)
        {
            return;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        bool isHit = Physics.Raycast(ray, out RaycastHit hit, 1000f, _placedRailLayerMask);
        if (isHit == false)
        {
            Debug.Log("[DEBUG] 레이가 아무것도 못 맞췄음 - LayerMask 또는 콜라이더 확인 필요");
            return;
        }

        Debug.Log($"[DEBUG] 레이가 맞은 오브젝트 : {hit.collider.gameObject.name}");

        var placedInfo = hit.collider.GetComponentInParent<PlacedRailInfo>();
        if (placedInfo == null)
        {
            Debug.Log("[DEBUG] PlacedRailInfo를 못 찾음 - 아직 실체화 안 됐다면 이상함");
            return;
        }

        CancelPlacedRail(placedInfo);
    }

    private void CancelPlacedRail(PlacedRailInfo placedInfo)
    {
        _occupiedGridCells.Remove(placedInfo.GridCell);
        NetworkRailService.Instance.ReturnRailToInventory(placedInfo.RailType);
        Destroy(placedInfo.gameObject);
    }

    private void UpdateFollowMouse()
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        bool isHitGround = Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayerMask);
        if (isHitGround == false)
        {
            return;
        }

        _curGridCell = WorldPosToGridCell(hit.point);
        Vector3 snappedGroundPos = GridCellToWorldPos(_curGridCell, hit.point.y);
        Vector3 hoverPos = snappedGroundPos + Vector3.up * _hoverHeight;
        _curGhostObject.transform.position = hoverPos;

        bool isCellOccupied = _occupiedGridCells.Contains(_curGridCell);
        bool hasObstacle = CheckObstacleByActualBounds(snappedGroundPos);
        _isValidPlacement = (hasObstacle == false && isCellOccupied == false);

        Material targetMaterial = _isValidPlacement ? _validMaterial : _invalidMaterial;
        UpdateFootprintIndicator(snappedGroundPos, targetMaterial);
    }

    private bool CheckObstacleByActualBounds(Vector3 placedGroundPos)
    {
        Bounds hoverBounds = GetGhostBounds();
        if (hoverBounds.size == Vector3.zero)
        {
            return false;
        }

        Vector3 checkCenter = placedGroundPos + Vector3.up * (hoverBounds.extents.y);
        Vector3 halfExtents = hoverBounds.extents * 0.9f;

        return Physics.CheckBox(checkCenter, halfExtents, _curGhostObject.transform.rotation, _obstacleLayerMask);
    }

    private Bounds GetGhostBounds()
    {
        var rendererList = _curGhostObject.GetComponentsInChildren<Renderer>();
        if (rendererList.Length == 0)
        {
            return new Bounds(_curGhostObject.transform.position, Vector3.zero);
        }

        Bounds bounds = rendererList[0].bounds;
        for (int i = 1; i < rendererList.Length; i++)
        {
            bounds.Encapsulate(rendererList[i].bounds);
        }

        return bounds;
    }


    private void UpdateFootprintIndicator(Vector3 snappedGroundPos, Material targetMaterial)
    {
        if (_footprintIndicator == null)
        {
            return;
        }

        _footprintIndicator.transform.position = snappedGroundPos + Vector3.up * _footprintYOffset;

        var footprintRenderer = _footprintIndicator.GetComponent<Renderer>();
        if (footprintRenderer != null)
        {
            footprintRenderer.material = targetMaterial;
        }
    }

    private void CheckRotateInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame == true)
        {
            RotateGhost();
        }
    }

    private void RotateGhost()
    {
        if (_curGhostObject == null)
        {
            return;
        }

        _curGhostObject.transform.Rotate(Vector3.up, 90f, Space.World);
    }

    private void CheckConfirmInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        bool isRightClickPressed = Mouse.current.rightButton.wasPressedThisFrame;
        if (isRightClickPressed == true && _isValidPlacement == true)
        {
            LockPlacementAndOpenConfirmPopup();
        }
    }

    private void LockPlacementAndOpenConfirmPopup()
    {
        _isWaitingConfirm = true;

        UIManager.Instance.OpenRailPlaceConfirmPopup(RotateGhost, OnClickConfirmPlacement, CancelPlacement);
    }

    private void OnClickConfirmPlacement()
    {
        CloseConfirmPopup();
        DropGhostToGroundAsync().Forget();
    }

    private async UniTaskVoid DropGhostToGroundAsync()
    {
        if (_curGhostObject == null)
        {
            return;
        }

        Vector3 startPos = _curGhostObject.transform.position;
        Vector3 groundPos = startPos - Vector3.up * _hoverHeight;

        float elapsed = 0f;
        while (elapsed < _dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _dropDuration);
            _curGhostObject.transform.position = Vector3.Lerp(startPos, groundPos, t);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _curGhostObject.transform.position = groundPos;
        FinalizePlacement();
    }

    private void FinalizePlacement()
    {
        ApplyGhostMaterial(_placedMaterial);
        SetGhostCollidersEnabled(true);
        _occupiedGridCells.Add(_curGridCell); 
        DestroyFootprintIndicator();

        var placedInfo = _curGhostObject.AddComponent<PlacedRailInfo>();
        placedInfo.RailType = _curRailType;
        placedInfo.GridCell = _curGridCell;

        NetworkRailService.Instance.ConsumeRailOnPlaced(_curRailType);

        _curGhostObject = null;
        _isWaitingConfirm = false;
    }

    private void CancelPlacement()
    {
        CloseConfirmPopup();
        DestroyFootprintIndicator();

        if (_curGhostObject != null)
        {
            Destroy(_curGhostObject);
            _curGhostObject = null;
        }

        _isWaitingConfirm = false;
    }

    private void DestroyFootprintIndicator()
    {
        if (_footprintIndicator != null)
        {
            Destroy(_footprintIndicator);
            _footprintIndicator = null;
        }
    }

    private void CloseConfirmPopup()
    {
        UIManager.Instance.CloseRailPlaceConfirmPopup();
    }

    private void ApplyGhostMaterial(Material material)
    {
        if (_curGhostObject == null || material == null)
        {
            return;
        }

        var rendererList = _curGhostObject.GetComponentsInChildren<Renderer>();
        foreach (var rendererItem in rendererList)
        {
            int subMeshCount = rendererItem.sharedMaterials.Length;
            var newMaterials = new Material[subMeshCount];
            for (int i = 0; i < subMeshCount; i++)
            {
                newMaterials[i] = material;
            }

            rendererItem.materials = newMaterials;
        }
    }

    private void SetGhostCollidersEnabled(bool isEnabled)
    {
        if (_curGhostObject == null)
        {
            return;
        }

        var colliderList = _curGhostObject.GetComponentsInChildren<Collider>();
        foreach (var colliderItem in colliderList)
        {
            colliderItem.enabled = isEnabled;
        }
    }

    private Vector2Int WorldPosToGridCell(Vector3 worldPos)
    {
        int cellX = Mathf.RoundToInt(worldPos.x / _gridCellSize);
        int cellZ = Mathf.RoundToInt(worldPos.z / _gridCellSize);
        return new Vector2Int(cellX, cellZ);
    }

    private Vector3 GridCellToWorldPos(Vector2Int gridCell, float worldY)
    {
        float worldX = gridCell.x * _gridCellSize;
        float worldZ = gridCell.y * _gridCellSize;
        return new Vector3(worldX, worldY, worldZ);
    }
}
