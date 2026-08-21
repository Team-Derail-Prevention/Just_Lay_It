using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{
    [Header("Game Start Settings")]
    [SerializeField] private int _startingCarriageCount = 3;
    [SerializeField, Min(0)] private int _resumeCountdownSeconds = 3;

    [Header("Runtime State")]
    [SerializeField] private GameState _currentGameState = GameState.Ready;

    private readonly TimeManager _timeManager = new TimeManager();

    private Transform _managerRoot;
    private StationObject _activeStation;
    private CentralTerminal _activeTerminal;
    private bool _isStartingGame;
    private bool _isCountdownRunning;
    private int _sessionVersion;
    private float _playTime;
    private int _lastNotifiedTime;

    public event Action<int> OnCountdownChanged;

    public static DataManager Data => DataManager.Instance;
    public static ResourceManager Resource => ResourceManager.Instance;
    public static PoolManager Pool => PoolManager.Instance;
    public static MapManager Map => MapManager.Instance;
    public static TrainManager Train => TrainManager.Instance;
    public static RailManager Rail => RailManager.Instance;
    public static MonsterSpawn Monster => MonsterSpawn.Instance;
    public static DroneManager Drone => DroneManager.Instance;
    public static UIManager UI => UIManager.Instance;
    public static TrainStatusEventHub TrainEventHub => TrainStatusEventHub.Instance;
    public static ResourceStatusEventHub ResourceEventHub => ResourceStatusEventHub.Instance;
    public static NetworkRailService NetworkRail => NetworkRailService.Instance;
    public static NetworkUpgradeService UpgradeService => NetworkUpgradeService.Instance;
    public static TimeManager Time => Instance != null ? Instance._timeManager : null;

    public GameState CurrentGameState => _currentGameState;

    protected override void Init()
    {
        base.Init();
        if (Instance != this) return;

        InitManagerRoot();
        OrganizeExistingManagers();
        InitializeGameFlowAsync().Forget();
    }

    private void OnEnable()
    {
        StationObject.OnStationEntered += HandleStationEntered;
        CentralTerminal.OnCentralTerminalEntered += HandleCentralTerminalEntered;
        CentralTerminal.OnExitDirectionSelected += SelectExitDirection;
    }

    private void Start()
    {
        RefreshManagerHierarchyAsync().Forget();
    }

    private void Update()
    {
        if (_currentGameState != GameState.Playing) return;

        _playTime += UnityEngine.Time.deltaTime;
        int currentSecond = (int)_playTime;

        if (currentSecond == _lastNotifiedTime) return;

        _lastNotifiedTime = currentSecond;
        TrainEventHub?.NotifyPlayTimeChanged(_lastNotifiedTime);
    }

    private void OnDisable()
    {
        StationObject.OnStationEntered -= HandleStationEntered;
        CentralTerminal.OnCentralTerminalEntered -= HandleCentralTerminalEntered;
        CentralTerminal.OnExitDirectionSelected -= SelectExitDirection;
    }

    public async UniTask StartGame()
    {
        if (_isStartingGame || _currentGameState == GameState.Playing)
        {
            Debug.LogWarning("[GameManager] 게임 시작 요청이 이미 처리 중이거나 게임이 진행 중입니다.");
            return;
        }

        _isStartingGame = true;

        try
        {
            RefreshManagerHierarchy();
            if (!await EnsureGameDataLoadedAsync()) return;

            ClearCurrentSession();
            ChangeGameState(GameState.Ready);

            bool isMapGenerated = await Map.GenerateMapAsync(this.GetCancellationTokenOnDestroy());
            if (!isMapGenerated)
            {
                Debug.LogError("[GameManager] 맵 생성에 실패하여 게임 시작을 취소합니다.");
                return;
            }
            // Map.OnMapGenerated 이벤트로 RailManager의 타일 조회 상태 초기화

            // 드론은 TrainManager.OnTrainSpawn을 구독하므로 기차보다 먼저 생성되어야 함
            if (Drone != null)
            {
                await Drone.SpawnAllAsync();
            }

            Train.SpawnFullTrain(_startingCarriageCount);

            // MonsterSpawn은 TrainManager.OnTrainSpawn을 구독하여 풀 초기화 후 스폰을 시작

            ChangeGameState(GameState.EventPaused);
            Debug.Log("[GameManager] 맵, 기차, 몬스터 스폰 완료");

        }
        finally
        {
            _isStartingGame = false;
        }
    }

    /// StationObject가 보상 처리하고, GameManager가 다음 인게임 사이클을 재개
    /// 역 UI가 회복 여부를 결정한 후 출발을 위해 호출하는 메서드
    public void CompleteStation(bool isHealed)
    {
        if (_currentGameState != GameState.EventPaused || _activeStation == null)
        {
            Debug.LogWarning("[GameManager] 완료할 활성 역 이벤트가 없습니다.");
            return;
        }

        _activeStation.ExitStation(isHealed);
        _activeStation = null;

        Train?.DepartStation();
        StartCountdownAsync().Forget();

        // TODO: Station UI가 실제 자재 소모 및 열차 회복 결과를 CompleteStation에 전달 필요(?)
    }

    public void HandleStationArrival()
    {
        HandleStationArrival(null, string.Empty);
    }

    public void HandleTerminalArrival()
    {
        HandleTerminalArrival(null);
    }

    /// CentralTerminal 이벤트 출구 선택시 호출 메서드
    public void SelectExitDirection(int directionIndex)
    {
        if (_currentGameState != GameState.EventPaused)
        {
            Debug.LogWarning("[GameManager] 이벤트가 정지 상태가 아니므로 출구 선택을 무시합니다.");
            return;
        }

        Debug.Log($"[GameManager] 출구 방향 {directionIndex}번을 선택했습니다.");
        _activeTerminal = null;

        ChangeGameState(GameState.ExitSelected);
        StartCountdownAsync().Forget();

        // TODO: TrainManager에서 출구 방향별 기차 위치·경로설정 메서드 필요
        // TODO: Terminal UI가 재료 적재, 소비, 무기 추가·강화 결과를 GameManager로 전달해야할 듯
    }

    public void GameOver()
    {
        if (_currentGameState == GameState.GameOver) return;

        Debug.Log("[GameManager] 게임 오버: 진행 중인 시스템을 정리합니다.");
        ChangeGameState(GameState.GameOver);
        PauseGameplayTime();

        ClearCurrentSession();

        // TODO: Result UI를 열고 로비로 돌아가는 UI 흐름필요
    }

    private void InitManagerRoot()
    {
        GameObject rootObject = GameObject.Find("@Managers");
        if (rootObject == null)
        {
            rootObject = new GameObject("@Managers");
        }

        _managerRoot = rootObject.transform;
        if (transform.parent != _managerRoot)
        {
            transform.SetParent(_managerRoot);
        }
    }

    private void OrganizeExistingManagers()
    {
        SetManagerParent(UI);
        SetManagerParent(Resource);
        SetManagerParent(Data);
        SetManagerParent(Map);
        SetManagerParent(Pool);
        SetManagerParent(Train);
        SetManagerParent(Rail);
        SetManagerParent(Monster);
        SetManagerParent(TrainEventHub);
        SetManagerParent(ResourceEventHub);
        SetManagerParent(NetworkRail);
        SetManagerParent(UpgradeService);
        SetManagerParent(Drone);
    }

    public void RefreshManagerHierarchy()
    {
        if (_managerRoot == null)
        {
            InitManagerRoot();
        }

        OrganizeExistingManagers();
    }

    public void RegisterManager(Component manager)
    {
        if (manager == null) return;

        if (_managerRoot == null)
        {
            InitManagerRoot();
        }

        SetManagerParent(manager);
    }
    private async UniTaskVoid InitializeGameFlowAsync()
    {
        if (!await WaitForRequiredManagersAsync()) return;

        await EnsureGameDataLoadedAsync();
        ChangeGameState(GameState.Ready);
        Debug.Log("[GameManager] 초기화 및 데이터 로드가 완료되었습니다.");
    }

    private async UniTask<bool> EnsureGameDataLoadedAsync()
    {
        if (Resource == null || Data == null)
        {
            Debug.LogError("[GameManager] ResourceManager 또는 DataManager가 없습니다.");
            return false;
        }

        if (!Data.IsLoaded)
        {
            Debug.Log("[GameManager] 게임 데이터 로드를 시작합니다.");
            await Data.LoadAllDatasAsync(this.GetCancellationTokenOnDestroy());
        }

        return Data.IsLoaded;
    }

    private async UniTask<bool> WaitForRequiredManagersAsync()
    {
        await UniTask.WaitUntil(
            () => UI != null && Data != null && Resource != null && Map != null && Train != null,
            cancellationToken: this.GetCancellationTokenOnDestroy());

        return ValidateStartDependencies();
    }

    private bool ValidateStartDependencies()
    {
        bool isValid = true;
        isValid &= ValidateManager(Data, nameof(DataManager));
        isValid &= ValidateManager(Resource, nameof(ResourceManager));
        isValid &= ValidateManager(Map, nameof(MapManager));
        isValid &= ValidateManager(Train, nameof(TrainManager));
        return isValid;
    }

    private void HandleStationEntered(StationObject station, string stationId)
    {
        HandleStationArrival(station, stationId);
    }

    private void HandleCentralTerminalEntered(CentralTerminal terminal)
    {
        HandleTerminalArrival(terminal);
    }

    private void HandleStationArrival(StationObject station, string stationId)
    {
        if (_currentGameState != GameState.Playing) return;

        _activeStation = station;
        PauseGameplayTime();
        StopAndDespawnMonsters();
        ChangeGameState(GameState.EventPaused);

        Debug.Log($"[GameManager] 역 도착: '{stationId}' 이벤트 처리를 기다립니다.");
        // TODO: Station UI를 열고 CompleteStation(bool)을 호출하도록 연결필요
    }

    private void HandleTerminalArrival(CentralTerminal terminal)
    {
        if (_currentGameState != GameState.Playing) return;

        _activeTerminal = terminal;
        PauseGameplayTime();
        StopAndDespawnMonsters();
        RemovePlayerPlacedRails();
        ChangeGameState(GameState.EventPaused);

        Debug.Log("[GameManager] 터미널 도착: 출구 방향 선택을 기다립니다.");
        // TODO: Terminal UI를 열고 CentralTerminal.SelectExitGate(int)와 연결필요(?)
    }

    private void PauseGameplayTime()
    {
        if (Time != null && !Time.IsPaused)
        {
            Time.Pause();
        }
        else
        {
            UnityEngine.Time.timeScale = 0f;
        }
        Debug.Log("[GameManager] 게임 시간을 일시정지했습니다.");
    }

    private void ResumeGameplayTime()
    {
        if (Time != null)
        {
            while (Time.IsPaused)
            {
                Time.Resume();
            }
        }

        UnityEngine.Time.timeScale = 1f;
    }

    public async UniTask StartCountdownAsync()
    {
        if (_isCountdownRunning) return;

        _isCountdownRunning = true;
        int sessionVersion = _sessionVersion;

        try
        {
            for (int remaining = _resumeCountdownSeconds; remaining > 0; remaining--)
            {
                OnCountdownChanged?.Invoke(remaining); // UI 카운트 표시

                Debug.Log($"[GameManager] {remaining}초 후 다음 구간을 시작합니다.");
                await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: this.GetCancellationTokenOnDestroy());

                if (sessionVersion != _sessionVersion) return;
            }

            OnCountdownChanged?.Invoke(0); // UI 카운트 끝 신호 

            ResumeMonsterSpawning();
            ResumeGameplayTime();
            ChangeGameState(GameState.Playing);
        }
        finally
        {
            _isCountdownRunning = false;
        }
    }

    private void ResumeMonsterSpawning()
    {
        if (Monster != null)
        {
            Monster.StartSpawning();
        }
        else
        {
            Debug.LogWarning("[GameManager] MonsterSpawn이 없어 몬스터 스폰을 재개하지 못했습니다.");
        }
    }

    private void StopAndDespawnMonsters()
    {
        Monster?.StopSpawning();
        Pool?.AllDespawnToPool();

        // TODO: PoolManager 몬스터 정리(스킬정리도 필요한지 확인필요)
    }

    private void RemovePlayerPlacedRails()
    {
        if (Rail == null)
        {
            Debug.LogWarning("[GameManager] RailManager가 없어 플레이어 레일을 정리하지 못했습니다.");
            return;
        }

        Rail.RemoveAllRail();
        Debug.Log("[GameManager] 플레이어가 설치한 레일을 모두 제거했습니다.");
    }

    private void ResetSessionState()
    {
        _activeStation = null;
        _activeTerminal = null;
        _playTime = 0f;
        _lastNotifiedTime = 0;
    }

    /// 게임 오버 로비 복귀 공통으로 사용 세션 정리
    private void ClearCurrentSession()
    {
        _sessionVersion++;
        Drone?.DespawnAll();
        StopAndDespawnMonsters();
        RemovePlayerPlacedRails();
        Train?.ClearExistingTrain();
        Map?.ClearMap();
        ResetSessionState();
    }

    /// 결과 UI가 확인된 뒤 호출
    public void ReturnToLobby()
    {
        ClearCurrentSession();
        ResumeGameplayTime();
        ChangeGameState(GameState.Ready);
        UI?.OpenContentUI(UIType.LobbyUI);

        // TODO: TrainManager 정리 메서드가 추가되면 ClearCurrentSession에서 호출필요
    }

    private void SetManagerParent(Component manager)
    {
        if (manager != null && manager.transform.parent != _managerRoot)
        {
            manager.transform.SetParent(_managerRoot);
        }
    }

    private async UniTaskVoid RefreshManagerHierarchyAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, this.GetCancellationTokenOnDestroy());
        RefreshManagerHierarchy();
    }

    private bool ValidateManager(Component manager, string managerName)
    {
        if (manager != null) return true;

        Debug.LogError($"[GameManager] 필수 매니저 '{managerName}'를 찾지 못했습니다.");
        return false;
    }


    private void ChangeGameState(GameState newState)
    {
        _currentGameState = newState;
        Debug.Log($"[GameManager] 게임 상태 변경: {_currentGameState}");
    }
}

