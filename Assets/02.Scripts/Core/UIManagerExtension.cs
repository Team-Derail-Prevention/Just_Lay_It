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
    HudTrainStatusUI,
    HudResourceUI,
    InGameMenuButtonUI,
    InGameMenuPopup,
    AugmentInventoryUI,
    TrainStrengtheningUI,
    WarehouseUI,
    TrainDepartureUI,
    BaseArrivalUI,
    TextInputPopup,
    StationArrivalUI,
    GameStartCountdownPopup,
    WeaponGachaUI,
    ScoreUI,
    HudMinimapUI,
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

    public static async Cysharp.Threading.Tasks.UniTask StartGameFromLobby(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.LobbyUI);

        var loadingUI = uiManager.OpenLoadingUI();
        if (loadingUI == null)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 null입니다. 게임 매니저가 초기화되지 않았습니다.");
            uiManager.CloseLoadingUI();
            return;
        }

        // 실제 로딩 추후 수정
        await GameManager.Instance.StartGame();

        if (loadingUI != null)
        {
            loadingUI.SetDataLoaded();
        }

        return;
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

    public static HudTrainStatusUI OpenHudTrainStatusUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenMainUI(UIType.HudTrainStatusUI);
        if (uiBase == null)
        {
            Debug.LogWarning("HudTrainStatusUI가 생성되지 않았습니다");
            return null;
        }

        return uiBase as HudTrainStatusUI;
    }

    public static void CloseHudTrainStatusUI(this UIManager uiManager)
    {
        uiManager.CloseMainUI(UIType.HudTrainStatusUI);
    }

    public static HudResourceUI OpenHudResourceUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenMainUI(UIType.HudResourceUI);
        if (uiBase == null)
        {
            Debug.LogWarning("HudResourceUI가 생성되지 않았습니다");
            return null;
        }

        return uiBase as HudResourceUI;
    }

    public static void CloseHudResourceUI(this UIManager uiManager)
    {
        uiManager.CloseMainUI(UIType.HudResourceUI);
    }

    public static HudMinimapUI OpenHudMinimapUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenMainUI(UIType.HudMinimapUI);
        if (uiBase == null)
        {
            Debug.LogWarning("HudMinimapUI가 생성되지 않았습니다");
            return null;
        }

        return uiBase as HudMinimapUI;
    }

    public static void CloseHudMinimapUI(this UIManager uiManager)
    {
        uiManager.CloseMainUI(UIType.HudMinimapUI);
    }

    public static void OpenInGameMenuButtonUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenMainUI(UIType.InGameMenuButtonUI);
        if (uiBase == null)
        {
            Debug.LogWarning("InGameMenuButtonUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseInGameMenuButtonUI(this UIManager uiManager)
    {
        uiManager.CloseMainUI(UIType.InGameMenuButtonUI);
    }

    public static void OpenInGameMenuPopup(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.InGameMenuPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("InGameMenuPopup가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseInGameMenuPopup(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.InGameMenuPopup);
    }

    public static void OpenAugmentInventoryUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.AugmentInventoryUI);
        if (uiBase == null)
        {
            Debug.LogWarning("AugmentInventoryUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseAugmentInventoryUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.AugmentInventoryUI);
    }

    public static void OpenTrainStrengtheningUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.TrainStrengtheningUI);
        if (uiBase == null)
        {
            Debug.LogWarning("TrainStrengtheningUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseTrainStrengtheningUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.TrainStrengtheningUI);
    }

    public static void OpenWarehouseUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.WarehouseUI);
        if (uiBase == null)
        {
            Debug.LogWarning("WarehouseUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseWarehouseUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.WarehouseUI);
    }

    public static void OpenTrainDepartureUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.TrainDepartureUI);
        if (uiBase == null)
        {
            Debug.LogWarning("TrainDepartureUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseTrainDepartureUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.TrainDepartureUI);
    }

    public static void OpenBaseArrivalUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenContentUI(UIType.BaseArrivalUI);
        if (uiBase == null)
        {
            Debug.LogWarning("BaseArrivalUI가 생성되지 않았습니다");
            return;
        }
    }

    public static void CloseBaseArrivalUI(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.BaseArrivalUI);
    }

    public static void OpenTextInputPopup(this UIManager uiManager, string message, Action<int> onConfirm, Action onInvalidInput = null, int maxAllowedAmount = int.MaxValue)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.TextInputPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("TextInputPopup가 생성되지 않았습니다");
            return;
        }

        if (uiBase is TextInputPopupUI textInputPopup)
        {
            textInputPopup.Init(message, onConfirm, onInvalidInput, maxAllowedAmount);
        }
    }

    public static void CloseTextInputPopup(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.TextInputPopup);
    }

    public static StationArrivalUI OpenStationArrivalUI(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenContentUI(UIType.StationArrivalUI); 
        if (uiBase == null)
        {
            Debug.LogWarning("StationArrivalUI가 생성되지 않았습니다");
            return null;
        }

        return uiBase as StationArrivalUI;
    }

    public static void CloseStationArrivalUI(this UIManager uiManager)
    {
        uiManager.CloseContentUI(UIType.StationArrivalUI);
    }

    public static GameStartCountdownPopup OpenGameStartCountdownPopup(this UIManager uiManager)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.GameStartCountdownPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("GameStartCountdownPopup가 생성되지 않았습니다");
            return null;
        }

        return uiBase as GameStartCountdownPopup;
    }

    public static void CloseGameStartCountdownPopup(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.GameStartCountdownPopup);
    }

    public static void OpenWeaponGachaUI(this UIManager uiManager)
    {
        uiManager.OpenPopupUI(UIType.WeaponGachaUI);
    }

    public static void CloseWeaponGachaUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.WeaponGachaUI);
    }

    public static ScoreUI OpenScoreUI(this UIManager uiManager, float totalDistance, int rescuedHumanCount, int collectedResourceCount, int killCount, ScoreResultType resultType, Action onConfirm)
    {
        var uiBase = uiManager.OpenPopupUI(UIType.ScoreUI);
        if (uiBase == null)
        {
            Debug.LogWarning("ScoreUI가 생성되지 않았습니다");
            return null;
        }

        if (uiBase is ScoreUI scoreUI)
        {
            scoreUI.Init(totalDistance, rescuedHumanCount, collectedResourceCount, killCount, resultType, onConfirm);
        }

        return uiBase as ScoreUI;
    }

    public static void CloseScoreUI(this UIManager uiManager)
    {
        uiManager.ClosePopupUI(UIType.ScoreUI);
    }

    public static void OpenRailPlaceConfirmPopup(this UIManager uiManager, Action onConfirm, Action onCancel)
    {
        var uiBase = uiManager.OpenMainUI(UIType.RailPlaceConfirmPopup);
        if (uiBase == null)
        {
            Debug.LogWarning("RailPlaceConfirmPopup가 생성되지 않았습니다");
            return;
        }

        if (uiBase is RailPlaceConfirmPopup confirmPopup)
        {
            confirmPopup.Init(onConfirm, onCancel);
        }
    }

    public static void CloseRailPlaceConfirmPopup(this UIManager uiManager)
    {
        uiManager.CloseMainUI(UIType.RailPlaceConfirmPopup);
    }
}
