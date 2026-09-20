using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HpWarningOverlayUI : UIBase
{
    [Header("비네트 이미지")]
    [SerializeField] private Image Image_Vignette;

    [Header("경고 색상")]
    [SerializeField] private Color _warningColor = Color.red;

    [Header("점멸 효과")]
    [SerializeField, Range(1, 10)] private int _blinkCount = 3;
    [SerializeField] private float _blinkDurationSeconds = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _blinkMaxAlpha = 0.6f;

    [Header("비네트 텍스처 생성 설정")]
    [SerializeField] private int _vignetteTextureSize = 256;
    [SerializeField, Range(0f, 1f)] private float _vignetteInnerRadius = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _vignetteOuterRadius = 0.75f;

    private Coroutine _blinkCoroutine;

    private void Awake()
    {
        CreateVignetteSprite();
    }

    private void OnEnable()
    {
        ApplyColor(0f);

        if (TrainStatusEventHub.Instance == null)
        {
            Debug.LogError("[HpWarningOverlayUI] TrainStatusEventHub.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
            return;
        }

        TrainStatusEventHub.Instance.OnLowHpWarning += OnLowHpWarning_TrainStatusEventHub;

        transform.SetAsFirstSibling();
    }

    private void OnDisable()
    {
        if (TrainStatusEventHub.Instance != null)
        {
            TrainStatusEventHub.Instance.OnLowHpWarning -= OnLowHpWarning_TrainStatusEventHub;
        }

        StopBlink();
    }

    private void OnLowHpWarning_TrainStatusEventHub()
    {
        PlayBlink();
    }

    private void PlayBlink()
    {
        StopBlink();

        transform.SetAsFirstSibling();
        _blinkCoroutine = StartCoroutine(CoPlayBlink());
    }

    private void StopBlink()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        ApplyColor(0f);
    }

    private IEnumerator CoPlayBlink()
    {
        for (int blinkIndex = 0; blinkIndex < _blinkCount; blinkIndex++)
        {
            float elapsedSeconds = 0f;

            while (elapsedSeconds < _blinkDurationSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(elapsedSeconds / _blinkDurationSeconds);
                float alpha = Mathf.Sin(progress * Mathf.PI) * _blinkMaxAlpha;
                ApplyColor(alpha);

                yield return null;
            }
        }

        ApplyColor(0f);
        _blinkCoroutine = null;
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
