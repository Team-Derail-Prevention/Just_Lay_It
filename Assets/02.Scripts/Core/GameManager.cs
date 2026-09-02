using Cysharp.Threading.Tasks;
using Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{
    [Header("Game Start Settings")]
    [SerializeField] private int _startingCarriageCount = 3;
    [SerializeField, Min(0)] private int _resumeCountdownSeconds = 3;

    [Header("Runtime State")]
    [SerializeField] private GameState _currentGameState = GameState.Ready;

    [Header("Session Records")]
    [SerializeField] private int _sessionKillCount = 0;
    [SerializeField] private int _sessionEarnedStone = 0;

    [Header("Game Stage")]
    [SerializeField] private GameStage _currentGameStage = GameStage.Stage1;

    [SerializeField, Min(0f)] private float _stage1ClearTimeLimit = 300f;
    [SerializeField, Min(0f)] private float _stage2ClearTimeLimit = 480f;
    [SerializeField, Min(0f)] private float _stage3ClearTimeLimit = 720f;

    private readonly TimeManager _timeManager = new TimeManager();

    private Transform _managerRoot;
    private StationObject _activeStation;
    private CentralTerminal _activeTerminal;
    private bool _isStartingGame;
    private bool _isCountdownRunning;
    private int _sessionVersion;
    private float _playTime;
    private int _lastNotifiedTime;
    private int _currentMapSize = 3;

    private readonly HashSet<StationObject> _completedStations = new();
    public int CompletedStationCount => _completedStations.Count;

    public int SessionKillCount => _sessionKillCount;
    public int SessionEarnedStone => _sessionEarnedStone;
    public int RequiredStationCount => (_currentMapSize - 1) * 2;

    public event Action<int> OnCountdownChanged;
    public event Action OnGameCleared;
    public event Action<GameState> OnGameStateChanged;

    public bool IsStageSelectUnlocked => Save != null && Save.HasClearedAllStagesSpecial;

    public static DataManager Data => DataManager.Instance;
    public static ResourceManager Resource => ResourceManager.Instance;
    public static PoolManager Pool => PoolManager.Instance;
    public static MapManager Map => MapManager.Instance;
    public static TrainManager Train => TrainManager.Instance;
    public static RailManager Rail => RailManager.Instance;
    public static MonsterSpawn Monster => MonsterSpawn.Instance;
    public static DroneManager Drone => DroneManager.Instance;
    public static UIManager UI => UIManager.Instance;
    public static SaveManager Save => SaveManager.Instance;
    public static WeaponStatManager Weapon => WeaponStatManager.Instance;
    public static TimeManager Time => Instance != null ? Instance._timeManager : null;

    public static MaterialTransferEventHub MaterialTransferEventHub => MaterialTransferEventHub.Instance;
    public static MoneyRequestEventHub MoneyRequestEventHub => MoneyRequestEventHub.Instance;
    public static TrainStatusEventHub TrainStatusEventHub => TrainStatusEventHub.Instance;
    public static ResourceStatusEventHub ResourceStatusEventHub => ResourceStatusEventHub.Instance;

    public static NetworkTrainCargoService NetworkTrainCargeService => NetworkTrainCargoService.Instance;
    public static NetworkTrainStrengtheningService NetworkTrainStrengtheningService => NetworkTrainStrengtheningService.Instance;
    public static NetworkAugmentService NetworkAugmentService => NetworkAugmentService.Instance;
    public static NetworkRailService NetworkRailService => NetworkRailService.Instance;
    public static NetworkUpgradeService NetworkUpgradeService => NetworkUpgradeService.Instance;
    public static NetworkResourceService NetworkResourceService => NetworkResourceService.Instance;
    public static NetworkWarehouseService NetworkWarehouseService => NetworkWarehouseService.Instance;
    public static NetworkGachaService NetworkGachaService => NetworkGachaService.Instance;

    public GameState CurrentGameState => _currentGameState;
    public GameStage CurrentGameStage => _currentGameStage;

    protected override void Init()
    {
        base.Init();
        if (Instance != this)
        {
            return;
        }

        InitManagerRoot();
        OrganizeExistingManagers();
        InitializeGameFlowAsync().Forget();
    }

    private void OnEnable()
    {
        StationObject.OnStationEntered += HandleStationEntered;
        CentralTerminal.OnCentralTerminalEntered += HandleCentralTerminalEntered;
        CentralTerminal.OnExitDirectionSelected += SelectExitDirection;
        MonsterHealth.OnMonsterDiedWithStone += HandleMonsterDied;
    }

    private void Start()
    {
        RefreshManagerHierarchyAsync().Forget();
        Debug.Log($"[GamaManager] 스테이지 체크 {CurrentGameStage}");
    }

    private void Update()
    {
        if (CurrentGameState != GameState.Playing)
        {
            return;
        }

        _playTime += UnityEngine.Time.deltaTime;
        int currentSecond = (int)_playTime;

        if (currentSecond == _lastNotifiedTime)
        {
            return;
        }

        _lastNotifiedTime = currentSecond;
        TrainStatusEventHub?.NotifyPlayTimeChanged(_lastNotifiedTime);
    }

    private void OnDisable()
    {
        StationObject.OnStationEntered -= HandleStationEntered;
        CentralTerminal.OnCentralTerminalEntered -= HandleCentralTerminalEntered;
        CentralTerminal.OnExitDirectionSelected -= SelectExitDirection;
        MonsterHealth.OnMonsterDiedWithStone -= HandleMonsterDied;
    }

#if UNITY_EDITOR
    public void SetGameStageForCheat(GameStage stage)
    {
        SetGameStage(stage);
        Debug.Log($"[GameManager] 치트 적용: Stage {(int)_currentGameStage} 선택. 다음 게임은 {GetMapSize(_currentGameStage)}x{GetMapSize(_currentGameStage)} 맵으로 시작합니다.");
    }
#endif

    public void SetGameStage(GameStage stage)
    {
        _currentGameStage = stage;
    }

    public async UniTask StartGame()
    {
        if (_isStartingGame || CurrentGameState == GameState.Playing)
        {
            Debug.LogWarning("[GameManager] 게임 시작 요청이 이미 처리 중이거나 게임이 진행 중입니다.");
            return;
        }

        _isStartingGame = true;

        try
        {
            RefreshManagerHierarchy();

            if (!await EnsureGameDataLoadedAsync())
            {
                return;
            }

            Save?.IncreaseTotalPlayCount();
            ClearCurrentSession();
            ChangeGameState(GameState.Ready);

            NetworkResourceService.ResetRun();
            NetworkWarehouseService.ResetRun();
            NetworkRailService.ResetRun();
            NetworkTrainStrengtheningService.ResetRun();
            NetworkTrainCargeService.ResetRun();
            NetworkAugmentService.ResetRun();
            NetworkGachaService.ResetRun();

            _currentMapSize = GetMapSize(_currentGameStage);

            bool isMapGenerated = await Map.GenerateMapAsync(this.GetCancellationTokenOnDestroy());
            if (!isMapGenerated)
            {
                Debug.LogError("[GameManager] 맵 생성에 실패하여 게임 시작을 취소합니다.");
                return;
            }

            if (_currentMapSize > 3)
            {
                bool isExpanded = await Map.ExpandMapAsync(_currentMapSize, this.GetCancellationTokenOnDestroy());

                if (!isExpanded)
                {
                    Debug.LogError("[GameManager] 맵 확장 실패");
                    return;
                }
            }

            if (Drone != null)
            {
                await Drone.SpawnAllAsync();
            }

            await Train.SpawnTerminalTrainAsync(_startingCarriageCount);
            NetworkAugmentService.GrantRandomStartingWeapon();

            ChangeGameState(GameState.EventPaused);
            Debug.Log("[GameManager] 맵, 기차, 몬스터 스폰 완료");

        }
        finally
        {
            _isStartingGame = false;
        }
    }

    public void CompleteStation(int stoneTaken, int citizenBoarded)
    {
        if (CurrentGameState != GameState.EventPaused || _activeStation == null)
        {
            Debug.LogWarning("[GameManager] 완료할 활성 역 이벤트가 없습니다.");
            return;
        }

        Train?.SpawnStationTrain(_activeStation, _startingCarriageCount);
        // Train?.AddVisitedStation(_activeStation.transform);

        _activeStation.ExitStation(stoneTaken, citizenBoarded);

        _completedStations.Add(_activeStation);

        _activeStation = null;
        UI?.CloseStationArrivalUI();

        HandleStationArrival();

        Train?.DepartStation();
        StartCountdownAsync().Forget();
    }

    public void HandleStationArrival()
    {
        HandleStationArrival(null, string.Empty);
    }

    public void HandleTerminalArrival()
    {
        HandleTerminalArrivalAsync(null).Forget();
    }

    public void SelectExitDirection(int directionIndex)
    {
        if (CurrentGameState != GameState.EventPaused)
        {
            Debug.LogWarning("[GameManager] 이벤트가 정지 상태가 아니므로 출구 선택을 무시합니다.");
            return;
        }

        Debug.Log($"[GameManager] 출구 방향 {directionIndex}번을 선택했습니다.");
        CentralTerminal.RailSpawnInfo exitInfo = _activeTerminal.GetStartPoint(directionIndex);
        Transform exitDirRoot = _activeTerminal.ExitDirRoots[directionIndex];

        Rail?.InitStartingRailPath(exitDirRoot);

        Train?.SpawnFullTrain(exitInfo.position, exitInfo.rotation, _startingCarriageCount);

        //if (_activeTerminal != null)
        //{
        //    Train?.AddVisitedStation(_activeTerminal.transform);
        //}

        _activeTerminal = null;

        ChangeGameState(GameState.ExitSelected);
        UI?.CloseBaseArrivalUI();
        Train?.DepartStation();
        StartCountdownAsync().Forget();
    }

    public void GameOver()
    {
        if (CurrentGameState == GameState.GameOver)
        {
            return;
        }

        Debug.Log("[GameManager] 게임 오버: 진행 중인 시스템을 정리합니다.");
        ChangeGameState(GameState.GameOver);
        PauseGameplayTime();

        FinalizeRunStatsAndGrantReward();
        OpenScoreReport(ScoreResultType.GameOver, ReturnToLobby);
    }

    public void GameClear()
    {
        if (CurrentGameState == GameState.GameClear)
        {
            return;
        }

        ChangeGameState(GameState.GameClear);

        GameStage clearedStage = _currentGameStage;
        float timeLimit = GetClearTimeLimit(clearedStage);
        bool isClearedInTime = _playTime <= timeLimit;
        bool isFinalStage = clearedStage >= GameStage.Stage3;

        if (isClearedInTime && !isFinalStage)
        {
            _currentGameStage = (GameStage)((int)clearedStage + 1);

            Debug.Log($"[GameManager] Stage {(int)clearedStage} 클리어 성공. 기록: {_playTime:F1}초 / 제한: {timeLimit:F1}초. 다음 스테이지: {(int)_currentGameStage}");
        }
        else if (!isClearedInTime)
        {
            Debug.Log($"[GameManager] Stage {(int)clearedStage}는 클리어했지만 제한 시간 초과: {_playTime:F1}초 / {timeLimit:F1}초. 다음 게임도 현재 스테이지입니다.");
        }
        else
        {
            Debug.Log("[GameManager] Stage 3 최종 클리어!");
        }

        OnGameCleared?.Invoke();
        FinalizeRunStatsAndGrantReward();

        if (isClearedInTime)
        {
            if (isFinalStage && Save != null)
            {
                Save.HasClearedAllStagesSpecial = true;
            }

            OpenGameClearResultUI(clearedStage, isFinalStage, ReturnToLobby);
        }
        else
        {
            OpenScoreReport(ScoreResultType.GameClear, ReturnToLobby);
        }
    }

#if UNITY_EDITOR
    public void Debug_ForceGameClearInTime()
    {
        _playTime = 0f;
        Debug.Log("[GameManager] (디버그) 제한시간 내 클리어를 강제로 트리거합니다.");
        GameClear();
    }

    public void Debug_ForceGameClearOverTime()
    {
        _playTime = GetClearTimeLimit(_currentGameStage) + 1f;
        Debug.Log("[GameManager] (디버그) 제한시간 초과 클리어를 강제로 트리거합니다.");
        GameClear();
    }
#endif

    private void FinalizeRunStatsAndGrantReward()
    {
        int rescuedHumanCount = NetworkResourceService?.GetLocalResourceViewModel().RescuedHumanCount ?? 0;
        int earnedCashCount = NetworkUpgradeService?.GrantRescueReward(rescuedHumanCount) ?? 0;

        RunStatsSnapshot snapshot = BuildRunStatsSnapshot(rescuedHumanCount, earnedCashCount);
        Save?.AddRunStatsToLifetime(snapshot);
    }

    private RunStatsSnapshot BuildRunStatsSnapshot(int rescuedHumanCount, int earnedCashCount)
    {
        RunStatsSnapshot snapshot = new RunStatsSnapshot();
        snapshot.Distance = Train != null ? Train.GetHeadTrainDistance() : 0f;
        snapshot.PlayTimeSeconds = _playTime;
        snapshot.RescuedHumanCount = rescuedHumanCount;
        snapshot.CollectedWoodCount = NetworkResourceService != null ? NetworkResourceService.SessionTotalWoodCollected : 0;
        snapshot.CollectedStoneCount = NetworkResourceService != null ? NetworkResourceService.SessionTotalStoneCollected : 0;
        snapshot.KillCount = _sessionKillCount;
        snapshot.RailCraftedCount = NetworkRailService != null ? NetworkRailService.SessionCraftedCount : 0;
        snapshot.RailInstalledCount = NetworkRailService != null ? NetworkRailService.SessionInstalledCount : 0;
        snapshot.EarnedCashCount = earnedCashCount;

        return snapshot;
    }

    private void OpenGameClearResultUI(GameStage clearedStage, bool isFinalStage, Action onConfirm)
    {
        GameClearResultData resultData = BuildGameClearResultData(clearedStage, isFinalStage);
        UI?.OpenGameClearResultUI(resultData, onConfirm);
    }

    private GameClearResultData BuildGameClearResultData(GameStage clearedStage, bool isFinalStage)
    {
        GameClearResultData resultData = new GameClearResultData();
        resultData.TotalDistance = Save != null ? Save.LifetimeTotalDistance : 0f;
        resultData.PlayTimeSeconds = Save != null ? Save.LifetimeTotalPlayTimeSeconds : 0f;
        resultData.RescuedHumanCount = Save != null ? Save.LifetimeTotalRescuedHumanCount : 0;
        resultData.CollectedWoodCount = Save != null ? Save.LifetimeTotalCollectedWood : 0;
        resultData.CollectedStoneCount = Save != null ? Save.LifetimeTotalCollectedStone : 0;
        resultData.KillCount = Save != null ? Save.LifetimeTotalKillCount : 0;
        resultData.RailCraftedCount = Save != null ? Save.LifetimeTotalRailCrafted : 0;
        resultData.RailInstalledCount = Save != null ? Save.LifetimeTotalRailInstalled : 0;
        resultData.EarnedCashCount = Save != null ? Save.LifetimeTotalEarnedCash : 0;
        resultData.TotalPlayCount = Save != null ? Save.TotalPlayCount : 0;
        resultData.TitleMessage = BuildGameClearTitleMessage(clearedStage, isFinalStage);

        GameStage nextStage = _currentGameStage;
        int nextStageTimeLimitMinutes = Mathf.RoundToInt(GetClearTimeLimit(nextStage) / 60f);
        resultData.NextStageNoticeMessage = isFinalStage
            ? string.Empty
            : $"다음 난이도는 {nextStageTimeLimitMinutes}분 안에 클리어를 노려 특수 클리어를 성공 하시기를 기원합니다.";

        resultData.IsFinalStage = isFinalStage;

        return resultData;
    }

    private string BuildGameClearTitleMessage(GameStage clearedStage, bool isFinalStage)
    {
        if (isFinalStage)
        {
            return "축하드립니다. 모든 매우 어려움 난이도 특수 클리어를 성공하셨습니다.\n이로써 모든 난이도 특수 클리어를 달성하셨습니다. 다시 한번 축하드립니다.";
        }

        GameStage nextStage = _currentGameStage;
        return $"{GetStageDisplayName(clearedStage)} 난이도 특수 클리어에 성공하셨습니다.\n자동으로 게임 시작 시 {GetStageDisplayName(nextStage)} 난이도가 시작됩니다.";
    }

    private string GetStageDisplayName(GameStage stage)
    {
        return stage switch
        {
            GameStage.Stage1 => "보통",
            GameStage.Stage2 => "어려움",
            GameStage.Stage3 => "매우 어려움",
            _ => stage.ToString()
        };
    }

    private void OpenScoreReport(ScoreResultType resultType, Action onConfirm)
    {
        int rescuedHumanCount = NetworkResourceService?.GetLocalResourceViewModel().RescuedHumanCount ?? 0;

        int currentCargo = NetworkResourceService != null ? NetworkResourceService.CurrentCargoLoad : 0;
        int warehouseStored = NetworkWarehouseService != null ? NetworkWarehouseService.TotalStoredResource : 0;
        int collectedResourceCount = currentCargo + warehouseStored;

        float totalDistance = Train != null ? Train.GetHeadTrainDistance() : 0f;

        UI?.OpenScoreUI(totalDistance, rescuedHumanCount, collectedResourceCount, _sessionKillCount, resultType, onConfirm);
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
        // TODO: Station UI를 열고 CompleteStation(stoneTaken, citizenBoarded)을 호출하도록 연결해야 한다.
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
        SetManagerParent(Drone);
        SetManagerParent(Save);
        SetManagerParent(Weapon);
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
        if (manager == null)
        {
            return;
        }

        if (_managerRoot == null)
        {
            InitManagerRoot();
        }

        SetManagerParent(manager);
    }
    private async UniTaskVoid InitializeGameFlowAsync()
    {
        if (!await WaitForRequiredManagersAsync())
        {
            return;
        }

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
        await UniTask.WaitUntil(() => UI != null && Data != null && Resource != null && Map != null && Train != null, cancellationToken: this.GetCancellationTokenOnDestroy());

        return ValidateStartDependencies();
    }

    private bool ValidateStartDependencies()
    {
        bool isValid = true;
        isValid &= ValidateManager(UI, nameof(UIManager));
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
        HandleTerminalArrivalAsync(terminal).Forget();
    }

    private void HandleStationArrival(StationObject station, string stationId)
    {
        if (CurrentGameState != GameState.Playing) return;

        StationArrivalUI stationUI = UI?.OpenStationArrivalUI();
        if (stationUI != null)
        {
            stationUI.Init(station, station.AvailableStone, station.AvailableCitizen);
        }

        _activeStation = station;
        PauseGameplayTime();
        StopAndDespawnMonsters();
        ChangeGameState(GameState.EventPaused);

        Debug.Log($"[GameManager] 역 도착: '{stationId}' 이벤트 처리를 기다립니다.");
        // TODO: Station UI를 열고 CompleteStation(bool)을 호출하도록 연결필요
    }

    private async UniTaskVoid HandleTerminalArrivalAsync(CentralTerminal terminal)
    {
        if (CurrentGameState != GameState.Playing)
        {
            return;
        }

        if (terminal != null && Train != null && Train.IsVisitedStation(terminal.transform))
        {
            return;
        }

        _activeTerminal = terminal;
        PauseGameplayTime();
        StopAndDespawnMonsters();
        RemovePlayerPlacedRails();

        RemoveCompletedStations();
        NetworkTrainCargeService?.UnloadAllCitizens();

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, this.GetCancellationTokenOnDestroy());

        Physics.SyncTransforms();
        Map?.RefreshAllTileOccupancies();
        Rail?.SyncGhostRailData();

        Debug.Log($"[GameManager] 스테이션 순회: {CompletedStationCount}/{RequiredStationCount}");

        if (CompletedStationCount >= RequiredStationCount)
        {
            GameClear();
            return;
        }

        ChangeGameState(GameState.EventPaused);

        UI?.OpenBaseArrivalUI();
        OpenScoreReport(ScoreResultType.BaseArrival, null);
        Debug.Log("[GameManager] 터미널 도착: 출구 방향 선택을 기다립니다.");
    }

    private void RemoveCompletedStations()
    {
        foreach (StationObject station in _completedStations)
        {
            if (station != null)
            {
                station.RemoveStationAndRails();
            }
        }
    }

    public void PauseGameplayTime()
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

    public void ResumeGameplayTime()
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

    public async UniTask StartCountdownAsync(Action onComplete = null)
    {
        if (_isCountdownRunning)
        {
            return;
        }

        _isCountdownRunning = true;
        int sessionVersion = _sessionVersion;

        try
        {
            for (int remaining = _resumeCountdownSeconds; remaining > 0; remaining--)
            {
                OnCountdownChanged?.Invoke(remaining);

                Debug.Log($"[GameManager] {remaining}초 후 다음 구간을 시작합니다.");
                await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: this.GetCancellationTokenOnDestroy());

                if (sessionVersion != _sessionVersion) return;
                GameStartCountdownPopup countdownPopup = UI?.OpenGameStartCountdownPopup();
                countdownPopup?.Init(onComplete);
            }

            OnCountdownChanged?.Invoke(0);
            ChangeGameState(GameState.Playing);

            ResumeMonsterSpawning();
            ResumeGameplayTime();
        }
        finally
        {
            _isCountdownRunning = false;
        }
    }

    private void ResumeMonsterSpawning()
    {
        if (Monster != null && CurrentGameState == GameState.Playing)
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

        Debug.Log("[GameManager] 몬스터스폰 정지");
        // TODO: Weapon 투사체 정리
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

    private void HandleMonsterDied(int dropStone)
    {
        if (CurrentGameState != GameState.Playing)
        {
            return;
        }

        _sessionKillCount++;
        _sessionEarnedStone += dropStone;

        NetworkResourceService?.AddStone(dropStone);

        Debug.Log($"몬스터 처치 현재 킬: {_sessionKillCount} / 누적 돌: {_sessionEarnedStone} (+{dropStone})");
    }

    private void ResetSessionState()
    {
        _activeStation = null;
        _activeTerminal = null;
        _playTime = 0f;
        _lastNotifiedTime = 0;
        _sessionKillCount = 0;
        _sessionEarnedStone = 0;
        _completedStations.Clear();
    }

    private void ClearCurrentSession()
    {
        _sessionVersion++;
        Drone?.DespawnAll();
        StopAndDespawnMonsters();
        RemovePlayerPlacedRails();
        Train?.ClearExistingTrain();
        Map?.ClearMap();
        ResetSessionState();
        Monster?.ResetGamePhase();
    }

    public void ReturnToLobby()
    {
        ClearCurrentSession();
        ResumeGameplayTime();
        ChangeGameState(GameState.Ready);

        NetworkResourceService.ResetRun();
        NetworkWarehouseService.ResetRun();
        NetworkRailService.ResetRun();
        NetworkTrainStrengtheningService.ResetRun();
        NetworkTrainCargeService.ResetRun();
        NetworkAugmentService.ResetRun();

        UI?.CloseHudTrainStatusUI();
        UI?.CloseHudResourceUI();
        UI?.CloseHudMinimapUI();
        UI?.CloseInGameMenuButtonUI();
        UI?.CloseRailBuildUI();
        UI?.CloseStationArrivalUI();
        UI?.CloseBaseArrivalUI();
        UI?.OpenContentUI(UIType.LobbyUI);
    }

    private int GetMapSize(GameStage stage)
    {
        return stage switch
        {
            GameStage.Stage1 => 3,
            GameStage.Stage2 => 5,
            GameStage.Stage3 => 7,
            _ => 3
        };
    }

    private float GetClearTimeLimit(GameStage stage)
    {
        return stage switch
        {
            GameStage.Stage1 => _stage1ClearTimeLimit,
            GameStage.Stage2 => _stage2ClearTimeLimit,
            GameStage.Stage3 => _stage3ClearTimeLimit,
            _ => 0f
        };
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
        if (manager != null)
        {
            return true;
        }

        Debug.LogError($"[GameManager] 필수 매니저 '{managerName}'를 찾지 못했습니다.");
        return false;
    }

    private void ChangeGameState(GameState newState)
    {
        _currentGameState = newState;
        Debug.Log($"[GameManager] 게임 상태 변경: {CurrentGameState}");

        OnGameStateChanged?.Invoke(newState);
    }
}

