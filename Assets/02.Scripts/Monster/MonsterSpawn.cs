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
        if(_isSpawning == true)
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
                    SpawnMonster();
                }
            }
            await UniTask.Delay(System.TimeSpan.FromSeconds(_spawnInterval), cancellationToken: token);
        }
    }

    private void SpawnMonster()
    {
        Vector3 spawnPos = GetSafeSpawnPosition();

        if (spawnPos == Vector3.zero)
        {
            return;
        }
        string monsterId = GetMonsterIdForCurrentPhase();

        GameObject newMonster = PoolManager.Instance.SpawnFromPool(monsterId, spawnPos );

        if (newMonster != null)
        {
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
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            Vector3 spawnDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);

            Vector3 spawnPos = _mainTrain.position + (spawnDirection * _spawnRadius);

            spawnPos.y = _mainTrain.position.y + _spawnYOffset;

            if (!Physics.CheckSphere(spawnPos, _checkRadius, _obstacleLayer))
            {
                return spawnPos;
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
        string projectileId = "MonsterProjectile";

        GameObject monsterPrefab = await ResourceManager.Instance.LoadAsset<GameObject>(targetMonsterId);
        GameObject projectilePrefab = await ResourceManager.Instance.LoadAsset<GameObject>(projectileId);

        if (monsterPrefab == null)
        {
            Debug.LogError($"{targetMonsterId} 프리팹을 찾을 수 없습니다! Addressable 체크를 확인하세요.");
            return;
        }

        Dictionary<string, int> initialPool = new Dictionary<string, int>
        {
            { targetMonsterId, 10 },
            { projectileId, 20 }
        };

        PoolManager.Instance.Init(this.transform, initialPool, (id) =>
        {
            if (id == targetMonsterId) return monsterPrefab;
            if (id == projectileId) return projectilePrefab;
            return null;
        });

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
