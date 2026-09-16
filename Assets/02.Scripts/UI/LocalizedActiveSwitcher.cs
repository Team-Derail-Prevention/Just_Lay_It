using UnityEngine;
using Enums;

public class LocalizedActiveSwitcher : MonoBehaviour
{
    [Header("언어별 오브젝트 연결")]
    [SerializeField] private GameObject _koreanObject;
    [SerializeField] private GameObject _englishObject;

    private void OnEnable()
    {
        LocalizationEventHub.Instance.OnLanguageChanged += OnLanguageChanged_LocalizationEventHub;
        ApplyActiveState(LocalizationManager.Instance.CurrentLanguage);
    }

    private void OnDisable()
    {
        if (LocalizationEventHub.Instance != null)
        {
            LocalizationEventHub.Instance.OnLanguageChanged -= OnLanguageChanged_LocalizationEventHub;
        }
    }

    private void OnLanguageChanged_LocalizationEventHub(LanguageType language)
    {
        ApplyActiveState(language);
    }

    private void ApplyActiveState(LanguageType language)
    {
        bool isKorean = language == LanguageType.Korean;

        _koreanObject.SetActive(isKorean);
        _englishObject.SetActive(isKorean == false);
    }
}
