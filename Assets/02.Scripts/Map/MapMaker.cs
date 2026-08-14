using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] private int _gridSizeX = 15;
    [SerializeField] private int _gridSizeZ = 15;
    [SerializeField] private float _spacing = 2f;
    [Tooltip("오브젝트가 없는 빈 그라운드 타일에 부여할 유니티 레이어 이름")]
    [SerializeField] private string _groundLayerName = "Ground";

    [Header("3X3 조합을 위한 맵 위치 설정")]
    [Tooltip("3x3 전체 맵 상에서의 이 맵의 그리드 위치 (예: 중앙은 (0,0,0), 주변은 (-1,0,1) 등)")]
    [SerializeField] private Vector3Int _mapGridPos = Vector3Int.zero;

    [Header("지상 오브젝트 배치 설정 (땅바닥 위)")]
    [SerializeField] private GameObject _stationPrefab;
    [SerializeField] private List<GameObject> _obstaclePrefabs;
    [SerializeField] private List<MaterialSpawnData> _materialSpawnDatas = new List<MaterialSpawnData>();
    [SerializeField] private int _targetObstacleCount = 5;

    [Header("지상 오브젝트 높이(Y축) 설정 (디폴트: 2)")]
    [SerializeField] private float _stationHeight = 2f;
    [SerializeField] private float _obstacleHeight = 2f;
    [SerializeField] private float _materialHeight = 2f;

    private Transform _mapRoot;

    public int GridSizeX => _gridSizeX;
    public int GridSizeZ => _gridSizeZ;
    public float Spacing => _spacing;
    public Transform MapRoot => _mapRoot;

    [ContextMenu("Generate 15x15 Grid Map With Objects")]
    public void GenerateMap()
    {
        if (_cubePrefabs == null || _cubePrefabs.Count == 0)
        {
            Debug.LogError("[MapMaker] 배치할 큐브 프리팹이 등록되지 않았습니다!");
            return;
        }

        SetupMapRoot();
        System.Random rand = new System.Random();
        List<Vector3> availablePositions = new List<Vector3>();
        Dictionary<Vector3, Vector2Int> posToGridMap = new Dictionary<Vector3, Vector2Int>();
        Dictionary<Vector3, GameObject> posToTileObj = new Dictionary<Vector3, GameObject>();

        Transform groundRoot = CreateSubRoot("Ground", _mapRoot);
        Transform stationRoot = CreateSubRoot("Station", _mapRoot);
        Transform obstaclesRoot = CreateSubRoot("Obstacles", _mapRoot);
        Transform materialsRoot = CreateSubRoot("Materials", _mapRoot);

        int groundLayerIndex = LayerMask.NameToLayer(_groundLayerName);
        int defaultLayerIndex = LayerMask.NameToLayer("Default");
        if (groundLayerIndex == -1)
        {
            Debug.LogWarning($"[MapMaker] '{_groundLayerName}' 레이어가 프로젝트에 존재하지 않습니다! 기본 레이어(Default)로 유지됩니다.");
        }

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int z = 0; z < _gridSizeZ; z++)
            {
                int prefabIndex = rand.Next(0, _cubePrefabs.Count);
                GameObject selectedCubePrefab = _cubePrefabs[prefabIndex];
                if (selectedCubePrefab == null) continue;

                float posX = (x - _gridSizeX / 2f) * _spacing;
                float posZ = (z - _gridSizeZ / 2f) * _spacing;
                Vector3 spawnPos = new Vector3(posX, 0f, posZ);

                GameObject cubeObj = InstantiatePrefabSafe(selectedCubePrefab, spawnPos, Quaternion.identity, groundRoot);
                cubeObj.name = $"Tile_{x}_{z}";

                if (groundLayerIndex != -1)
                {
                    cubeObj.layer = groundLayerIndex;
                }

                MapTileInfo tileInfo = cubeObj.GetComponent<MapTileInfo>();
                if (tileInfo == null)
                {
                    tileInfo = cubeObj.AddComponent<MapTileInfo>();
                }

                tileInfo.InitTile(new Vector2Int(x, z), _mapGridPos, canInstallRail: true);

                availablePositions.Add(spawnPos);
                posToGridMap[spawnPos] = new Vector2Int(x, z);
                posToTileObj[spawnPos] = cubeObj;
            }
        }

        SetupGroundBoxCollider(groundRoot);

        int centerGridX = _gridSizeX / 2; // 7
        int centerGridZ = _gridSizeZ / 2; // 7
        bool hasStation = (_stationPrefab != null);

        if (hasStation)
        {
            Vector3 stationSpawnPos = Vector3.zero;
            foreach (var kvp in posToGridMap)
            {
                if (kvp.Value.x == centerGridX && kvp.Value.y == centerGridZ)
                {
                    stationSpawnPos = new Vector3(kvp.Key.x, _stationHeight, kvp.Key.z);
                    break;
                }
            }

            GameObject stationObj = InstantiatePrefabSafe(_stationPrefab, stationSpawnPos, Quaternion.identity, stationRoot);
            stationObj.name = "Station_Main";

            int stationProtectionRadius = 1; 
            foreach (var kvp in posToTileObj)
            {
                if (posToGridMap.TryGetValue(kvp.Key, out Vector2Int gridCoord))
                {
                    int distanceX = Mathf.Abs(gridCoord.x - centerGridX);
                    int distanceZ = Mathf.Abs(gridCoord.y - centerGridZ);

                    if (distanceX <= stationProtectionRadius && distanceZ <= stationProtectionRadius)
                    {
                        GameObject tileObj = kvp.Value;
                        if (tileObj != null)
                        {
                            if (defaultLayerIndex != -1) tileObj.layer = defaultLayerIndex;
                            if (tileObj.TryGetComponent<MapTileInfo>(out var tileInfo))
                            {
                                tileInfo.CanInstallRail = false;
                            }
                        }
                    }
                }
            }
        }

        int spawnExclusionRadius = 2; 
        availablePositions.RemoveAll(pos => {
            if (posToGridMap.TryGetValue(pos, out Vector2Int gridCoord))
            {
                int distanceX = Mathf.Abs(gridCoord.x - centerGridX);
                int distanceZ = Mathf.Abs(gridCoord.y - centerGridZ);
                return distanceX <= spawnExclusionRadius && distanceZ <= spawnExclusionRadius;
            }
            return false;
        });

        Debug.Log($"[MapMaker] 정중앙 (7,7) 기준 5x5 영역은 오브젝트 스폰 후보군에서 제외되었으며, 스테이션이 있는 경우 3x3 영역의 그라운드는 Default 레이어로 처리되었습니다. (스테이션 존재: {hasStation})");

        SpawnObstacles(ref availablePositions, obstaclesRoot, rand, defaultLayerIndex, posToTileObj);
        SpawnMaterials(ref availablePositions, materialsRoot, rand, defaultLayerIndex, posToTileObj);

        Debug.Log($"[MapMaker] 15x15 맵 생성 완료! (스테이션 존재: {hasStation}, MapGridPos: {_mapGridPos})");
    }

    private void SetupGroundBoxCollider(Transform groundRoot)
    {
        BoxCollider boxCollider = groundRoot.gameObject.AddComponent<BoxCollider>();

        float totalSizeX = _gridSizeX * _spacing;
        float totalSizeZ = _gridSizeZ * _spacing;

        boxCollider.size = new Vector3(totalSizeX, 1f, totalSizeZ);
        boxCollider.center = new Vector3(-1f, 0.5f, -1f);
    }

    private GameObject InstantiatePrefabSafe(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            return instance;
        }
