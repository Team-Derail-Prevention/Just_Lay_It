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

    public static void StartGameFromLobby(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.LobbyUI);
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
}
