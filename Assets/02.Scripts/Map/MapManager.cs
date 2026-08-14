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
    [SerializeField] private float _mapSpacing = 20f;

    private readonly Vector3Int[] _mapOffsets = new Vector3Int[]
    {
        new Vector3Int(-1, 1, 0),  new Vector3Int(0, 1, 0),  new Vector3Int(1, 1, 0),
        new Vector3Int(1, 0, 0),   new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(-1, -1, 0), new Vector3Int(-1, 0, 0)
    };

    private Dictionary<Vector3Int, GameObject> _spawnedMaps = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, int> _mapTypeData = new Dictionary<Vector3Int, int>();

    public event Action<Dictionary<Vector3Int, int>> OnMapGenerated;
    public Transform MapRoot { get { return _mapRoot; } }

    private async void Start()
    {
        InitMapRoot();

        await EnsureDataLoadedAsync();

        await GenerateMapAsync();
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
        if (_mapRoot == null)
        {
            GameObject rootObj = new GameObject("@MapRoot");
            _mapRoot = rootObj.transform;
            _mapRoot.SetParent(transform);
            _mapRoot.localPosition = Vector3.zero;
        }
    }

    private async UniTask EnsureDataLoadedAsync()
    {
        if (DataManager.Instance != null && DataManager.Instance.IsLoaded)
            return;

        UniTaskCompletionSource tcs = new UniTaskCompletionSource();

        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnDataLoadCompleted += () => tcs.TrySetResult();
        }
        else
        {
            Debug.LogError("[MapManager] DataManager 인스턴스를 찾을 수 없습니다! 씬에 DataManager가 존재하는지 확인하세요.");
            return;
        }

        await tcs.Task;
    }

    public async UniTask GenerateMapAsync(CancellationToken cancellationToken = default)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[MapManager] DataManager 인스턴스가 존재하지 않습니다.");
            return;
        }

        IReadOnlyList<MapData> allMapDatas = DataManager.Instance.GetAllData<MapData>();
        if (allMapDatas == null || allMapDatas.Count == 0)
        {
            Debug.LogError("[MapManager] DataManager에서 MapData를 가져오지 못했습니다! DataManager에서 'MapData' 로드가 정상적으로 호출되었는지, JSON 파일 내 items 구조가 올바른지 확인해주세요.");
            return;
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
            Debug.LogError("[MapManager] CentralTerminal 타입의 MapData를 찾을 수 없습니다!");
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
        }

        Debug.Log("[MapManager] 데이터 기반 3x3 맵 생성 완료!");
        OnMapGenerated?.Invoke(_mapTypeData);
    }

    private async UniTask SpawnMapFromDataAsync(MapData mapData, Vector3Int gridPos, int typeId, string mapNameTag, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mapData.AddressablePath))
        {
            Debug.LogError($"[MapManager] 맵 데이터(ID: {mapData.Id})의 AddressablePath가 비어있습니다.");
            return;
        }

        GameObject prefab = await ResourceManager.Instance.LoadAsset<GameObject>(mapData.AddressablePath);
        if (prefab == null)
        {
            Debug.LogError($"[MapManager] 어드레서블 로드 실패: '{mapData.AddressablePath}' (ID: {mapData.Id})");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * _mapSpacing, 0, gridPos.y * _mapSpacing);
        GameObject mapObj = Instantiate(prefab, worldPos, Quaternion.identity, _mapRoot);

        _spawnedMaps[gridPos] = mapObj;
        _mapTypeData[gridPos] = typeId;
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

    private void ClearMap()
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
    }

    public Dictionary<Vector3Int, int> GetMapTypeData()
    {
        return _mapTypeData;
    }
}