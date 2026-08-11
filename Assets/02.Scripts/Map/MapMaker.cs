using System.Collections.Generic;
using UnityEngine;

public class MapMaker : MonoBehaviour
{
    [System.Serializable]
    public struct MaterialSpawnData
    {
        [Tooltip("배치할 머테리얼 프리팹")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("해당 머테리얼의 최소 생성 보장 개수")]
        [SerializeField] private int _minCount;

        public GameObject Prefab => _prefab;
        public int MinCount => _minCount;
    }

    [Header("바닥 맵 생성 설정")]
    [SerializeField] private List<GameObject> _cubePrefabs = new List<GameObject>();
    [SerializeField] private int _gridSizeX = 14;
    [SerializeField] private int _gridSizeZ = 14;
    [SerializeField] private float _spacing = 2f;

    [Header("지상 오브젝트 배치 설정 (땅바닥 위)")]
    [SerializeField] private GameObject _stationPrefab;
    [SerializeField] private List<GameObject> _obstaclePrefabs;
    [SerializeField] private List<MaterialSpawnData> _materialSpawnDatas = new List<MaterialSpawnData>();
    [SerializeField] private int _targetObstacleCount = 5;
    [SerializeField] private int _targetMaterialCount = 8;

    [Header("지상 오브젝트 높이(Y축) 설정 (디폴트: 2)")]
    [SerializeField] private float _stationHeight = 2f;
    [SerializeField] private float _obstacleHeight = 2f;
    [SerializeField] private float _materialHeight = 2f;

    private Transform _mapRoot;