#endif
        return Instantiate(prefab, position, rotation, parent);
    }

    private void SetupMapRoot()
    {
        GameObject existingRoot = GameObject.Find("@MapChildRoot");
        if (existingRoot != null)
        {
            DestroyImmediate(existingRoot);
        }
        GameObject newRoot = new GameObject("@MapChildRoot");
        _mapRoot = newRoot.transform;
    }

    private Transform CreateSubRoot(string name, Transform parent)
    {
        GameObject subObj = new GameObject(name);
        subObj.transform.SetParent(parent);
        return subObj.transform;
    }

    private void SpawnObstacles(ref List<Vector3> availablePositions, Transform obstaclesRoot, System.Random rand, int defaultLayerIndex, Dictionary<Vector3, GameObject> posToTileObj)
    {
        if (_obstaclePrefabs == null || _obstaclePrefabs.Count == 0) return;

        int countToSpawn = Mathf.Min(_targetObstacleCount, availablePositions.Count);
        for (int i = 0; i < countToSpawn; i++)
        {
            if (availablePositions.Count == 0) break;

            int index = rand.Next(0, availablePositions.Count);
            Vector3 basePos = availablePositions[index];
            availablePositions.RemoveAt(index);

            if (posToTileObj.TryGetValue(basePos, out GameObject tileObj))
            {
                if (defaultLayerIndex != -1) tileObj.layer = defaultLayerIndex;
                if (tileObj.TryGetComponent<MapTileInfo>(out var tileInfo))
                {
                    tileInfo.CanInstallRail = false;
                }
            }

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

            GameObject obstacleObj = InstantiatePrefabSafe(selectedPrefab, targetPos, Quaternion.identity, subRoot);
            obstacleObj.name = $"Obstacle_{i}";
        }
    }

    private void SpawnMaterials(ref List<Vector3> availablePositions, Transform materialsRoot, System.Random rand, int defaultLayerIndex, Dictionary<Vector3, GameObject> posToTileObj)
    {
        if (_materialSpawnDatas == null || _materialSpawnDatas.Count == 0) return;

        foreach (var data in _materialSpawnDatas)
        {
            if (data.Prefab == null || data.MinCount <= 0) continue;

            int spawnCount = Mathf.Min(data.MinCount, availablePositions.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                if (availablePositions.Count == 0) break;

                int index = rand.Next(0, availablePositions.Count);
                Vector3 basePos = availablePositions.Count > 0 ? availablePositions[index] : Vector3.zero;
                availablePositions.RemoveAt(index);

                if (posToTileObj.TryGetValue(basePos, out GameObject tileObj))
                {
                    if (defaultLayerIndex != -1) tileObj.layer = defaultLayerIndex;
                    if (tileObj.TryGetComponent<MapTileInfo>(out var tileInfo))
                    {
                        tileInfo.CanInstallRail = false;
                    }
                }

                InstantiateMaterial(data.Prefab, basePos, materialsRoot);
            }
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

        GameObject resourceObj = InstantiatePrefabSafe(prefab, targetPos, Quaternion.identity, subRoot);
        resourceObj.name = $"Resource_{subRoot.childCount - 1}";
    }
}