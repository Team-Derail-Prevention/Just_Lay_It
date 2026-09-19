using UnityEngine;

public static class DisplayModeController
{
    public const int WindowedIndex = 0;
    public const int ExclusiveFullScreenIndex = 1;
    public const int BorderlessIndex = 2;

    private const float WindowedWorkAreaHeightRatio = 0.8f;
    private const float WindowedWorkAreaWidthLimitRatio = 0.9f;
    private const float WindowedAspectRatio = 16f / 9f;

    private static FullScreenMode _lastRequestedMode;
    private static int _lastRequestedFrame = -1;

    public static void Apply(int index)
    {
        switch (index)
        {
            case WindowedIndex:
                ApplyScreenMode(FullScreenMode.Windowed);
                Cursor.lockState = CursorLockMode.None;
                break;
            case ExclusiveFullScreenIndex:
                ApplyScreenMode(FullScreenMode.ExclusiveFullScreen);
                Cursor.lockState = CursorLockMode.None;
                break;
            case BorderlessIndex:
                ApplyScreenMode(FullScreenMode.FullScreenWindow);
                Cursor.lockState = CursorLockMode.Confined;
                break;
            default:
                Debug.LogWarning($"[DisplayModeController] 알 수 없는 디스플레이 모드 인덱스입니다. index={index}");
                break;
        }
    }

    private static void ApplyScreenMode(FullScreenMode targetMode)
    {
        if (Application.isEditor)
        {
            return;
        }

        if (GetEffectiveMode() == targetMode)
        {
            return;
        }

        Vector2Int targetSize = GetTargetSize(targetMode);

        Screen.SetResolution(targetSize.x, targetSize.y, targetMode);

        _lastRequestedMode = targetMode;
        _lastRequestedFrame = Time.frameCount;
    }

    private static FullScreenMode GetEffectiveMode()
    {
        if (_lastRequestedFrame == Time.frameCount)
        {
            return _lastRequestedMode;
        }

        return Screen.fullScreenMode;
    }

    private static Vector2Int GetTargetSize(FullScreenMode targetMode)
    {
        if (targetMode == FullScreenMode.Windowed)
        {
            return GetWindowedSize();
        }

        return GetDisplaySize();
    }

    private static Vector2Int GetDisplaySize()
    {
        DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;

        if (displayInfo.width > 0 && displayInfo.height > 0)
        {
            return new Vector2Int(displayInfo.width, displayInfo.height);
        }

        Resolution currentResolution = Screen.currentResolution;

        return new Vector2Int(currentResolution.width, currentResolution.height);
    }

    private static Vector2Int GetWindowedSize()
    {
        DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;

        int workAreaWidth = Mathf.RoundToInt(displayInfo.workArea.width);
        int workAreaHeight = Mathf.RoundToInt(displayInfo.workArea.height);

        if (workAreaWidth <= 0 || workAreaHeight <= 0)
        {
            Vector2Int displaySize = GetDisplaySize();

            workAreaWidth = displaySize.x;
            workAreaHeight = displaySize.y;
        }

        int windowedHeight = Mathf.RoundToInt(workAreaHeight * WindowedWorkAreaHeightRatio);
        int windowedWidth = Mathf.RoundToInt(windowedHeight * WindowedAspectRatio);
        int maxWindowedWidth = Mathf.RoundToInt(workAreaWidth * WindowedWorkAreaWidthLimitRatio);

        if (windowedWidth > maxWindowedWidth)
        {
            windowedWidth = maxWindowedWidth;
            windowedHeight = Mathf.RoundToInt(windowedWidth / WindowedAspectRatio);
        }

        return new Vector2Int(windowedWidth, windowedHeight);
    }
}
