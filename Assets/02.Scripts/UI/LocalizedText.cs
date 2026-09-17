using Enums;
using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    [Header("로컬라이즈 설정")]
    [SerializeField] private string _localizationId;

    [Header("컴포넌트 연결")]
    [SerializeField] private TMP_Text _targetText;

    private void Reset()
    {
        _targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        LocalizationEventHub.Instance.OnLanguageChanged += OnLanguageChanged_LocalizationEventHub;

        if (LocalizationManager.Instance.IsReady)
        {
            ApplyLocalization();
        }
        else
        {
            LocalizationManager.Instance.OnLocalizationReady += OnLocalizationReady_LocalizationManager;
        }
    }

    private void OnDisable()
    {
        if (LocalizationEventHub.Instance != null)
        {
            LocalizationEventHub.Instance.OnLanguageChanged -= OnLanguageChanged_LocalizationEventHub;
        }

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLocalizationReady -= OnLocalizationReady_LocalizationManager;
        }
    }

    private void OnLanguageChanged_LocalizationEventHub(LanguageType language)
    {
        ApplyLocalization();
    }

    private void OnLocalizationReady_LocalizationManager()
    {
        LocalizationManager.Instance.OnLocalizationReady -= OnLocalizationReady_LocalizationManager;
        ApplyLocalization();
    }

    public void ApplyLocalization()
    {
        if (_targetText == null || string.IsNullOrWhiteSpace(_localizationId))
        {
            Debug.LogWarning($"[LocalizedText] {gameObject.name}에 텍스트 컴포넌트 또는 Id가 설정되지 않았습니다.");
            return;
        }

        _targetText.text = LocalizationManager.Instance.GetText(_localizationId);
        _targetText.fontSize = LocalizationManager.Instance.GetFontSize(_localizationId);
    }
}
