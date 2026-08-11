using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private Dictionary<string, Queue<GameObject>> _objectPools = new();
    private Dictionary<string, List<GameObject>> _activedObjects = new();
    private Transform _poolRoot;

    private Func<string, GameObject> _resourceLoader;

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
    public void Init(Transform poolRoot, Dictionary<string, int> initialPoolData, Func<string, GameObject> resourceLoader = null)
    {
        _poolRoot = poolRoot;
        _resourceLoader = resourceLoader;

        if (initialPoolData == null)
        {
            Debug.LogWarning("[PoolManager] 초기화할 풀 데이터가 없습니다. 빈 상태로 시작합니다.");
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
                DespawnToPool(activeList[i]);
            }
        }
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
        GameObject gameObject = null;

        if (_resourceLoader != null)
        {
            gameObject = _resourceLoader.Invoke(address);
        }

        if (gameObject == null)
        {
            gameObject = Resources.Load<GameObject>($"Pool/{address}");
        }

        return gameObject;
    }
}