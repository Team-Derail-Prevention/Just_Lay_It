using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class SandstormOverlayUI : UIBase
{
    [Header("모래 레이어")]
    [SerializeField] private RawImage RawImage_Sand;
    [SerializeField] private CanvasGroup CanvasGroup_Root;

    [Header("모래 색상")]
    [SerializeField] private Color _sandColor = new Color(0.76f, 0.64f, 0.42f, 1f);

    [Header("타원형 시야 확보 영역 (가로세로 비율)")]
    [SerializeField] private float _ovalAspectX = 1.4f;
    [SerializeField] private float _ovalAspectY = 1f;

    [Header("타원 안쪽 반지름 (이 안쪽은 완전히 투명)")]
    [SerializeField, Range(0f, 1.5f)] private float _ovalInnerRadius = 0.35f;

    [Header("타원 바깥 반지름 (이 바깥은 완전히 모래로 덮임)")]
    [SerializeField, Range(0f, 1.5f)] private float _ovalOuterRadius = 0.75f;

    [Header("텍스처 생성 해상도")]
    [SerializeField] private int _textureWidth = 256;
    [SerializeField] private int _textureHeight = 256;

    [Header("모래 스크롤 속도")]
    [SerializeField] private Vector2 _scrollSpeed = new Vector2(0.08f, 0.03f);

    [Header("페이드 시간")]
    [SerializeField] private float _fadeInDuration = 1.5f;
    [SerializeField] private float _fadeOutDuration = 1.5f;

    private Vector2 _currentUvOffset = Vector2.zero;
    private CancellationTokenSource _showCts;

    private void Awake()
    {
        CreateSandTexture();

        if (CanvasGroup_Root != null)
        {
            CanvasGroup_Root.alpha = 0f;
        }
    }

    private void Update()
    {
        if (RawImage_Sand == null)
        {
            return;
        }

        _currentUvOffset += _scrollSpeed * Time.deltaTime;
        RawImage_Sand.uvRect = new Rect(_currentUvOffset, Vector2.one);
    }

    private void OnDestroy()
    {
        CancelShowTask();
    }

    public void Show(float duration)
    {
        CancelShowTask();

        _showCts = new CancellationTokenSource();
        RunShowSequence(duration, _showCts.Token).Forget();
    }

    public void Hide()
    {
        CancelShowTask();

        _showCts = new CancellationTokenSource();
        RunFadeOutOnly(_showCts.Token).Forget();
    }

    private void CancelShowTask()
    {
        if (_showCts != null)
        {
            _showCts.Cancel();
            _showCts.Dispose();
            _showCts = null;
        }
    }

    private async UniTaskVoid RunShowSequence(float duration, CancellationToken token)
    {
        await FadeCanvasGroup(0f, 1f, _fadeInDuration, token);

        float holdDuration = Mathf.Max(0f, duration - _fadeInDuration - _fadeOutDuration);
        await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: token);

        await FadeCanvasGroup(1f, 0f, _fadeOutDuration, token);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseSandstormOverlayUI();
        }
    }

    private async UniTaskVoid RunFadeOutOnly(CancellationToken token)
    {
        float currentAlpha = 0f;
        if (CanvasGroup_Root != null)
        {
            currentAlpha = CanvasGroup_Root.alpha;
        }

        await FadeCanvasGroup(currentAlpha, 0f, _fadeOutDuration, token);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseSandstormOverlayUI();
        }
    }

    private async UniTask FadeCanvasGroup(float fromAlpha, float toAlpha, float fadeDuration, CancellationToken token)
    {
        if (CanvasGroup_Root == null)
        {
            return;
        }

        if (fadeDuration <= 0f)
        {
            CanvasGroup_Root.alpha = toAlpha;
            return;
        }

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            token.ThrowIfCancellationRequested();

            elapsedTime += Time.deltaTime;
            float progressRatio = Mathf.Clamp01(elapsedTime / fadeDuration);
            CanvasGroup_Root.alpha = Mathf.Lerp(fromAlpha, toAlpha, progressRatio);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        CanvasGroup_Root.alpha = toAlpha;
    }

    private void CreateSandTexture()
    {
        if (RawImage_Sand == null)
        {
            Debug.LogError("[SandstormOverlayUI] RawImage_Sand가 연결되지 않았습니다.");
            return;
        }

        Texture2D sandTexture = GenerateSandTexture(_textureWidth, _textureHeight);
        RawImage_Sand.texture = sandTexture;
        RawImage_Sand.color = _sandColor;
    }

    private Texture2D GenerateSandTexture(int textureWidth, int textureHeight)
    {
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;

        Color[] pixels = new Color[textureWidth * textureHeight];
        Vector2 center = new Vector2(textureWidth * 0.5f, textureHeight * 0.5f);

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float normalizedX = (x - center.x) / (textureWidth * 0.5f);
                float normalizedY = (y - center.y) / (textureHeight * 0.5f);

                float adjustedX = normalizedX / _ovalAspectX;
                float adjustedY = normalizedY / _ovalAspectY;
                float distance = Mathf.Sqrt((adjustedX * adjustedX) + (adjustedY * adjustedY));

                float coverAlpha = Mathf.InverseLerp(_ovalInnerRadius, _ovalOuterRadius, distance);
                coverAlpha = Mathf.Clamp01(coverAlpha);

                int index = (y * textureWidth) + x;
                pixels[index] = new Color(1f, 1f, 1f, coverAlpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }
}
