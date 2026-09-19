using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Enums;
using System.Collections.Generic;

public class SettingUI : UIBase
{
    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Slider _sliderBgmVolume;
    [SerializeField] private Slider _sliderSfxVolume;
    [SerializeField] private TMP_Dropdown _dropdownDisplayMode;
    [SerializeField] private TMP_Dropdown _dropdownLanguage;

    [Header("버튼 연결")]
    [SerializeField] private UIButton _btnReset;
    [SerializeField] private UIButton _btnSave;
    [SerializeField] private UIButton _btnBack;

    private float _savedBgmVolume;
    private float _savedSfxVolume;
    private int _savedDisplayMode;

    private void OnEnable()
    {
        RefreshDisplayModeOptions();
        LoadSavedSettings();

        _sliderBgmVolume.onValueChanged.AddListener(OnBgmVolumeChanged);
        _sliderSfxVolume.onValueChanged.AddListener(OnSfxVolumeChanged);
        _dropdownDisplayMode.onValueChanged.AddListener(OnDisplayModeChanged);
        _dropdownLanguage.onValueChanged.AddListener(OnLanguageChanged);

        _btnReset.BindOnClickButtonEvent(OnClick_Reset);
        _btnSave.BindOnClickButtonEvent(OnClick_Save);
        _btnBack.BindOnClickButtonEvent(OnClick_Back);

        LocalizationEventHub.Instance.OnLanguageChanged += OnLanguageChanged_LocalizationEventHub;
    }

    private void OnDisable()
    {
        _sliderBgmVolume.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        _sliderSfxVolume.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        _dropdownDisplayMode.onValueChanged.RemoveListener(OnDisplayModeChanged);
        _dropdownLanguage.onValueChanged.RemoveListener(OnLanguageChanged);

        _btnReset.UnBindOnClickButtonEvent(OnClick_Reset);
        _btnSave.UnBindOnClickButtonEvent(OnClick_Save);
        _btnBack.UnBindOnClickButtonEvent(OnClick_Back);

        if (LocalizationEventHub.Instance != null)
        {
            LocalizationEventHub.Instance.OnLanguageChanged -= OnLanguageChanged_LocalizationEventHub;
        }
    }

    private void OnLanguageChanged_LocalizationEventHub(LanguageType language)
    {
        RefreshDisplayModeOptions();
    }

    private void RefreshDisplayModeOptions()
    {
        int currentValue = _dropdownDisplayMode.value;

        _dropdownDisplayMode.ClearOptions();

        List<string> options = new List<string>
        {
            LocalizationManager.Instance.GetText("Setting_PopUp_UI_TR_03"),
            LocalizationManager.Instance.GetText("Setting_PopUp_UI_TR_04"),
            LocalizationManager.Instance.GetText("Setting_PopUp_UI_TR_05")
        };

        _dropdownDisplayMode.AddOptions(options);
        _dropdownDisplayMode.SetValueWithoutNotify(currentValue);
        _dropdownDisplayMode.RefreshShownValue();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame == true)
        {
            OnClick_Back();
        }
    }

    private void LoadSavedSettings()
    {
        _savedBgmVolume = SaveManager.Instance.BgmVolume;
        _savedSfxVolume = SaveManager.Instance.SfxVolume;
        _savedDisplayMode = SaveManager.Instance.DisplayMode;

        int savedLanguage = (int)SaveManager.Instance.Language;

        _sliderBgmVolume.value = _savedBgmVolume;
        _sliderSfxVolume.value = _savedSfxVolume;
        _dropdownDisplayMode.value = _savedDisplayMode;
        _dropdownLanguage.value = savedLanguage;

        ApplyBgmVolume(_savedBgmVolume);
        ApplySfxVolume(_savedSfxVolume);
        ApplyDisplayMode(_savedDisplayMode);
        ApplyLanguage(savedLanguage);
    }

    private void OnBgmVolumeChanged(float value)
    {
        ApplyBgmVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        ApplySfxVolume(value);
    }

    private void OnDisplayModeChanged(int index)
    {
        ApplyDisplayMode(index);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus == false)
        {
            return;
        }

        ApplyDisplayMode(_dropdownDisplayMode.value);
    }

    private void ApplyBgmVolume(float value)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.SetBgmVolume(value);
    }

    private void ApplySfxVolume(float value)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.SetSfxVolume(value);
    }

    private void ApplyDisplayMode(int index)
    {
        DisplayModeController.Apply(index);
    }

    private void OnLanguageChanged(int index)
    {
        ApplyLanguage(index);
    }

    private void ApplyLanguage(int index)
    {
        LocalizationManager.Instance.PreviewLanguage((LanguageType)index);
    }

    private void OnClick_Reset()
    {
        SettingsSaveData defaults = new SettingsSaveData();

        _sliderBgmVolume.value = defaults.BgmVolume;
        _sliderSfxVolume.value = defaults.SfxVolume;
        _dropdownDisplayMode.value = defaults.DisplayMode;
        _dropdownLanguage.value = defaults.Language;

        ApplyBgmVolume(defaults.BgmVolume);
        ApplySfxVolume(defaults.SfxVolume);
        ApplyDisplayMode(defaults.DisplayMode);
        ApplyLanguage(defaults.Language);
    }

    private void OnClick_Save()
    {
        _savedBgmVolume = _sliderBgmVolume.value;
        _savedSfxVolume = _sliderSfxVolume.value;
        _savedDisplayMode = _dropdownDisplayMode.value;

        SaveManager.Instance.BgmVolume = _savedBgmVolume;
        SaveManager.Instance.SfxVolume = _savedSfxVolume;
        SaveManager.Instance.DisplayMode = _savedDisplayMode;

        LocalizationManager.Instance.CommitLanguage();

        Debug.Log("환경설정 저장 완료!");
    }

    private void OnClick_Back()
    {
        ApplyBgmVolume(_savedBgmVolume);
        ApplySfxVolume(_savedSfxVolume);
        ApplyDisplayMode(_savedDisplayMode);
        LocalizationManager.Instance.RevertLanguage();

        UIManager.Instance.CloseSettingUI();
    }
}
