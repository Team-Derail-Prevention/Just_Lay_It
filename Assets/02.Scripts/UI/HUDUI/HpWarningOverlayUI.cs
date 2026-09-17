using UnityEngine;
using UnityEngine.UI;

public class HpWarningOverlayUI : UIBase
{
    [Header("비네트 이미지")]
    [SerializeField] private Image Image_Vignette;

    [Header("체력 임계값")]
    [SerializeField, Range(0f, 1f)] private float _hpRatioStart = 0.5f;

    [Header("경고 색상")]
    [SerializeField] private Color _warningColor = Color.red;

    [Header("최대 강도")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.6f;

    [Header("점멸 효과")]
    [SerializeField] private bool _isPulseEnabled = true;
    [SerializeField, Range(0f, 1f)] private float _pulseHpRatioThreshold = 0.15f;
    [SerializeField] private float _pulseSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float _pulseAmplitude = 0.25f;

    [Header("비네트 텍스처 생성 설정")]
    [SerializeField] private int _vignetteTextureSize = 256;
    [SerializeField, Range(0f, 1f)] private float _vignetteInnerRadius = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _vignetteOuterRadius = 0.75f;

    private float _baseAlpha = 0f;
    private bool _isPulseActive = false;

    private void Awake()
    {
        CreateVignetteSprite();
    }

    private void OnEnable()
    {
        if (TrainStatusEventHub.Instance == null)
        {
            Debug.LogError("[HpWarningOverlayUI] TrainStatusEventHub.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
            return;
        }

        TrainStatusEventHub.Instance.OnHpChanged += SetHp;

        if (TrainManager.Instance != null && TrainManager.Instance.ActiveTrain != null)
        {
            Train activeTrain = TrainManager.Instance.ActiveTrain;
            SetHp(activeTrain.CurrentHp, activeTrain.MaxHp);
        }

        transform.SetAsFirstSibling();
    }

    private void OnDisable()
    {
        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnHpChanged -= SetHp;
        }
    }

    private void Update()
    {
        if (_isPulseActive == false)
        {
            return;
        }

        ApplyPulse();
    }

    public void SetHp(float curHp, float maxHp)
    {
        transform.SetAsFirstSibling();

        float hpRatio = 0f;
        if (maxHp > 0f)
        {
            hpRatio = curHp / maxHp;
        }

        UpdateWarningIntensity(hpRatio);
    }

    private void UpdateWarningIntensity(float hpRatio)
    {
        if (hpRatio >= _hpRatioStart)
        {
            _baseAlpha = 0f;
            _isPulseActive = false;
            ApplyColor(0f);
            return;
        }

        float intensityRatio = Mathf.InverseLerp(_hpRatioStart, 0f, hpRatio);
        _baseAlpha = intensityRatio * _maxAlpha;

        _isPulseActive = (_isPulseEnabled == true) && (hpRatio <= _pulseHpRatioThreshold);

        if (_isPulseActive == false)
        {
            ApplyColor(_baseAlpha);
        }
    }

    private void ApplyPulse()
    {
        float pulseOffset = Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmplitude;
        float pulsedAlpha = Mathf.Clamp01(_baseAlpha + pulseOffset);
        ApplyColor(pulsedAlpha);
    }

    private void ApplyColor(float alpha)
    {
        if (Image_Vignette == null)
        {
            return;
        }

        Color targetColor = _warningColor;
        targetColor.a = alpha;
        Image_Vignette.color = targetColor;
    }

    private void CreateVignetteSprite()
    {
        if (Image_Vignette == null)
        {
            Debug.LogError("[HpWarningOverlayUI] Image_Vignette가 연결되지 않았습니다.");
            return;
        }

        Texture2D vignetteTexture = GenerateVignetteTexture(_vignetteTextureSize, _vignetteInnerRadius, _vignetteOuterRadius);
        Sprite vignetteSprite = Sprite.Create(vignetteTexture, new Rect(0f, 0f, _vignetteTextureSize, _vignetteTextureSize), new Vector2(0.5f, 0.5f));

        Image_Vignette.sprite = vignetteSprite;
        Image_Vignette.color = new Color(_warningColor.r, _warningColor.g, _warningColor.b, 0f);
    }

    private Texture2D GenerateVignetteTexture(int textureSize, float innerRadiusRatio, float outerRadiusRatio)
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureSize * textureSize];

        Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
        float maxDistance = textureSize * 0.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDistance = distance / maxDistance;
                float alpha = Mathf.InverseLerp(innerRadiusRatio, outerRadiusRatio, normalizedDistance);
                alpha = Mathf.Clamp01(alpha);

                int index = (y * textureSize) + x;
                pixels[index] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }
}
