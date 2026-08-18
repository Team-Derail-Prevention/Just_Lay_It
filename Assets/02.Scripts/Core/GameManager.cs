using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{
    public static DataManager Data { get { return DataManager.Instance; } }
    public static ResourceManager Resource { get { return ResourceManager.Instance; } }
    public static PoolManager Pool { get { return PoolManager.Instance; } }
    public static MapManager Map { get { return MapManager.Instance; } }
    public static TrainManager Train { get { return TrainManager.Instance; } }
    public static UIManager UI { get { return UIManager.Instance; } }
    public static TrainStatusEventHub TrainEventHub { get { return TrainStatusEventHub.Instance; } }
    public static ResourceStatusEventHub ResourceEventHub { get { return ResourceStatusEventHub.Instance; } }
    public static NetworkRailService NetworkRail { get { return NetworkRailService.Instance; } }
    public static NetworkUpgradeService UpgradeService { get { return NetworkUpgradeService.Instance; } }

    private TimeManager _timeManager = new TimeManager();
    public static TimeManager Time
    {
        get
        {
            if (Instance != null) return Instance._timeManager;
            return null;
        }
    }

    [SerializeField] private GameState _currentGameState = GameState.Ready;
    public GameState CurrentGameState { get { return _currentGameState; } }

    private Transform _managerRoot;

    private float _playTime = 0f;
    private int _lastNotifiedTime = 0;

    protected override void Init()
    {
        base.Init();
        if (Instance != this) return;

        InitManagerRoot();
        OrganizeExistingManagers();
        InitializeGameFlowAsync().Forget();
    }

    private void Update()
    {
        if (_currentGameState == GameState.Playing)
        {
            _playTime += UnityEngine.Time.deltaTime;

            int currentSecond = (int)_playTime;

            if (currentSecond != _lastNotifiedTime)
            {
                _lastNotifiedTime = currentSecond;
                TrainStatusEventHub.Instance.NotifyPlayTimeChanged(_lastNotifiedTime);
            }
        }
    }

    private void InitManagerRoot()
    {
        GameObject rootObject = GameObject.Find("@Managers");
        if (rootObject == null) rootObject = new GameObject("@Managers");

        _managerRoot = rootObject.transform;
        if (transform.parent != _managerRoot) rootObject.transform.SetParent(transform);
    }

    private void OrganizeExistingManagers()
    {
        if (UI != null && UI.transform.parent != _managerRoot) UI.transform.SetParent(_managerRoot);
        if (Resource != null && Resource.transform.parent != _managerRoot) Resource.transform.SetParent(_managerRoot);
        if (Data != null && Data.transform.parent != _managerRoot) Data.transform.SetParent(_managerRoot);
        if (Map != null && Map.transform.parent != _managerRoot) Map.transform.SetParent(_managerRoot);
        if (Pool != null && Pool.transform.parent != _managerRoot) Pool.transform.SetParent(_managerRoot);
        if (Train != null && Train.transform.parent != _managerRoot) Train.transform.SetParent(_managerRoot);
        if (TrainEventHub != null && TrainEventHub.transform.parent != _managerRoot) TrainEventHub.transform.SetParent(_managerRoot);
        if (ResourceEventHub != null && ResourceEventHub.transform.parent != _managerRoot) ResourceEventHub.transform.SetParent(_managerRoot);
        if (NetworkRail != null && NetworkRail.transform.parent != _managerRoot) NetworkRail.transform.SetParent(_managerRoot);
        if (UpgradeService != null && UpgradeService.transform.parent != _managerRoot) UpgradeService.transform.SetParent(_managerRoot);
    }

    private async UniTaskVoid InitializeGameFlowAsync()
    {
        await UniTask.WaitUntil(() => ResourceManager.Instance != null && DataManager.Instance != null);

        if (!DataManager.Instance.IsLoaded)
        {
            Debug.Log("[GameManager] 데이터 로드를 시작합니다...");
            await DataManager.Instance.LoadAllDatasAsync(this.GetCancellationTokenOnDestroy());
        }

        ChangeGameState(GameState.Ready);
        Debug.Log("[GameManager] 초기화 및 데이터 로드 완료.");
    }

    public async UniTask StartGame()
    {
        Debug.Log("[GameManager] 게임 시작! 맵 생성을 요청합니다.");
        ChangeGameState(GameState.Playing);

        if (Map != null)
        {
            bool isMapGenerated = await Map.GenerateMapAsync(this.GetCancellationTokenOnDestroy());

            if (!isMapGenerated)
            {
                Debug.LogError("[GameManager] 맵 생성에 실패하여 게임을 중단합니다. 기차를 스폰하지 않습니다.");
                ChangeGameState(GameState.Ready);
                return; 
            }

            Debug.Log("[GameManager] 맵 생성 완료!");
        }

        if (Train != null)
        {
            Debug.Log("[GameManager] 기차를 스폰합니다.");
            TrainManager.Instance.SpawnFullTrain(3);
        }
        else
        {
            Debug.LogError("[GameManager] TrainManager 인스턴스를 찾을 수 없어 기차를 스폰할 수 없습니다.");
        }

        if (Pool != null)
        {
            //
        }

        if (Time != null)
        {
            Time.Resume();
        }

        //if (Rail)
        {

        }
    }

    public void GameOver()
    {
        Debug.Log("[GameManager] 게임 오버! 맵과 오브젝트를 정리합니다.");

        ChangeGameState(GameState.GameOver);

        if (Time != null)
        {
            Time.Resume();
        }

        if (Map != null)
        {
            Map.ClearMap();
        }

        if (Pool != null)
        {
            Pool.AllDespawnToPool();
        }

        // TODO: UI 매니저를 통해 '로비 화면' 또는 '결과 화면' 띄우기
    }

    public void HandleTerminalArrival()
    {
        if (_currentGameState == GameState.EventPaused)
        {
            return;
        }

        Debug.Log("[GameManager] 종착역 도달: 게임을 일시정지하고 종착역 UI 페이즈로 전환합니다.");
        ChangeGameState(GameState.EventPaused);

        if (Time != null)
        {
            Time.Pause();
        }
    }

    public void HandleStationArrival()
    {
        if (_currentGameState == GameState.EventPaused)
        {
            return;
        }

        Debug.Log("[GameManager] 일반 기차역 도달: 게임을 일시정지하고 기차역 UI 페이즈로 전환합니다.");
        ChangeGameState(GameState.EventPaused);

        if (Time != null)
        {
            Time.Pause();
        }
    }

    public void SelectExitDirection(int directionIndex)
    {
        Debug.Log($"[GameManager] 출구 방향({directionIndex}번) 선택됨: 게임을 재개합니다.");

        if (Time != null)
        {
            Time.Resume();
        }

        ChangeGameState(GameState.ExitSelected);
        ChangeGameState(GameState.Playing);

        // TODO: 관련Manager에 지시하여 기차 출발 로직 실행
    }

    private void ChangeGameState(GameState newState)
    {
        _currentGameState = newState;
        Debug.Log($"[GameManager] 게임 상태 변경: {_currentGameState}");
    }
}