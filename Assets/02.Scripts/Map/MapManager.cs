using Cysharp.Threading.Tasks;
using Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapManager : SingletonBase<MapManager>
{
    [Header("Map Settings")]
    [SerializeField] private Transform _mapRoot;
    [SerializeField] private float _mapSpacing = 30f;
    [SerializeField] private MapSkirtMaker _skirtMaker;

    [Header("Auto Spawn Rail Settings")]
    [SerializeField] private string _straightRailAddress = "Prefab/Rail_Straight";
    [SerializeField] private float _railSpawnOffset = 2f;

    private readonly Vector3Int[] _initialMapOffsets =
    {
        new Vector3Int(-1, 0, 1),  new Vector3Int(0, 0, 1),  new Vector3Int(1, 0, 1),
        new Vector3Int(1, 0, 0),   new Vector3Int(1, 0, -1), new Vector3Int(0, 0, -1),
        new Vector3Int(-1, 0, -1), new Vector3Int(-1, 0, 0)
    };

    private readonly Dictionary<Vector3Int, GameObject> _spawnedMaps = new();
    private readonly Dictionary<Vector3Int, int> _mapTypeData = new();
    private readonly List<MapTileInfo> _spawnedTiles = new();

    private int _currentRadius = 1;
    private bool _isExpanding;

    public event Action<Dictionary<Vector3Int, int>> OnMapGenerated;
    public Transform MapRoot => _mapRoot;

    protected override void Init()
    {
        base.Init();
        InitMapRoot();
    }

    private async void Start()
    {
        await NeedDataLoadAsync();
        Debug.Log("[MapManager] 데이터 로드 대기 완료. GameManager의 시작 명령을 기다립니다.");
    }

    private void InitMapRoot()
    {
        GameObject existingRoot = GameObject.Find("@MapRoot");

        if (existingRoot != null)
        {
            DestroyImmediate(existingRoot);
        }

        GameObject rootObj = new("@MapRoot");
        _mapRoot = rootObj.transform;
        _mapRoot.position = Vector3.zero;
        _mapRoot.rotation = Quaternion.identity;
    }

    private async UniTask NeedDataLoadAsync()
    {
        if (GameManager.Data != null && GameManager.Data.IsLoaded)
        {
            return;
        }

        UniTaskCompletionSource tcs = new();

        if (GameManager.Data != null)
        {
            GameManager.Data.OnDataLoadCompleted += () => tcs.TrySetResult();
        }
        else
        {
            Debug.LogError("[MapManager] DataManager 인스턴스를 찾을 수 없습니다!");
            return;
        }

        await tcs.Task;
    }

    public async UniTask<bool> GenerateMapAsync(CancellationToken cancellationToken = default)
    {
        if (GameManager.Data == null)
        {
            Debug.LogError("[MapManager] DataManager 인스턴스가 존재하지 않습니다.");
            return false;
        }

        IReadOnlyList<MapData> allMapDatas = GameManager.Data.GetAllData<MapData>();

        if (allMapDatas == null || allMapDatas.Count == 0)
        {
            Debug.LogError("[MapManager] MapData를 가져오지 못했습니다.");
            return false;
        }

        MapData centralData = null;
        List<MapData> stationDatas = new();
        List<MapData> normalDatas = new();

        foreach (MapData data in allMapDatas)
        {
            if (data == null)
            {
                continue;
            }

            if (data.Type == MapTypeConst.CentralTerminal)
            {
                centralData = data;
            }
            else if (data.Type == MapTypeConst.Station)
            {
                stationDatas.Add(data);
            }
            else if (data.Type == MapTypeConst.Normal)
            {
                normalDatas.Add(data);
            }
        }

        if (centralData == null)
        {
            Debug.LogError("[MapManager] CentralTerminal 타입의 MapData를 찾을 수 없습니다.");
            return false;
        }

        if (stationDatas.Count == 0 || normalDatas.Count == 0)
        {
            Debug.LogError("[MapManager] Station 또는 Normal 타입의 MapData가 없습니다.");
            return false;
        }

        _currentRadius = 1;

        await SpawnMapFromDataAsync(centralData, Vector3Int.zero, 2, "CentralTerminal", cancellationToken);

        List<bool> assignedTypes = CreateInitialStationLayout();

        for (int i = 0; i < _initialMapOffsets.Length; i++)
        {
            Vector3Int gridPos = _initialMapOffsets[i];
            bool isStation = assignedTypes[i];

            MapData selectedData = isStation ? stationDatas[Random.Range(0, stationDatas.Count)] : normalDatas[Random.Range(0, normalDatas.Count)];

            await SpawnMapFromDataAsync(selectedData, gridPos, isStation ? 1 : 0, isStation ? "StationMap" : "NormalMap", cancellationToken);

            await UniTask.Yield(cancellationToken);
        }

        Physics.SyncTransforms();
        RefreshAllTileOccupancies();

        OnMapGenerated?.Invoke(_mapTypeData);

        Debug.Log("[MapManager] 초기 맵 생성 완료. 스테이션 배치 완료.확장 대기 중");
        return true;
    }

    public async UniTask<bool> ExpandMapAsync(int targetSize, CancellationToken cancellationToken = default)
    {
        if (targetSize != 5 && targetSize != 7)
        {
            Debug.LogError("[MapManager] 확장 가능한 맵 크기는 5x5 또는 7x7입니다.");
            return false;
        }

        if (_isExpanding)
        {
            Debug.LogWarning("[MapManager] 이미 맵을 확장 중입니다.");
            return false;
        }

        int targetRadius = (targetSize - 1) / 2;

        if (targetRadius <= _currentRadius)
        {
            Debug.Log($"[MapManager] 이미 {targetSize}x{targetSize} 이상으로 생성되어 있습니다.");
            return false;
        }

        if (GameManager.Data == null)
        {
            Debug.LogError("[MapManager] DataManager 인스턴스가 존재하지 않습니다.");
            return false;
        }

        _isExpanding = true;

        try
        {
            IReadOnlyList<MapData> allMapDatas = GameManager.Data.GetAllData<MapData>();

            List<MapData> stationDatas = new();
            List<MapData> normalDatas = new();

            foreach (MapData data in allMapDatas)
            {
                if (data == null)
                {
                    continue;
                }

                if (data.Type == MapTypeConst.Station)
                {
                    stationDatas.Add(data);
                }
                else if (data.Type == MapTypeConst.Normal)
                {
                    normalDatas.Add(data);
                }
            }

            if (stationDatas.Count == 0 || normalDatas.Count == 0)
            {
                Debug.LogError("[MapManager] 확장에 필요한 Station 또는 Normal MapData가 없습니다.");
                return false;
            }

            for (int radius = _currentRadius + 1; radius <= targetRadius; radius++)
            {
                int currentSize = radius * 2 + 1;

                int requiredStationCount = (currentSize - 1) * 2;

                int currentStationCount = CountStationMaps();
                int stationsToAdd = Mathf.Max(0, requiredStationCount - currentStationCount);

                List<Vector3Int> newPositions = new();

                foreach (Vector3Int gridPos in GetRingPositions(radius))
                {
                    if (!_spawnedMaps.ContainsKey(gridPos))
                    {
                        newPositions.Add(gridPos);
                    }
                }

                if (stationsToAdd > newPositions.Count)
                {
                    Debug.LogError($"[MapManager] 스테이션을 배치할 공간이 부족합니다. 필요: {stationsToAdd}, 남은 칸: {newPositions.Count}");
                    return false;
                }

                int remainingTiles = newPositions.Count;

                foreach (Vector3Int gridPos in newPositions)
                {
                    bool isStation = stationsToAdd > 0 && (stationsToAdd >= remainingTiles || Random.value < (float)stationsToAdd / remainingTiles);

                    if (isStation)
                    {
                        stationsToAdd--;
                    }

                    remainingTiles--;

                    MapData selectedData = isStation ? stationDatas[Random.Range(0, stationDatas.Count)] : normalDatas[Random.Range(0, normalDatas.Count)];

                    await SpawnMapFromDataAsync(selectedData, gridPos, isStation ? 1 : 0, isStation ? "StationMap" : "NormalMap", cancellationToken);

                    await UniTask.Yield(cancellationToken);
                }

                _currentRadius = radius;
            }

            Physics.SyncTransforms();
            RefreshAllTileOccupancies();

            OnMapGenerated?.Invoke(_mapTypeData);

            Debug.Log($"[MapManager] {targetSize}x{targetSize} 확장 완료. 스테이션 총 {CountStationMaps()}개");

            return true;
        }
        finally
        {
            _isExpanding = false;
        }
    }

    public void SpawnSkirt(int mapSize)
    {
        if (_skirtMaker == null)
        {
            Debug.LogWarning("[MapManager] MapSkirtMaker가 연결되어 있지 않습니다.");
            return;
        }

        _skirtMaker.GenerateSkirt(mapSize, _mapSpacing, _mapRoot);
    }

    private IEnumerable<Vector3Int> GetRingPositions(int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                bool isOuterEdge = Mathf.Abs(x) == radius || Mathf.Abs(z) == radius;

                if (isOuterEdge)
                {
                    yield return new Vector3Int(x, 0, z);
                }
            }
        }
    }

    private int CountStationMaps()
    {
        int count = 0;

        foreach (int mapType in _mapTypeData.Values)
        {
            if (mapType == 1)
            {
                count++;
            }
        }

        return count;
    }

    private async UniTask SpawnMapFromDataAsync(MapData mapData, Vector3Int gridPos, int typeId, string mapNameTag,CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mapData.AddressablePath))
        {
            Debug.LogError($"[MapManager] 맵 데이터(ID: {mapData.Id})의 AddressablePath가 비어있습니다.");
            return;
        }

        GameObject prefab = await GameManager.Resource.LoadAsset<GameObject>(mapData.AddressablePath);

        if (prefab == null)
        {
            Debug.LogError($"[MapManager] 어드레서블 로드 실패: '{mapData.AddressablePath}'");
            return;
        }

        Vector3 worldPosition = new(gridPos.x * _mapSpacing, 0, gridPos.z * _mapSpacing);

        GameObject mapObject = Instantiate(prefab, worldPosition, Quaternion.identity, _mapRoot);

        mapObject.name = $"{mapNameTag} ({gridPos.x}, {gridPos.z})";

        _spawnedMaps[gridPos] = mapObject;
        _mapTypeData[gridPos] = typeId;

        RegisterMapTiles(mapObject, gridPos, typeId == 2);
        Transform railParent = mapObject.transform.Find("offsetRailRoot");

        if (railParent == null)
        {
            GameObject railFolder = new("offsetRailRoot");
            railFolder.transform.SetParent(mapObject.transform);
            railFolder.transform.localPosition = Vector3.zero;
            railParent = railFolder.transform;
        }

        if (typeId == 2)
        {
            CentralTerminal terminal = mapObject.GetComponentInChildren<CentralTerminal>();

            Vector3 centerPosition = terminal != null? terminal.transform.position : worldPosition;

            await SpawnRailsAroundAsync(centerPosition, true, railParent, terminal, null);
        }
        else if (typeId == 1)
        {
            StationObject stationObj = mapObject.GetComponentInChildren<StationObject>();

            if (stationObj != null)
            {
                stationObj.Initialize(mapData.Id);
            }

            Vector3 centerPosition = stationObj != null? stationObj.transform.position : worldPosition;

            await SpawnRailsAroundAsync(centerPosition, false, railParent, null, stationObj);
        }
    }

    private async UniTask SpawnRailsAroundAsync(Vector3 centerPos, bool isTerminal, Transform railParent, CentralTerminal terminal = null, StationObject station = null)
    {
        int railCount = 3;
        float railLength = 2f;
        float railHeight = 1f;

        if (isTerminal)
        {
            var paths = new (Vector3 dir, Quaternion trainRot, Quaternion railRot)[]
            {
                (new Vector3(0, 0, 1),  Quaternion.identity,         Quaternion.Euler(0, 90, 0)),
                (new Vector3(0, 0, -1), Quaternion.Euler(0, 180, 0), Quaternion.Euler(0, 90, 0)),
                (new Vector3(-1, 0, 0), Quaternion.Euler(0, 270, 0), Quaternion.identity),
                (new Vector3(1, 0, 0),  Quaternion.Euler(0, 90, 0),  Quaternion.identity)
            };

            for (int d = 0; d < paths.Length; d++)
            {
                GameObject dirRoot = new(d.ToString());
                dirRoot.transform.SetParent(railParent);
                dirRoot.transform.localPosition = Vector3.zero;

                if (terminal != null)
                {
                    terminal.RegisterExitDirRoot(d, dirRoot.transform);
                }

                for (int i = 0; i < 2 * railCount; i++)
                {
                    Vector3 offset = paths[d].dir * (_railSpawnOffset + i * railLength);

                    GameObject railObj = await PlaceSingleRailAsync(centerPos + offset, paths[d].railRot, railHeight, dirRoot.transform);

                    if (i == 0 && railObj != null && terminal != null)
                    {
                        terminal.RegisterStartPoint(d, railObj.transform.position, paths[d].trainRot);
                    }
                }
            }
        }
        else
        {
            var paths = new (Vector3 dir, Quaternion trainRot, Quaternion railRot)[]
            {
                (new Vector3(-1, 0, 0), Quaternion.Euler(0, 270, 0), Quaternion.identity),
                (new Vector3(1, 0, 0), Quaternion.Euler(0, 90, 0), Quaternion.identity)
            };

            for (int d = 0; d < paths.Length; d++)
            {
                GameObject dirRoot = new(d.ToString());
                dirRoot.transform.SetParent(railParent);
                dirRoot.transform.localPosition = Vector3.zero;

                if (station != null)
                {
                    station.RegisterExitDirRoot(d, dirRoot.transform);
                }

                for (int i = 0; i < railCount; i++)
                {
                    Vector3 offset = paths[d].dir * (_railSpawnOffset + i * railLength);

                    GameObject railObj = await PlaceSingleRailAsync(centerPos + offset, paths[d].railRot, railHeight, dirRoot.transform);

                    if (i == 0 && railObj != null && station != null)
                    {
                        station.RegisterStartPoint(d, railObj.transform.position, paths[d].trainRot);
                    }
                }
            }
        }
    }

    private async UniTask<GameObject> PlaceSingleRailAsync(Vector3 targetPos, Quaternion rotation, float height,Transform parent)
    {
        if (string.IsNullOrEmpty(_straightRailAddress))
        {
            return null;
        }

        GameObject railPrefab = await GameManager.Resource.LoadAsset<GameObject>(_straightRailAddress);

        if (railPrefab == null)
        {
            Debug.LogWarning(
                $"[MapManager] 레일 프리팹('{_straightRailAddress}') 로드 실패.");
            return null;
        }

        Vector3 finalPos = new(targetPos.x, height, targetPos.z);

        GameObject railObject = Instantiate(railPrefab, finalPos, rotation, parent);

        railObject.name = "AutoSpawned_StraightRail";
        return railObject;
    }

    private List<bool> CreateInitialStationLayout()
    {
        List<bool> layout = new()
        {
            true, true, true, true, false, false, false, false
        };

        const int maxIterations = 100;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            for (int i = layout.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);

                (layout[i], layout[randomIndex]) = (layout[randomIndex], layout[i]);
            }

            if (IsValidLayout(layout))
            {
                return layout;
            }
        }

        return new List<bool>
        {
            true, true, false, true, true, false, false, false
        };
    }

    private bool IsValidLayout(List<bool> layout)
    {
        int currentStreak = 0;
        int maxStreak = 0;

        for (int i = 0; i < layout.Count * 2; i++)
        {
            if (layout[i % layout.Count])
            {
                currentStreak++;
                maxStreak = Mathf.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak <= 2;
    }

    public void ClearMap()
    {
        foreach (KeyValuePair<Vector3Int, GameObject> kvp in _spawnedMaps)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }

        _spawnedMaps.Clear();
        _mapTypeData.Clear();
        _spawnedTiles.Clear();

        _skirtMaker?.RemoveSkirt();

        _currentRadius = 1;
        _isExpanding = false;
    }

    private void RegisterMapTiles(GameObject mapObject, Vector3Int mapGridPos, bool isCentralTerminal)
    {
        MapTileInfo[] tiles = mapObject.GetComponentsInChildren<MapTileInfo>(true);

        foreach (MapTileInfo tile in tiles)
        {
            tile.SetParentMapGridPosition(mapGridPos);
            tile.SetTerminalReservedAreaEnabled(isCentralTerminal);

            _spawnedTiles.Add(tile);
        }
    }

    public void RefreshAllTileOccupancies()
    {
        foreach (MapTileInfo tile in _spawnedTiles)
        {
            if (tile != null)
            {
                tile.RefreshOccupancy();
            }
        }
    }

    public void RefreshTileAtWorldPosition(Vector3 worldPosition)
    {
        MapTileInfo closestTile = null;
        float closestSqrDistance = float.MaxValue;

        foreach (MapTileInfo tile in _spawnedTiles)
        {
            if (tile == null)
            {
                continue;
            }

            Vector3 offset = tile.transform.position - worldPosition;
            float sqrDistance = offset.x * offset.x + offset.z * offset.z;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestTile = tile;
            }
        }

        if (closestTile != null)
        {
            closestTile.RefreshOccupancy();
        }
    }

    public Dictionary<Vector3Int, int> GetMapTypeData()
    {
        return _mapTypeData;
    }
}