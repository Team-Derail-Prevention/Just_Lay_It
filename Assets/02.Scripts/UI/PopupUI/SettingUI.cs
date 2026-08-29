using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SettingUI : UIBase
{
    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Slider _sliderBgmVolume;
    [SerializeField] private Slider _sliderSfxVolume;
    [SerializeField] private TMP_Dropdown _dropdownDisplayMode;

    [Header("버튼 연결")]
    [SerializeField] private UIButton _btnReset;
    [SerializeField] private UIButton _btnSave;
    [SerializeField] private UIButton _btnBack;

    private float _savedBgmVolume;
    private float _savedSfxVolume;
    private int _savedDisplayMode;

    private void OnEnable()
    {
        LoadSavedSettings();

        _sliderBgmVolume.onValueChanged.AddListener(OnBgmVolumeChanged);
        _sliderSfxVolume.onValueChanged.AddListener(OnSfxVolumeChanged);
        _dropdownDisplayMode.onValueChanged.AddListener(OnDisplayModeChanged);

        _btnReset.BindOnClickButtonEvent(OnClick_Reset);
        _btnSave.BindOnClickButtonEvent(OnClick_Save);
        _btnBack.BindOnClickButtonEvent(OnClick_Back);
    }

    private void OnDisable()
    {
        _sliderBgmVolume.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        _sliderSfxVolume.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        _dropdownDisplayMode.onValueChanged.RemoveListener(OnDisplayModeChanged);

        _btnReset.UnBindOnClickButtonEvent(OnClick_Reset);
        _btnSave.UnBindOnClickButtonEvent(OnClick_Save);
        _btnBack.UnBindOnClickButtonEvent(OnClick_Back);
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

        _sliderBgmVolume.value = _savedBgmVolume;
        _sliderSfxVolume.value = _savedSfxVolume;
        _dropdownDisplayMode.value = _savedDisplayMode;

        ApplyBgmVolume(_savedBgmVolume);
        ApplySfxVolume(_savedSfxVolume);
        ApplyDisplayMode(_savedDisplayMode);
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
        switch (index)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Cursor.lockState = CursorLockMode.Confined;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Cursor.lockState = CursorLockMode.None;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Cursor.lockState = CursorLockMode.Confined;
                break;
        }
    }

    private void OnClick_Reset()
    {
        float defaultBgmVolume = 1f;
        float defaultSfxVolume = 1f;
        int defaultDisplayMode = 1;

        _sliderBgmVolume.value = defaultBgmVolume;
        _sliderSfxVolume.value = defaultSfxVolume;
        _dropdownDisplayMode.value = defaultDisplayMode;

        ApplyBgmVolume(defaultBgmVolume);
        ApplySfxVolume(defaultSfxVolume);
        ApplyDisplayMode(defaultDisplayMode);
    }

    private void OnClick_Save()
    {
        _savedBgmVolume = _sliderBgmVolume.value;
        _savedSfxVolume = _sliderSfxVolume.value;
        _savedDisplayMode = _dropdownDisplayMode.value;

        SaveManager.Instance.BgmVolume = _savedBgmVolume;
        SaveManager.Instance.SfxVolume = _savedSfxVolume;
        SaveManager.Instance.DisplayMode = _savedDisplayMode;

        Debug.Log("환경설정 저장 완료!");
    }

    private void OnClick_Back()
    {
        ApplyBgmVolume(_savedBgmVolume);
        ApplySfxVolume(_savedSfxVolume);
        ApplyDisplayMode(_savedDisplayMode);

        UIManager.Instance.CloseSettingUI();
    }
}
