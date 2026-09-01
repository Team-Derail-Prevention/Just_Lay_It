using UnityEngine;
using UnityEngine.UI;

public class HudViewControlUI : UIBase
{
    [Header("프리셋 이미지")]
    [SerializeField] private Image Image_Preset;
    [SerializeField] private Sprite[] _presetSprites;

    private void OnEnable()
    {
        if (Image_Preset == null)
        {
            Debug.LogError("[HudViewControlUI] Image_Preset이 null입니다. 인스펙터에서 연결했는지 확인하세요.");
        }

        if (_presetSprites == null || _presetSprites.Length == 0)
        {
            Debug.LogWarning("[HudViewControlUI] _presetSprites가 비어 있습니다. 인스펙터에서 스프라이트를 등록했는지 확인하세요.");
        }

        FollowCamera.OnPresetIndex += SetPresetImage;
    }

    private void OnDisable()
    {
        FollowCamera.OnPresetIndex -= SetPresetImage;
    }

    private void SetPresetImage(int presetIndex)
    {
        if (Image_Preset == null || _presetSprites == null || _presetSprites.Length == 0)
        {
            return;
        }

        if (presetIndex < 0 || presetIndex >= _presetSprites.Length)
        {
            Debug.LogWarning($"[HudViewControlUI] presetIndex({presetIndex})가 _presetSprites 범위를 벗어났습니다.");
            return;
        }

        Image_Preset.sprite = _presetSprites[presetIndex];
    }
}
