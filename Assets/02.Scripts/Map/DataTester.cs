using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class DataTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool _autoRunOnStart = true;

    private async void Start()
    {
        if (_autoRunOnStart)
        {
            await RunTestSequenceAsync(this.GetCancellationTokenOnDestroy());
        }
    }

    private async UniTask RunTestSequenceAsync(CancellationToken cancellationToken)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[MapTestManager] 씬에 DataManager 인스턴스가 존재하지 않습니다!");
            return;
        }

        if (!DataManager.Instance.IsLoaded)
        {
            Debug.Log("[MapTestManager] DataManager 데이터 로드 시작...");
            await DataManager.Instance.LoadAllDatasAsync(cancellationToken);
        }
        else
        {
            Debug.Log("[MapTestManager] DataManager가 이미 로드되어 있습니다.");
        }
    }
}