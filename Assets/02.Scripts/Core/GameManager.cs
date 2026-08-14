using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{

    public static ResourceManager Resource { get { return ResourceManager.Instance; } }
    public static DataManager Data { get { return DataManager.Instance; } }
    public static PoolManager Pool { get { return PoolManager.Instance; } }

    //public static TimeManager Time { get { return Instance._timeManager; } }


    // TODO: FlowManager가 생기면 여기에 한 줄 추가, 아래 게임 흐름을 가지고 갈 예정

    protected override void Init()
    {
        base.Init();

        if (Instance != this)
        {
            return;
        }

        if (DataManager.Instance != null && DataManager.Instance.IsLoaded)
        {
            return;
        }


        if (ResourceManager.Instance != null/*&& ResourceManager.Instance.IsLoaded*/)
        {
            return;
        }

        if (PoolManager.Instance != null /*&& PoolManager.Instance.IsLoaded*/)
        {
            return;
        }


        InitPool();

        LoadDataAsync().Forget();
    }

    private void InitPool()
    {
        GameObject poolRoot = new GameObject("@PoolRoot");
        poolRoot.transform.SetParent(transform);

        PoolManager.Instance.Init(poolRoot.transform, new Dictionary<string, int>());
    }

    private async UniTaskVoid LoadDataAsync()
    {
        await DataManager.Instance.LoadAllDatasAsync(this.GetCancellationTokenOnDestroy());
    }

    // TODO: 풀 미리 생성. 어떤 풀을 몇 개 만들지 정해지면 위 Init에 넘겨주세요.

    #region 게임 흐름

    // FlowManager가 생기면 각 메서드 _flowManager.XXX()

    public GameState State { get { return _state; } }

    public event Action<GameState> OnStateChanged;

    private GameState _state = GameState.Ready;

    public void StartGame()
    {
        ChangeState(GameState.Playing);
    }

    public void ArriveStation()
    {
        //GameManager.Time.Pause();

        // TODO: 정거장에서 뭘 할지(정산, 보급, 업그레이드) 정해지면 여기에.
    }

    public void EnterNextSection(int exitDirection)
    {
        ReleaseRun();

        // TODO: exitDirection으로 다음 맵을 만들고 기차를 옮기는 처리 추가.
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);

        // TODO: 게임오버 조건이 정해지면 부르는 쪽을 연결.
    }

    public void ReleaseRun()
    {
        GameManager.Pool.AllDespawnToPool();

        ChangeState(GameState.Ready);
    }

    private void ChangeState(GameState next)
    {
        if (_state == next)
        {
            return;
        }

        _state = next;
        OnStateChanged?.Invoke(_state);
    }

    #endregion
}