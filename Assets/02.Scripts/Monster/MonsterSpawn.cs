using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

public class MonsterSpawn : SingletonBase<MonsterSpawn>
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform _mainTrain;
    [SerializeField] private float _spawnRadius = 15;
    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private int _maxMonsterLimit = 50;
    [SerializeField] private float _initialSpawnDelay = 30f;
    [SerializeField] private float _spawnYOffset = 2.0f;

    [Header("Phase Settings")]
    [SerializeField] private List<string> _phase1Monsters = new List<string> { "Monster_01" };
    [SerializeField] private List<string> _phase2Monsters = new List<string> { "Monster_01", "Monster_02" };
    [SerializeField] private float _phase2StartTime = 120f;

    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _checkRadius = 1f;

    private float _elapsedTime = 0;
    private int _currentMonsterCount = 0;
    private bool _isSpawning = false;
    private CancellationTokenSource _cts;

    private async void Start()
    {
        await InitializePoolAsync();
    }

    private void Update()
    {
        if (_isSpawning)
        {
            _elapsedTime += Time.deltaTime;
        }
    }

    private void OnEnable()
    {
        TrainManager.OnTrainSpawn += HandleTrainSpawn;
    }

    private void OnDisable()
    {
        TrainManager.OnTrainSpawn -= HandleTrainSpawn;
    }

    public void StartSpawning()
    {
        if (_isSpawning == true)
        {
            return;
        }

        _isSpawning = true;
        _cts = new CancellationTokenSource();
        SpawnLoopRoutine(_cts.Token).Forget();
    }

    public void StopSpawning()
    {
        _isSpawning = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid SpawnLoopRoutine(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_mainTrain != null && _mainTrain.gameObject.activeInHierarchy)
            {
                if (_currentMonsterCount < _maxMonsterLimit)
                {
                    SpawnMonsterAsync().Forget();
                }
            }
            await UniTask.Delay(System.TimeSpan.FromSeconds(_spawnInterval), cancellationToken: token);
        }
    }

    private async UniTask SpawnMonsterAsync()
    {
        Vector3 spawnPos = GetSafeSpawnPosition();

        if (spawnPos == Vector3.zero)
        {
            return;
        }
        string monsterId = GetMonsterIdForCurrentPhase();

        GameObject newMonster = PoolManager.Instance.SpawnFromPool(monsterId, new Vector3(0, -9999f, 0));

        if (newMonster != null)
        {
            await UniTask.DelayFrame(2);

            if (newMonster == null || !newMonster.activeInHierarchy)
            {
                return;
            }

            newMonster.transform.position = spawnPos;

            MonsterData monsterData = DataManager.Instance.GetData<MonsterData>(monsterId);

            if (monsterData != null)
            {
                MonsterMove moveScript = newMonster.GetComponent<MonsterMove>();
                if (moveScript != null)
                {
                    moveScript.Initialize(monsterData, _mainTrain);
                }
            }

            MonsterHealth healthScript = newMonster.GetComponent<MonsterHealth>();

            if (healthScript != null)
            {
                healthScript.Initialize(monsterData);
            }

            _currentMonsterCount++;
        }
    }

    private Vector3 GetSafeSpawnPosition()
    {
        int maxAttempts = 20;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            Vector3 spawnDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 targetPos = _mainTrain.position + (spawnDirection * _spawnRadius);

            Vector3 rayOrigin = new Vector3(targetPos.x, targetPos.y + 10f, targetPos.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    Vector3 finalSpawnPos = new Vector3(targetPos.x, hit.point.y + _spawnYOffset, targetPos.z);

                    if (!Physics.CheckSphere(finalSpawnPos, _checkRadius, _obstacleLayer))
                    {
                        return finalSpawnPos;
                    }
                }
            }
        }

        return Vector3.zero;
    }

    private string GetMonsterIdForCurrentPhase()
    {
        List<string> currentPool;

        if (_elapsedTime >= _phase2StartTime)
        {
            currentPool = _phase2Monsters;
        }
        else
        {
            currentPool = _phase1Monsters;
        }

        int randomIndex = Random.Range(0, currentPool.Count);
        return currentPool[randomIndex];
    }

    public void DecreaseMonsterCount()
    {
        if (_currentMonsterCount > 0)
        {
            _currentMonsterCount--;
        }
    }

    private async UniTask InitializePoolAsync()
    {
        string targetMonsterId = "Monster_01";
        string targetMonster2Id = "Monster_02";
        string targetMonster3Id = "Monster_03";
        string targetDebuffMonsterId = "DebuffMonster_01";
        string targetDebuffMonster2Id = "DebuffMonster_02";
        string targetDebuffMonster3Id = "DebuffMonster_03";
        string targetDebuffMonster4Id = "DebuffMonster_04";
        string projectileId = "MonsterProjectile";

        GameObject monsterPrefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetMonsterId);
        GameObject monster2Prefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetMonster2Id);
        GameObject monster3Prefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetMonster3Id);
        GameObject debuffMonsterPrefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetDebuffMonsterId);
        GameObject debuffMonster2Prefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetDebuffMonster2Id);
        GameObject debuffMonster3Prefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetDebuffMonster3Id);
        GameObject debuffMonster4Prefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetDebuffMonster4Id);
        GameObject projectilePrefab = await ResourceManager.Instance.LoadAsset<GameObject>(projectileId);

        if (monsterPrefab == null)
        {
            Debug.LogError($"{targetMonsterId} 프리팹을 찾을 수 없습니다! Addressable 체크를 확인하세요.");
            return;
        }

        Dictionary<string, int> initialPool = new Dictionary<string, int>
        {
            { targetMonsterId, 10 },
            { targetMonster2Id, 10 },
            { targetMonster3Id, 10 },
            { targetDebuffMonsterId, 5 },
            { targetDebuffMonster2Id, 5 },
            { targetDebuffMonster3Id, 5 },
            { targetDebuffMonster4Id, 5 },
            { projectileId, 20 }
        };

        Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>
        {
            { targetMonsterId, monsterPrefab },
            { targetMonster2Id, monster2Prefab },
            { targetMonster3Id, monster3Prefab },
            { targetDebuffMonsterId, debuffMonsterPrefab },
            { targetDebuffMonster2Id, debuffMonster2Prefab },
            { targetDebuffMonster3Id, debuffMonster3Prefab },
            { targetDebuffMonster4Id, debuffMonster4Prefab },
            { projectileId, projectilePrefab }
        };

        PoolManager.Instance.Init(this.transform, initialPool, prefabMap);

        Debug.Log("[MonsterSpawn] 몬스터 프리팹 사전 로드 및 풀 초기화 완료!");
    }

    private void HandleTrainSpawn(Transform trainTransform)
    {
        _mainTrain = trainTransform;
        Debug.Log($"{_initialSpawnDelay}초 대기 시작.");

        WaitAndStartSpawningAsync().Forget();
    }

    private async UniTaskVoid WaitAndStartSpawningAsync()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(_initialSpawnDelay));

        if (_mainTrain != null && _mainTrain.gameObject.activeInHierarchy)
        {
            Debug.Log("몬스터 스폰 시작");
            StartSpawning();
        }
    }
}