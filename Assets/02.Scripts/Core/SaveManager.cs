using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class SaveManager : SingletonBase<SaveManager>
{
    private const string UpgradeSaveKey = "UpgradeSaveData";
    private const string FirstPlayNoticeSeenKey = "HasSeenFirstPlayNotice";
    private const string TotalPlayCountKey = "TotalPlayCount";
    private const string LifetimeStatsKey = "LifetimeStatsData";
    private const string AllStagesClearedKey = "HasClearedAllStagesSpecial";
    private const string SettingsSaveKey = "SettingsSaveData";

    private readonly HashSet<UpgradeSlotViewModel> _subscribedSlotSet = new HashSet<UpgradeSlotViewModel>();

    private UpgradeViewModel _upgradeViewModel;
    private UpgradeSaveData _loadedSaveData;
    private LifetimeStatsData _lifetimeStatsData;
    private SettingsSaveData _settingsSaveData;
    private bool _isLoading;

    public bool HasSeenFirstPlayNotice
    {
        get
        {
            return PlayerPrefs.GetInt(FirstPlayNoticeSeenKey, 0) == 1;
        }
        set
        {
            PlayerPrefs.SetInt(FirstPlayNoticeSeenKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

#if UNITY_EDITOR
    public void Debug_ResetFirstPlayNotice()
    {
        HasSeenFirstPlayNotice = false;
        Debug.Log("[SaveManager] HasSeenFirstPlayNotice를 초기화했습니다.");
    }
#endif

    public int TotalPlayCount
    {
        get
        {
            return PlayerPrefs.GetInt(TotalPlayCountKey, 0);
        }
    }

    public bool HasClearedAllStagesSpecial
    {
        get => PlayerPrefs.GetInt(AllStagesClearedKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(AllStagesClearedKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public float BgmVolume
    {
        get => GetSettingsSaveData().BgmVolume;
        set
        {
            SettingsSaveData data = GetSettingsSaveData();
            data.BgmVolume = value;
            SaveSettingsData(data);
        }
    }

    public float SfxVolume
    {
        get => GetSettingsSaveData().SfxVolume;
        set
        {
            SettingsSaveData data = GetSettingsSaveData();
            data.SfxVolume = value;
            SaveSettingsData(data);
        }
    }

    public int DisplayMode
    {
        get => GetSettingsSaveData().DisplayMode;
        set
        {
            SettingsSaveData data = GetSettingsSaveData();
            data.DisplayMode = value;
            SaveSettingsData(data);
        }
    }

    public int IncreaseTotalPlayCount()
    {
        int increasedCount = TotalPlayCount + 1;
        PlayerPrefs.SetInt(TotalPlayCountKey, increasedCount);
        PlayerPrefs.Save();

        return increasedCount;
    }

    public float LifetimeTotalDistance => GetLifetimeStatsData().TotalDistance;
    public float LifetimeTotalPlayTimeSeconds => GetLifetimeStatsData().TotalPlayTimeSeconds;
    public int LifetimeTotalRescuedHumanCount => GetLifetimeStatsData().TotalRescuedHumanCount;
    public int LifetimeTotalCollectedWood => GetLifetimeStatsData().TotalCollectedWood;
    public int LifetimeTotalCollectedStone => GetLifetimeStatsData().TotalCollectedStone;
    public int LifetimeTotalKillCount => GetLifetimeStatsData().TotalKillCount;
    public int LifetimeTotalRailCrafted => GetLifetimeStatsData().TotalRailCrafted;
    public int LifetimeTotalRailInstalled => GetLifetimeStatsData().TotalRailInstalled;
    public int LifetimeTotalEarnedCash => GetLifetimeStatsData().TotalEarnedCash;

    public void AddRunStatsToLifetime(RunStatsSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        LifetimeStatsData data = GetLifetimeStatsData();
        data.TotalDistance += snapshot.Distance;
        data.TotalPlayTimeSeconds += snapshot.PlayTimeSeconds;
        data.TotalRescuedHumanCount += snapshot.RescuedHumanCount;
        data.TotalCollectedWood += snapshot.CollectedWoodCount;
        data.TotalCollectedStone += snapshot.CollectedStoneCount;
        data.TotalKillCount += snapshot.KillCount;
        data.TotalRailCrafted += snapshot.RailCraftedCount;
        data.TotalRailInstalled += snapshot.RailInstalledCount;
        data.TotalEarnedCash += snapshot.EarnedCashCount;

        SaveLifetimeStatsData(data);
    }

    private LifetimeStatsData GetLifetimeStatsData()
    {
        if (_lifetimeStatsData == null)
        {
            _lifetimeStatsData = LoadLifetimeStatsData();
        }

        return _lifetimeStatsData;
    }

    private LifetimeStatsData LoadLifetimeStatsData()
    {
        if (PlayerPrefs.HasKey(LifetimeStatsKey) == false)
        {
            return new LifetimeStatsData();
        }

        string json = PlayerPrefs.GetString(LifetimeStatsKey);
        LifetimeStatsData data = JsonUtility.FromJson<LifetimeStatsData>(json);

        return data ?? new LifetimeStatsData();
    }

    private void SaveLifetimeStatsData(LifetimeStatsData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(LifetimeStatsKey, json);
        PlayerPrefs.Save();

        _lifetimeStatsData = data;
    }

    private SettingsSaveData GetSettingsSaveData()
    {
        if (_settingsSaveData == null)
        {
            _settingsSaveData = LoadSettingsData();
        }

        return _settingsSaveData;
    }

    private SettingsSaveData LoadSettingsData()
    {
        if (PlayerPrefs.HasKey(SettingsSaveKey) == false)
        {
            return new SettingsSaveData();
        }

        string json = PlayerPrefs.GetString(SettingsSaveKey);
        SettingsSaveData data = JsonUtility.FromJson<SettingsSaveData>(json);

        return data ?? new SettingsSaveData();
    }

    private void SaveSettingsData(SettingsSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SettingsSaveKey, json);
        PlayerPrefs.Save();

        _settingsSaveData = data;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterManager(this);
        }

        InitializeUpgradeSaveData();
    }

    // 종료시 저장
    private void OnApplicationQuit()
    {
        SaveUpgradeData();
    }

    private void InitializeUpgradeSaveData()
    {
        if (GameManager.NetworkUpgradeService == null)
        {
            Debug.LogWarning("[SaveManager] NetworkUpgradeService를 찾지 못해 업그레이드 저장을 초기화하지 못했습니다.");
            return;
        }

        _upgradeViewModel = GameManager.NetworkUpgradeService.GetLocalUpgradeViewModel();
        _upgradeViewModel.PropertyChanged += OnPropertyChanged_UpgradeViewModel;
        SubscribeSlotViewModels();

        _isLoading = true;
        _loadedSaveData = LoadUpgradeData();
        _upgradeViewModel.CurrentCash = _loadedSaveData.Cash;
        ApplySavedSlotLevels();
        _isLoading = false;
    }

    private void OnPropertyChanged_UpgradeViewModel(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(UpgradeViewModel.CurrentCash))
        {
            SaveUpgradeData();
            return;
        }

        if (eventArgs.PropertyName == "SlotListAdded")
        {
            SubscribeSlotViewModels();
            ApplySavedSlotLevels();
        }
    }

    private void OnPropertyChanged_UpgradeSlotViewModel(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(UpgradeSlotViewModel.CurrentLevel))
        {
            SaveUpgradeData();
        }
    }

    private void SubscribeSlotViewModels()
    {
        foreach (var slotKv in _upgradeViewModel.SlotDic)
        {
            UpgradeSlotViewModel slotViewModel = slotKv.Value;
            if (slotViewModel == null || _subscribedSlotSet.Add(slotViewModel) == false)
            {
                continue;
            }

            slotViewModel.PropertyChanged += OnPropertyChanged_UpgradeSlotViewModel;
        }
    }

    private void ApplySavedSlotLevels()
    {
        if (_loadedSaveData == null || _loadedSaveData.SlotList == null)
        {
            return;
        }

        foreach (UpgradeSlotSaveData slotSaveData in _loadedSaveData.SlotList)
        {
            if (slotSaveData == null || string.IsNullOrWhiteSpace(slotSaveData.SlotDataId))
            {
                continue;
            }

            UpgradeSlotViewModel slotViewModel = _upgradeViewModel.GetSlot(slotSaveData.SlotDataId);
            if (slotViewModel == null)
            {
                continue;
            }

            while (slotViewModel.CurrentLevel < slotSaveData.Level && slotViewModel.IsMaxLevel == false)
            {
                slotViewModel.LevelUp();
            }
        }
    }

    private void SaveUpgradeData()
    {
        if (_isLoading || _upgradeViewModel == null)
        {
            return;
        }

        UpgradeSaveData saveData = CreateUpgradeSaveData();
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(UpgradeSaveKey, json);
        PlayerPrefs.Save();

        _loadedSaveData = saveData;
    }

    private UpgradeSaveData LoadUpgradeData()
    {
        if (PlayerPrefs.HasKey(UpgradeSaveKey) == false)
        {
            return new UpgradeSaveData();
        }

        string json = PlayerPrefs.GetString(UpgradeSaveKey);
        UpgradeSaveData saveData = JsonUtility.FromJson<UpgradeSaveData>(json);

        return saveData ?? new UpgradeSaveData();
    }

    private UpgradeSaveData CreateUpgradeSaveData()
    {
        UpgradeSaveData saveData = new UpgradeSaveData();
        Dictionary<string, int> savedLevelDic = new Dictionary<string, int>();

        if (_loadedSaveData != null && _loadedSaveData.SlotList != null)
        {
            foreach (UpgradeSlotSaveData loadedSlotData in _loadedSaveData.SlotList)
            {
                if (loadedSlotData == null || string.IsNullOrWhiteSpace(loadedSlotData.SlotDataId))
                {
                    continue;
                }

                savedLevelDic[loadedSlotData.SlotDataId] = loadedSlotData.Level;
            }
        }

        foreach (var slotKv in _upgradeViewModel.SlotDic)
        {
            savedLevelDic[slotKv.Key] = slotKv.Value.CurrentLevel;
        }

        saveData.Cash = _upgradeViewModel.CurrentCash;

        foreach (var savedLevelKv in savedLevelDic)
        {
            UpgradeSlotSaveData slotSaveData = new UpgradeSlotSaveData();
            slotSaveData.SlotDataId = savedLevelKv.Key;
            slotSaveData.Level = savedLevelKv.Value;
            saveData.SlotList.Add(slotSaveData);
        }

        return saveData;
    }
}

[Serializable]
public class UpgradeSaveData
{
    public int Cash;
    public List<UpgradeSlotSaveData> SlotList = new List<UpgradeSlotSaveData>();
}

[Serializable]
public class UpgradeSlotSaveData
{
    public string SlotDataId;
    public int Level;
}

[Serializable]
public class LifetimeStatsData
{
    public float TotalDistance;
    public float TotalPlayTimeSeconds;
    public int TotalRescuedHumanCount;
    public int TotalCollectedWood;
    public int TotalCollectedStone;
    public int TotalKillCount;
    public int TotalRailCrafted;
    public int TotalRailInstalled;
    public int TotalEarnedCash;
}

[Serializable]
public class SettingsSaveData
{
    public float BgmVolume = 1f;
    public float SfxVolume = 1f;
    public int DisplayMode = 1;
}

