using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class MapManager : SingletonBase<MapManager>
{
    [Header("Map Prefab Settings")]
    [SerializeField] private GameObject _centralTerminalPrefab;
    [SerializeField] private List<GameObject> _stationMapPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> _normalMapPrefabs = new List<GameObject>();

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

    private void Awake()
    {
        InitMapRoot();
    }

    private void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[MapManager] 맵 리셋 및 재생성 테스트 시작");
            GenerateMap();
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


    public void GenerateMap()
    {
        ClearMap();

        Assert.IsNotNull(_centralTerminalPrefab, "[MapManager] Central Terminal Prefab이 할당되지 않았습니다!");
        SpawnMapObject(_centralTerminalPrefab, Vector3Int.zero, "CentralTerminal");
        _mapTypeData[Vector3Int.zero] = 2;

        List<bool> assignedTypes = RandomStationLayout();

        for (int i = 0; i < _mapOffsets.Length; i++)
        {
            Vector3Int gridPos = _mapOffsets[i];
            bool isStation = assignedTypes[i];
            GameObject selectedPrefab = null;

            int typeId = isStation ? 1 : 0;

            if (isStation)
            {
                if (_stationMapPrefabs == null || _stationMapPrefabs.Count == 0)
                {
                    Debug.LogError($"[MapManager] Station 맵 프리팹 리스트가 비어있습니다! (Grid: {gridPos})");
                    continue;
                }
                selectedPrefab = _stationMapPrefabs[Random.Range(0, _stationMapPrefabs.Count)];
            }
            else
            {
                if (_normalMapPrefabs == null || _normalMapPrefabs.Count == 0)
                {
                    Debug.LogError($"[MapManager] Normal 맵 프리팹 리스트가 비어있습니다! (Grid: {gridPos})");
                    continue;
                }
                selectedPrefab = _normalMapPrefabs[Random.Range(0, _normalMapPrefabs.Count)];
            }

            if (selectedPrefab == null)
            {
                Debug.LogError($"[MapManager] 리스트에서 선택된 맵 프리팹이 null입니다. (isStation: {isStation}, Grid: {gridPos})");
                continue;
            }

            SpawnMapObject(selectedPrefab, gridPos, isStation ? "StationMap" : "NormalMap");
        }

        Debug.Log("[MapManager] 3x3 맵 생성 및 규칙 배치 완료!");

        OnMapGenerated?.Invoke(_mapTypeData);
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

        Debug.LogWarning("[MapManager] 유효한 셔플 레이아웃을 찾지 못해 기본 레이아웃을 반환합니다.");
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
                if (currentStreak > maxStreak)
                {
                    maxStreak = currentStreak;
                }
            }
            else
            {
                currentStreak = 0;
            }
        }

        return maxStreak <= 2;
    }

    private void SpawnMapObject(GameObject prefab, Vector3Int gridPos, string mapNameTag)
    {
        Assert.IsNotNull(prefab, $"[MapManager] Spawn 실패: '{mapNameTag}' 프리팹이 null입니다. (Grid: {gridPos})");

        Vector3 worldPos = new Vector3(gridPos.x * _mapSpacing, 0, gridPos.y * _mapSpacing);
        GameObject mapObj = Instantiate(prefab, worldPos, Quaternion.identity, _mapRoot);

        //mapObj.name = $"{mapNameTag}_{gridPos.x}_{gridPos.y}";
        _spawnedMaps[gridPos] = mapObj;
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
    }

    public Dictionary<Vector3Int, int> GetMapTypeData()
    {
        return _mapTypeData;
    }
}