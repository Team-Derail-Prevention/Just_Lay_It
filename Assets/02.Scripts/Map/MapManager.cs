using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapManager : SingletonBase<MapManager>
{
    [Header("Map Settings")]
    [SerializeField] private Transform _mapRoot;
    [SerializeField] private float _mapSpacing = 30f;

    [Header("Auto Spawn Rail Settings")]
    [SerializeField] private string _straightRailAddress = "Prefab/Rail_Straight";
    [SerializeField] private float _railSpawnOffset = 2f;

    private readonly Vector3Int[] _mapOffsets = new Vector3Int[]
       {
        new Vector3Int(-1, 0, 1),  new Vector3Int(0, 0, 1),  new Vector3Int(1, 0, 1),
        new Vector3Int(1, 0, 0),   new Vector3Int(1, 0, -1), new Vector3Int(0, 0, -1),
        new Vector3Int(-1, 0, -1), new Vector3Int(-1, 0, 0)
       };

    private Dictionary<Vector3Int, GameObject> _spawnedMaps = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, int> _mapTypeData = new Dictionary<Vector3Int, int>();
    private readonly List<MapTileInfo> _spawnedTiles = new List<MapTileInfo>();

    public event Action<Dictionary<Vector3Int, int>> OnMapGenerated;
    public Transform MapRoot { get { return _mapRoot; } }

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            Debug.Log("[MapManager] 맵 리셋 및 재생성 테스트 시작");
            ClearMap();
            _ = GenerateMapAsync();
        }
    }

    private void InitMapRoot()
    {
        GameObject existingRoot = GameObject.Find("@MapRoot");
        if (existingRoot != null)
        {
            DestroyImmediate(existingRoot);
        }

        GameObject rootObj = new GameObject("@MapRoot");
        _mapRoot = rootObj.transform;
        _mapRoot.position = Vector3.zero;
        _mapRoot.rotation = Quaternion.identity;
    }

    private async UniTask NeedDataLoadAsync()
    {
        if (GameManager.Data != null && GameManager.Data.IsLoaded)
            return;

        UniTaskCompletionSource tcs = new UniTaskCompletionSource();

        if (GameManager.Data != null)
        {
            GameManager.Data.OnDataLoadCompleted += () => tcs.TrySetResult();
        }
        else
        {
            Debug.LogError("[MapManager] DataManager 인스턴스를 찾을 수 없습니다! 씬에 DataManager가 존재하는지 확인하세요.");
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
            Debug.LogError("[MapManager] DataManager에서 MapData를 가져오지 못했습니다! DataManager에서 'MapData' 로드가 정상적으로 호출되었는지, JSON 파일 내 items 구조가 올바른지 확인해주세요.");
            return false;
        }

        MapData centralData = null;
        List<MapData> stationDatas = new List<MapData>();
        List<MapData> normalDatas = new List<MapData>();

        foreach (var data in allMapDatas)
        {
            if (data == null) continue;

            if (data.Type == MapTypeConst.CentralTerminal) centralData = data;
            else if (data.Type == MapTypeConst.Station) stationDatas.Add(data);
            else if (data.Type == MapTypeConst.Normal) normalDatas.Add(data);
        }

        if (centralData != null)
        {
            await SpawnMapFromDataAsync(centralData, Vector3Int.zero, 2, "CentralTerminal", cancellationToken);
        }
        else
        {
            Debug.LogError("[MapManager] CentralTerminal 타입의 MapData를 찾을 수 없습니다! JSON 데이터에 해당 타입이 있는지 확인해주세요. 맵 생성을 중단합니다.");
            return false;
        }

        List<bool> assignedTypes = RandomStationLayout();

        for (int i = 0; i < _mapOffsets.Length; i++)
        {
            Vector3Int gridPos = _mapOffsets[i];
            bool isStation = assignedTypes[i];
            int typeId = isStation ? 1 : 0;

            MapData selectedData = null;

            if (isStation)
            {
                if (stationDatas.Count == 0)
                {
                    Debug.LogError($"[MapManager] Station 타입 MapData가 없습니다! (Grid: {gridPos})");
                    continue;
                }
                selectedData = stationDatas[Random.Range(0, stationDatas.Count)];
            }
            else
            {
                if (normalDatas.Count == 0)
                {
                    Debug.LogError($"[MapManager] Normal 타입 MapData가 없습니다! (Grid: {gridPos})");
                    continue;
                }
                selectedData = normalDatas[Random.Range(0, normalDatas.Count)];
            }

            if (selectedData == null)
            {
                Debug.LogError($"[MapManager] 선택된 맵 데이터가 null입니다. (Grid: {gridPos})");
                continue;
            }

            string tag = isStation ? "StationMap" : "NormalMap";
            await SpawnMapFromDataAsync(selectedData, gridPos, typeId, tag, cancellationToken);

            await UniTask.Yield(CancellationToken.None);
        }

        Physics.SyncTransforms();
        RefreshAllTileOccupancies();

        Debug.Log("[MapManager] 데이터 기반 3x3 맵 생성 및 자동 레일 설치 완료!");
        OnMapGenerated?.Invoke(_mapTypeData);

        return true;
    }

    private async UniTask SpawnMapFromDataAsync(MapData mapData, Vector3Int gridPos, int typeId, string mapNameTag, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mapData.AddressablePath))
        {
            Debug.LogError($"[MapManager] 맵 데이터(ID: {mapData.Id})의 AddressablePath가 비어있습니다.");
            return;
        }

        GameObject prefab = await GameManager.Resource.LoadAsset<GameObject>(mapData.AddressablePath);
        if (prefab == null)
        {
            Debug.LogError($"[MapManager] 어드레서블 로드 실패: '{mapData.AddressablePath}' (ID: {mapData.Id})");
            return;
        }

        Vector3 worldPosition = new Vector3(gridPos.x * _mapSpacing, 0, gridPos.z * _mapSpacing);
        GameObject mapObject = Instantiate(prefab, worldPosition, Quaternion.identity, _mapRoot);

        mapObject.name = $"{mapNameTag} ({gridPos.x}, {gridPos.y})";

        _spawnedMaps[gridPos] = mapObject;
        _mapTypeData[gridPos] = typeId;
        RegisterMapTiles(mapObject, gridPos);

        Transform railParent = mapObject.transform.Find("offsetRailRoot");
        if (railParent == null)
        {
            GameObject railFolder = new GameObject("offsetRailRoot");
            railFolder.transform.SetParent(mapObject.transform);
            railFolder.transform.localPosition = Vector3.zero;
            railParent = railFolder.transform;
        }

        if (typeId == 2)
        {
            CentralTerminal terminal = mapObject.GetComponentInChildren<CentralTerminal>();
            Vector3 centerPosition = terminal != null ? terminal.transform.position : worldPosition;

            await SpawnRailsAroundAsync(centerPosition, true, railParent, terminal, null);
        }
        else if (typeId == 1)
        {
            StationObject stationObj = mapObject.GetComponentInChildren<StationObject>();
            if (stationObj != null)
            {
                stationObj.Initialize(mapData.Id);
            }
            Vector3 centerPosition = stationObj != null ? stationObj.transform.position : worldPosition;

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
            (Vector3 dir, Quaternion rot)[] paths = new (Vector3, Quaternion)[]
            {
                (new Vector3(0, 0, 1), Quaternion.Euler(0, 90, 0)),
                (new Vector3(0, 0, -1), Quaternion.Euler(0, 90, 0)),
                (new Vector3(-1, 0, 0), Quaternion.identity),
                (new Vector3(1, 0, 0), Quaternion.identity)
            };

            for (int d = 0; d < paths.Length; d++)
            {
                GameObject dirRoot = new GameObject(d.ToString());
                dirRoot.transform.SetParent(railParent);
                dirRoot.transform.localPosition = Vector3.zero;

                for (int i = 0; i < (2 * railCount); i++)
                {
                    Vector3 offset = paths[d].dir * (_railSpawnOffset + (i * railLength));
                    Vector3 targetPos = centerPos + offset;
                    Quaternion rot = paths[d].rot;

                    GameObject railObj = await PlaceSingleRailAsync(targetPos, rot, railHeight, dirRoot.transform);

                    if (i == 0 && railObj != null && terminal != null)
                    {
                        terminal.RegisterStartPoint(d, railObj.transform.position, railObj.transform.rotation);
                    }
                }
            }
        }
        else
        {
            (Vector3 dir, Quaternion rot)[] paths = new (Vector3, Quaternion)[]
            {
                (new Vector3(-1, 0, 0), Quaternion.identity),
                (new Vector3(1, 0, 0), Quaternion.identity)
            };

            for (int d = 0; d < paths.Length; d++)
            {
                GameObject dirRoot = new GameObject(d.ToString());
                dirRoot.transform.SetParent(railParent);
                dirRoot.transform.localPosition = Vector3.zero;

                for (int i = 0; i < railCount; i++)
                {
                    Vector3 offset = paths[d].dir * (_railSpawnOffset + (i * railLength));
                    Vector3 targetPos = centerPos + offset;
                    Quaternion rot = paths[d].rot;

                    GameObject railObj = await PlaceSingleRailAsync(targetPos, rot, railHeight, dirRoot.transform);

                    if (i == 0 && railObj != null && station != null)
                    {
                        station.RegisterStartPoint(d, railObj.transform.position, railObj.transform.rotation);
                    }
                }
            }
        }
    }

    private async UniTask<GameObject> PlaceSingleRailAsync(Vector3 targetPos, Quaternion rotation, float height, Transform parent)
    {
        if (string.IsNullOrEmpty(_straightRailAddress)) return null;

        GameObject railPrefab = await GameManager.Resource.LoadAsset<GameObject>(_straightRailAddress);

        if (railPrefab != null)
        {
            Vector3 finalPos = new Vector3(targetPos.x, height, targetPos.z);

            GameObject railObj = Instantiate(railPrefab, finalPos, rotation, parent);
            railObj.name = "AutoSpawned_StraightRail";
            return railObj;
        }
        else
        {
            Debug.LogWarning($"[MapManager] 레일 프리팹('{_straightRailAddress}') 로드 실패.");
            return null;
        }
    }

    private List<bool> RandomStationLayout()
    {
        List<bool> layout = new List<bool> { true, true, true, true, false, false, false, false };
        int maxIterations = 100;
        int currentIteration = 0;

        while (currentIteration < maxIterations)
        {
            for (int i = layout.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                bool temp = layout[i];
                layout[i] = layout[randomIndex];
                layout[randomIndex] = temp;
            }

            if (IsValidLayout(layout))
            {
                return layout;
            }
            currentIteration++;
        }

        return new List<bool> { true, true, false, true, true, false, false, false };
    }

    private bool IsValidLayout(List<bool> layout)
    {
        int n = layout.Count;
        int currentStreak = 0;
        int maxStreak = 0;

        for (int i = 0; i < n * 2; i++)
        {
            int index = i % n;
            if (layout[index])
            {
                currentStreak++;
                if (currentStreak > maxStreak) maxStreak = currentStreak;
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
        foreach (var kvp in _spawnedMaps)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        _spawnedMaps.Clear();
        _mapTypeData.Clear();
        _spawnedTiles.Clear();
    }

    private void RegisterMapTiles(GameObject mapObject, Vector3Int mapGridPos)
    {
        MapTileInfo[] tiles = mapObject.GetComponentsInChildren<MapTileInfo>(true);
        foreach (MapTileInfo tile in tiles)
        {
            tile.SetParentMapGridPosition(mapGridPos);
            _spawnedTiles.Add(tile);
        }
    }

    private void RefreshAllTileOccupancies()
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
            if (tile == null) continue;

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