using UnityEngine;
using System;
using TMPro;

public class GameStartCountdownPopup : UIBase
{
    [SerializeField] private TextMeshProUGUI Text_Countdown;

    private Action _onComplete;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownChanged += UpdateCountdownText;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownChanged -= UpdateCountdownText;
        }

        _onComplete = null;
    }

    public void Init(Action onComplete = null)
    {
        _onComplete = onComplete;
    }

    private void UpdateCountdownText(int remainingSeconds)
    {
        if (remainingSeconds > 0)
        {
            if (Text_Countdown != null)
            {
                Text_Countdown.text = string.Format(LocalizationManager.Instance.GetText("GameStartCountdown_PopUp_UI_01"), remainingSeconds);
            }
        }
        else
        {
            Action onComplete = _onComplete;

            UIManager.Instance.CloseGameStartCountdownPopup();

            onComplete?.Invoke();
        }
    }
}
