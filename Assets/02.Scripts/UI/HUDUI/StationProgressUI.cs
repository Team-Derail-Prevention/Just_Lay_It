using UnityEngine;
using UnityEngine.UI;

public class StationProgressUI : UIBase
{
    [Header("정거장 점령 체크 아이콘")]
    [SerializeField] private GameObject[] GameObject_LevelChecks;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStationProgressChanged += RefreshProgress;
            RefreshProgress(GameManager.Instance.CompletedStationCount);
        }
        else
        {
            Debug.LogError("[StationProgressUI] GameManager.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStationProgressChanged -= RefreshProgress;
        }
    }

    private void RefreshProgress(int completedCount)
    {
        for (int i = 0; i < GameObject_LevelChecks.Length; i++)
        {
            if (GameObject_LevelChecks[i] == null)
            {
                continue;
            }

            bool isAchieved = i < completedCount;
            GameObject_LevelChecks[i].SetActive(isAchieved);
        }
    }
}
