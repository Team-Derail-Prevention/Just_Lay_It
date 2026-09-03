using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private Dictionary<string, Queue<GameObject>> _objectPools = new();
    private Dictionary<string, List<GameObject>> _activedObjects = new();
    private Dictionary<string, GameObject> _prefabMap = new(); // poolId -> prefab, 여러 시스템이 각자 등록해도 서로 안 지워짐
    private Transform _poolRoot;

    public static event Action OnAllDespawnedToPool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 여러 시스템(MonsterSpawn, WeaponFire 등)이 각자 Init()을 호출해도 서로의
    // 등록을 안 지우도록, prefabMap은 통째로 교체하지 않고 키 단위로 합쳐짐(merge).
    // poolRoot도 null이 아닐 때만 갱신해서 먼저 설정한 쪽이 유지되게 함.
    public void Init(Transform poolRoot, Dictionary<string, int> initialPoolData, Dictionary<string, GameObject> prefabMap = null)
    {
        if (poolRoot != null)
        {
            _poolRoot = poolRoot;
        }

        if (prefabMap != null)
        {
            foreach (var kvp in prefabMap)
            {
                _prefabMap[kvp.Key] = kvp.Value;
            }
        }

        if (initialPoolData == null)
        {
            if (prefabMap == null)
            {
                Debug.LogWarning("[PoolManager] 초기화할 풀 데이터도 프리팹 맵도 없습니다. 아무 것도 하지 않습니다.");
            }
            return;
        }

        foreach (var kvp in initialPoolData)
        {
            string poolId = kvp.Key;
            int initSize = kvp.Value;

            if (!_objectPools.ContainsKey(poolId))
            {
                _objectPools.Add(poolId, new Queue<GameObject>());
            }

            GameObject prefab = LoadGameObject(poolId);
            if (prefab != null)
            {
                for (int i = 0; i < initSize; i++)
                {
                    CreateNewObject(poolId, prefab);
                }
            }
        }
        Debug.Log("[PoolManager] 독립형 풀 초기화 완료!");
    }

    public GameObject SpawnFromPool(string poolId)
        => GetFromPool(poolId, Vector3.zero, Quaternion.identity);

    public GameObject SpawnFromPool(string poolId, Vector3 position)
        => GetFromPool(poolId, position, Quaternion.identity);

    public GameObject SpawnFromPool(string poolId, Vector3 position, Quaternion rotation)
        => GetFromPool(poolId, position, rotation);

    public GameObject SpawnFromPool(string poolId, Vector3 position, Quaternion rotation, Transform parent)
        => GetFromPool(poolId, position, rotation, parent);

    public T SpawnFromPool<T>(string poolId) where T : Component
    {
        GameObject obj = GetFromPool(poolId, Vector3.zero, Quaternion.identity);
        if (obj.TryGetComponent<T>(out T component))
            return component;
        throw new Exception($"[PoolManager] Pool Id({poolId})에 컴포넌트({typeof(T)})가 존재하지 않습니다.");
    }

    public T SpawnFromPool<T>(string poolId, Vector3 position, Quaternion rotation) where T : Component
    {
        GameObject obj = GetFromPool(poolId, position, rotation);
        if (obj.TryGetComponent<T>(out T component))
            return component;
        throw new Exception($"[PoolManager] Pool Id({poolId})에 컴포넌트({typeof(T)})가 존재하지 않습니다.");
    }

    public void DespawnToPool(GameObject obj)
    {
        if (obj == null) return;

        string poolId = obj.name; // 오브젝트 이름으로 풀 키 매칭

        if (!_objectPools.TryGetValue(poolId, out Queue<GameObject> pool))
        {
            Debug.LogError($"[PoolManager] Pool Id({poolId})에 해당하는 풀이 없습니다. 파괴합니다.");
            Destroy(obj);
            return;
        }

        if (_activedObjects.TryGetValue(poolId, out List<GameObject> activeList))
        {
            activeList.Remove(obj);
        }

        obj.SetActive(false);
        if (_poolRoot != null)
        {
            obj.transform.SetParent(_poolRoot, false);
        }
        pool.Enqueue(obj);
    }

    public void AllDespawnToPool()
    {
        foreach (List<GameObject> activeList in _activedObjects.Values)
        {
            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                GameObject obj = activeList[i];

                if (obj.TryGetComponent<IForceDespawnable>(out IForceDespawnable despawnable))
                {
                    despawnable.OnForcedDespawn();
                }

                DespawnToPool(obj);
            }
        }

        OnAllDespawnedToPool?.Invoke();
    }

    private GameObject GetFromPool(string poolId, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!_objectPools.ContainsKey(poolId))
        {
            _objectPools[poolId] = new Queue<GameObject>();
            _activedObjects[poolId] = new List<GameObject>();
        }

        Queue<GameObject> poolQueue = _objectPools[poolId];
        if (poolQueue.Count <= 0)
        {
            GameObject prefab = LoadGameObject(poolId);
            if (prefab != null)
            {
                CreateNewObject(poolId, prefab);
            }
            else
            {
                Debug.LogError($"[PoolManager] '{poolId}' 에셋을 로드할 수 없어 생성에 실패했습니다.");
                return null;
            }
        }

        GameObject obj = poolQueue.Dequeue();

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }

        obj.SetActive(true);

        if (!_activedObjects.ContainsKey(poolId))
        {
            _activedObjects[poolId] = new List<GameObject>();
        }
        _activedObjects[poolId].Add(obj);

        return obj;
    }

    private GameObject CreateNewObject(string poolId, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, _poolRoot);
        obj.name = poolId;
        obj.SetActive(false);
        _objectPools[poolId].Enqueue(obj);
        return obj;
    }

    private GameObject LoadGameObject(string address)
    {
        if (_prefabMap.TryGetValue(address, out GameObject mappedPrefab) && mappedPrefab != null)
        {
            return mappedPrefab;
        }

        return Resources.Load<GameObject>($"Pool/{address}");
    }
}