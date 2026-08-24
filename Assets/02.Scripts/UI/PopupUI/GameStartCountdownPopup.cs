using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using TMPro;

public class GameStartCountdownPopup : UIBase
{
    [SerializeField] private TextMeshProUGUI Text_Countdown;

    private Action _onComplete;

    public void Init(Action onComplete)
    {
        _onComplete = onComplete;

        GameManager.Instance.OnCountdownChanged += UpdateCountdownText;
        GameManager.Instance.StartCountdownAsync().Forget();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownChanged -= UpdateCountdownText;
        }

    }
    private void UpdateCountdownText(int remainingSeconds)
    {
        Debug.Log($"[Popup] Received: {remainingSeconds}, TextRef null? {Text_Countdown == null}");

        if (remainingSeconds > 0)
        {
            if (Text_Countdown != null)
            {
                Text_Countdown.text = $"'{remainingSeconds}' 초 후 열차가 출발 합니다.";
            }
        }
        else
        {
            GameManager.Instance.OnCountdownChanged -= UpdateCountdownText;
            UIManager.Instance.CloseGameStartCountdownPopup();

            _onComplete?.Invoke();
        }
    }
}
