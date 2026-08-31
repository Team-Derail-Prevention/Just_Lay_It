using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class LoadingUI : UIBase
{
    [Header("로딩바 컴포넌트")]
    [SerializeField] private Slider Slider_LoadingBar;
    [SerializeField] private TextMeshProUGUI Text_LoadingLabel;

    [Header("로딩 설정")]
    [SerializeField] private float _loadingBarTimer = 2.0f;
    [SerializeField] private string _loadingMessage = "기지를 향해 나아가는 중";

    private CancellationTokenSource _cts;
    private bool _isDataLoaded;
    private int _lastDisplayedPercent = -1;

    private void OnEnable()
    {
        if (Slider_LoadingBar != null)
        {
            Slider_LoadingBar.value = 0f;
            Slider_LoadingBar.interactable = false;
        }

        _isDataLoaded = false;
        _lastDisplayedPercent = -1;
        _cts = new CancellationTokenSource();

        FillBarRoutineAsync(_cts.Token).Forget();
    }

    private void OnDisable()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private async UniTaskVoid FillBarRoutineAsync(CancellationToken token)
    {
        float timer = 0f;

        // 로딩바 연출
        while (timer < _loadingBarTimer)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Lerp(0f, 0.9f, timer / _loadingBarTimer);
            SetProgressUI(progress);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 실제 로딩 끝날때까지 대기
        while (_isDataLoaded == false)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 로딩 완료시에만
        SetProgressUI(1f);

        UIManager.Instance.CloseLoadingUI();
        GameManager.Instance.StartCountdownAsync(OpenInGameHudForTest).Forget();
    }

    private void OpenInGameHudForTest()
    {
        UIManager.Instance.OpenRailBuildUI();
        UIManager.Instance.OpenHudTrainStatusUI();
        UIManager.Instance.OpenHudResourceUI();
        UIManager.Instance.OpenHudMinimapUI();
        UIManager.Instance.OpenHudViewControlUI();
        UIManager.Instance.OpenInGameMenuButtonUI();
    }

    private void SetProgressUI(float progress01)
    {
        if (Slider_LoadingBar != null)
        {
            Slider_LoadingBar.value = progress01;
        }

        if (Text_LoadingLabel == null)
        {
            return;
        }

        int percent = Mathf.RoundToInt(progress01 * 100f);
        if (percent == _lastDisplayedPercent)
        {
            return;
        }

        _lastDisplayedPercent = percent;
        Text_LoadingLabel.text = $"{_loadingMessage} {percent}%";
    }

    // 실제 맵 로딩 관련 추후 수정
    public void SetDataLoaded()
    {
        _isDataLoaded = true;
    }
}