    [ContextMenu("Generate 14x14 Grid Map With Objects")]
    public void GenerateMap()
    {
        if (_cubePrefabs == null || _cubePrefabs.Count == 0)
        {
            Debug.LogError("[MapMaker] 배치할 큐브 프리팹 리스트가 비어 있습니다.");
            return;
        }

        GameObject existingRoot = GameObject.Find("@MapRoot_14x14");
        if (existingRoot != null)
        {
            DestroyImmediate(existingRoot);
        }

        GameObject rootObj = new GameObject("@MapRoot_14x14");
        _mapRoot = rootObj.transform;

        GameObject groundFolder = new GameObject("Ground");
        groundFolder.transform.SetParent(_mapRoot);
        Transform groundRoot = groundFolder.transform;

        GameObject surfaceFolder = new GameObject("SurfaceObjects");
        surfaceFolder.transform.SetParent(_mapRoot);
        Transform surfaceRoot = surfaceFolder.transform;

        GameObject stationRootFolder = new GameObject("StationRoot");
        stationRootFolder.transform.SetParent(surfaceRoot);
        Transform stationRoot = stationRootFolder.transform;

        GameObject obstaclesFolder = new GameObject("Obstacles");
        obstaclesFolder.transform.SetParent(surfaceRoot);
        Transform obstaclesRoot = obstaclesFolder.transform;

        GameObject materialsFolder = new GameObject("Materials");
        materialsFolder.transform.SetParent(surfaceRoot);
        Transform materialsRoot = materialsFolder.transform;

        System.Random rand = new System.Random();
        List<Vector3> availableSpawnPositions = new List<Vector3>();

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int z = 0; z < _gridSizeZ; z++)
            {
                int randomIndex = rand.Next(0, _cubePrefabs.Count);
                GameObject selectedPrefab = _cubePrefabs[randomIndex];

                if (selectedPrefab == null)
                {
                    continue;
                }

                float posX = (x - _gridSizeX / 2f) * _spacing;
                float posZ = (z - _gridSizeZ / 2f) * _spacing;
                Vector3 spawnPos = new Vector3(posX, 0f, posZ);

                Quaternion prefabRotation = selectedPrefab.transform.rotation;

                GameObject cubeObj = Instantiate(selectedPrefab, spawnPos, prefabRotation, groundRoot);
                cubeObj.name = $"Cube_{x}_{z}_{selectedPrefab.name}";

                Vector3 surfacePos = new Vector3(posX, 0.5f, posZ);
                availableSpawnPositions.Add(surfacePos);
            }
        }

        SpawnInternalObjects(availableSpawnPositions, stationRoot, obstaclesRoot, materialsRoot, rand);

        Debug.Log($"[MapMaker] 14x14 맵 및 지상 기물 배치 완료!");
    }

    private void SpawnInternalObjects(List<Vector3> availablePositions, Transform stationRoot, Transform obstaclesRoot, Transform materialsRoot, System.Random rand)
    {
        if (availablePositions.Count == 0) return;

        SpawnStation(ref availablePositions, stationRoot, rand);

        SpawnObstacles(ref availablePositions, obstaclesRoot, rand);

        SpawnResources(ref availablePositions, materialsRoot, rand);
    }

    private void SpawnStation(ref List<Vector3> availablePositions, Transform stationRoot, System.Random rand)
    {
        if (_stationPrefab == null || availablePositions.Count == 0) return;

        List<Vector3> centerCandidates = new List<Vector3>();
        float mapWidth = _gridSizeX * _spacing;
        float centerThreshold = mapWidth * 0.25f;

        foreach (var pos in availablePositions)
        {
            if (Vector3.Distance(Vector3.zero, new Vector3(pos.x, 0f, pos.z)) <= centerThreshold)
            {
                centerCandidates.Add(pos);
            }
        }

        if (centerCandidates.Count == 0)
        {
            centerCandidates = availablePositions;
        }

        int randomIndex = rand.Next(0, centerCandidates.Count);
        Vector3 selectedBasePos = centerCandidates[randomIndex];

        Vector3 stationPos = new Vector3(selectedBasePos.x, _stationHeight, selectedBasePos.z);

        string groupName = _stationPrefab.name;
        Transform subRoot = stationRoot.Find(groupName);
        if (subRoot == null)
        {
            GameObject newSubRoot = new GameObject(groupName);
            newSubRoot.transform.SetParent(stationRoot);
            subRoot = newSubRoot.transform;
        }

        GameObject stationObj = Instantiate(_stationPrefab, stationPos, Quaternion.identity, subRoot);
        stationObj.name = "Station_Object";

        float safetyRadius = _spacing * 1.5f;
        List<Vector3> positionsToRemove = new List<Vector3>();

        foreach (var pos in availablePositions)
        {
            if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(selectedBasePos.x, 0f, selectedBasePos.z)) <= safetyRadius)
            {
                positionsToRemove.Add(pos);
            }
        }

        foreach (var pos in positionsToRemove)
        {
            availablePositions.Remove(pos);
        }
    }

    private void SpawnObstacles(ref List<Vector3> availablePositions, Transform obstaclesRoot, System.Random rand)
    {
        if (_obstaclePrefabs == null || _obstaclePrefabs.Count == 0) return;

        int countToSpawn = Mathf.Min(_targetObstacleCount, availablePositions.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            int index = rand.Next(0, availablePositions.Count);
            Vector3 basePos = availablePositions[index];
            availablePositions.RemoveAt(index);

            int prefabIndex = rand.Next(0, _obstaclePrefabs.Count);
            GameObject selectedPrefab = _obstaclePrefabs[prefabIndex];
            if (selectedPrefab == null) continue;

            Vector3 targetPos = new Vector3(basePos.x, _obstacleHeight, basePos.z);

            string groupName = selectedPrefab.name;
            Transform subRoot = obstaclesRoot.Find(groupName);
            if (subRoot == null)
            {
                GameObject newSubRoot = new GameObject(groupName);
                newSubRoot.transform.SetParent(obstaclesRoot);
                subRoot = newSubRoot.transform;
            }

            GameObject obstacleObj = Instantiate(selectedPrefab, targetPos, Quaternion.identity, subRoot);
            obstacleObj.name = $"Obstacle_{i}";
        }
    }

    private void SpawnResources(ref List<Vector3> availablePositions, Transform materialsRoot, System.Random rand)
    {
        if (_materialSpawnDatas == null || _materialSpawnDatas.Count == 0) return;

        int spawnedTotalCount = 0;
        Dictionary<GameObject, int> spawnedCounts = new Dictionary<GameObject, int>();

        foreach (var data in _materialSpawnDatas)
        {
            if (data.Prefab == null || data.MinCount <= 0) continue;

            int countToSpawnMin = Mathf.Min(data.MinCount, availablePositions.Count);
            spawnedCounts[data.Prefab] = 0;

            for (int i = 0; i < countToSpawnMin; i++)
            {
                int index = rand.Next(0, availablePositions.Count);
                Vector3 basePos = availablePositions[index];
                availablePositions.RemoveAt(index);

                InstantiateMaterial(data.Prefab, basePos, materialsRoot);
                spawnedCounts[data.Prefab]++;
                spawnedTotalCount++;

                if (availablePositions.Count == 0) break;
            }
        }

        int remainingTargetCount = Mathf.Max(0, _targetMaterialCount - spawnedTotalCount);
        int actualSpawnCount = Mathf.Min(remainingTargetCount, availablePositions.Count);

        for (int i = 0; i < actualSpawnCount; i++)
        {
            if (availablePositions.Count == 0) break;

            int index = rand.Next(0, availablePositions.Count);
            Vector3 basePos = availablePositions[index];
            availablePositions.RemoveAt(index);

            List<GameObject> validPrefabs = new List<GameObject>();
            foreach (var data in _materialSpawnDatas)
            {
                if (data.Prefab != null) validPrefabs.Add(data.Prefab);
            }

            if (validPrefabs.Count == 0) break;

            int prefabIndex = rand.Next(0, validPrefabs.Count);
            GameObject selectedPrefab = validPrefabs[prefabIndex];

            InstantiateMaterial(selectedPrefab, basePos, materialsRoot);
        }
    }

    private void InstantiateMaterial(GameObject prefab, Vector3 basePos, Transform materialsRoot)
    {
        Vector3 targetPos = new Vector3(basePos.x, _materialHeight, basePos.z);

        string groupName = prefab.name;
        Transform subRoot = materialsRoot.Find(groupName);
        if (subRoot == null)
        {
            GameObject newSubRoot = new GameObject(groupName);
            newSubRoot.transform.SetParent(materialsRoot);
            subRoot = newSubRoot.transform;
        }

        GameObject resourceObj = Instantiate(prefab, targetPos, Quaternion.identity, subRoot);
        resourceObj.name = $"Resource_{subRoot.childCount - 1}";
    }
}