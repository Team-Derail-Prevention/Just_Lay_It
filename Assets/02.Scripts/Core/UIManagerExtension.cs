using UnityEngine;
using System;

public enum UIRootType
{
    None = 0,
    BackgroundUI,
    MainUI,
    ContentUI,
    PopupUI,
    VeryFrontUI
}

public enum UIType
{
    StartUI,
    LobbyUI,
    LobbyUpgradeUI,
    SettingUI,
    GameBookUI,
    ExitConfirmPopup,
    LoadingUI,
    RailBuildUI,
    RailPlaceConfirmPopup,
}
public static class UIManagerExtension
{
    public static string GetUIPath(this UIManager uiManager, UIRootType uiRootType, UIType uiType)
    {
        string path = string.Empty;

        path = $"UI/{uiRootType}/{uiType}";
        return path;
    }

    public static void ShowStartupUIOnGameStart(this UIManager uiManager)
    {
        uiManager.OpenContentUI(UIType.StartUI);
    }

    public static void CompleteStartUI(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.StartUI);
        uiManager.OpenContentUI(UIType.LobbyUI);
    }

    public static Cysharp.Threading.Tasks.UniTask StartGameFromLobby(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.LobbyUI);

        var loadingUI = uiManager.OpenLoadingUI();
        if (loadingUI == null)
        {
            return Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        // 실제 로딩 추후 수정

        loadingUI.SetDataLoaded();

        return Cysharp.Threading.Tasks.UniTask.CompletedTask;
    }

    public static LoadingUI OpenLoadingUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
        if (uiBase == null)
        {
            Debug.LogWarning("LoadingUI가 생성되지 않았습니다");
            return null;
        }

        return uiBase as LoadingUI;
    }

    public static void CloseLoadingUI(this UIManager uiManager)
    {
        uiManager.CloseUI(UIRootType.VeryFrontUI, UIType.LoadingUI);
    }

    public static void OpenLobbyUpgradeUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenContentUI(UIType.LobbyUpgradeUI);

        if (uiBase == null)
        {
            Debug.LogWarning("LobbyUpgradeUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseLobbyUpgradeUI(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.LobbyUpgradeUI);

    }

    public static void OpenGameBookUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.GameBookUI);
        if (uiBase == null)
        {
            Debug.LogWarning("GameBookUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseGameBookUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.GameBookUI);
    }

    public static void OpenSettingUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.SettingUI);
        if (uiBase == null)
        {
            Debug.LogWarning("SettingUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseSettingUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.SettingUI);
    }

    public static void OpenExitConfirmPopup(this UIManager uiManager, Action onConfirmExit, Action onCancel = null, string message = null)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.ExitConfirmPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("ExitConfirmPopup가 생성되지 않았습니다");
            return;
        }

        if (uiBase is ExitConfirmPopup exitConfirmPopup)
        {
            exitConfirmPopup.Init(message, onConfirmExit, onCancel);
        }
    }

    public static void CloseExitConfirmPopup(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.ExitConfirmPopup);
    }

    public static void OpenRailBuildUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenMainUI(UIType.RailBuildUI);
        if (uiBase == null)
        {
            Debug.LogWarning("RailBuildUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseRailBuildUI(this UIManager uiManager)
    {
        uiManager.CloseMainUI(UIType.RailBuildUI);
    }

    public static void OpenRailPlaceConfirmPopup(this UIManager uiManager, Action onRotate, Action onConfirm, Action onCancel)
    {
        var uiBase = uiManager.OpenMainUI(UIType.RailPlaceConfirmPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("RailPlaceConfirmPopup가 생성되지 않았습니다");
            return;
        }

        if (uiBase is RailPlaceConfirmPopup confirmPopup)
        {
            confirmPopup.Init(onRotate, onConfirm, onCancel);
        }
    }

    public static void CloseRailPlaceConfirmPopup(this UIManager uiManager)
    {
        uiManager.CloseMainUI(UIType.RailPlaceConfirmPopup);
    }
}
