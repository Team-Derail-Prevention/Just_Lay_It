using Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;

public class SaveManager : SingletonBase<SaveManager>
{
    private const string SaveFileName = "save.dat";

    private readonly HashSet<UpgradeSlotViewModel> _subscribedSlotSet = new HashSet<UpgradeSlotViewModel>();

    private UpgradeViewModel _upgradeViewModel;
    private SaveFileData _saveFileData;
    private bool _isLoading;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public bool HasSeenFirstPlayNotice
    {
        get => GetSaveFileData().HasSeenFirstPlayNotice;
        set
        {
            GetSaveFileData().HasSeenFirstPlayNotice = value;
            WriteSaveFile();
        }
    }

#if UNITY_EDITOR
    public void Debug_ResetFirstPlayNotice()
    {
        HasSeenFirstPlayNotice = false;
        Debug.Log("[SaveManager] HasSeenFirstPlayNotice를 초기화했습니다.");
    }
#endif

    public int TotalPlayCount => GetSaveFileData().TotalPlayCount;

    public bool HasClearedAllStagesSpecial
    {
        get => GetSaveFileData().HasClearedAllStagesSpecial;
        set
        {
            GetSaveFileData().HasClearedAllStagesSpecial = value;
            WriteSaveFile();
        }
    }

    public GameStage CurrentGameStage
    {
        get => GetSaveFileData().CurrentGameStage;
        set
        {
            GetSaveFileData().CurrentGameStage = value;
            WriteSaveFile();
        }
    }

    public float BgmVolume
    {
        get => GetSaveFileData().Settings.BgmVolume;
        set
        {
            GetSaveFileData().Settings.BgmVolume = value;
            WriteSaveFile();
        }
    }

    public float SfxVolume
    {
        get => GetSaveFileData().Settings.SfxVolume;
        set
        {
            GetSaveFileData().Settings.SfxVolume = value;
            WriteSaveFile();
        }
    }

    public int DisplayMode
    {
        get => GetSaveFileData().Settings.DisplayMode;
        set
        {
            GetSaveFileData().Settings.DisplayMode = value;
            WriteSaveFile();
        }
    }

    public LanguageType Language
    {
        get => (LanguageType)GetSaveFileData().Settings.Language;
        set
        {
            GetSaveFileData().Settings.Language = (int)value;
            WriteSaveFile();
        }
    }

    public int IncreaseTotalPlayCount()
    {
        SaveFileData data = GetSaveFileData();
        data.TotalPlayCount += 1;
        WriteSaveFile();

        return data.TotalPlayCount;
    }

    public float LifetimeTotalDistance => GetSaveFileData().LifetimeStats.TotalDistance;
    public float LifetimeTotalPlayTimeSeconds => GetSaveFileData().LifetimeStats.TotalPlayTimeSeconds;
    public int LifetimeTotalRescuedHumanCount => GetSaveFileData().LifetimeStats.TotalRescuedHumanCount;
    public int LifetimeTotalCollectedWood => GetSaveFileData().LifetimeStats.TotalCollectedWood;
    public int LifetimeTotalCollectedStone => GetSaveFileData().LifetimeStats.TotalCollectedStone;
    public int LifetimeTotalKillCount => GetSaveFileData().LifetimeStats.TotalKillCount;
    public int LifetimeTotalRailCrafted => GetSaveFileData().LifetimeStats.TotalRailCrafted;
    public int LifetimeTotalRailInstalled => GetSaveFileData().LifetimeStats.TotalRailInstalled;
    public int LifetimeTotalEarnedCash => GetSaveFileData().LifetimeStats.TotalEarnedCash;

    public void AddRunStatsToLifetime(RunStatsSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        LifetimeStatsData data = GetSaveFileData().LifetimeStats;
        data.TotalDistance += snapshot.Distance;
        data.TotalPlayTimeSeconds += snapshot.PlayTimeSeconds;
        data.TotalRescuedHumanCount += snapshot.RescuedHumanCount;
        data.TotalCollectedWood += snapshot.CollectedWoodCount;
        data.TotalCollectedStone += snapshot.CollectedStoneCount;
        data.TotalKillCount += snapshot.KillCount;
        data.TotalRailCrafted += snapshot.RailCraftedCount;
        data.TotalRailInstalled += snapshot.RailInstalledCount;
        data.TotalEarnedCash += snapshot.EarnedCashCount;

        WriteSaveFile();
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);

        LoadSaveFile();

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

    private SaveFileData GetSaveFileData()
    {
        if (_saveFileData == null)
        {
            LoadSaveFile();
        }

        return _saveFileData;
    }

    private void LoadSaveFile()
    {
        string path = SaveFilePath;

        if (File.Exists(path) == false)
        {
            _saveFileData = new SaveFileData();
            return;
        }

        try
        {
            byte[] cipherBytes = File.ReadAllBytes(path);
            string json = SaveCrypto.Decrypt(cipherBytes);
            SaveFileData loadedData = JsonUtility.FromJson<SaveFileData>(json);

            _saveFileData = loadedData ?? new SaveFileData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 세이브 파일 로드 실패, 손상된 것으로 판단하여 초기화합니다. 예외={ex}");
            _saveFileData = new SaveFileData();
        }
    }

    private void WriteSaveFile()
    {
        try
        {
            string json = JsonUtility.ToJson(_saveFileData);
            byte[] cipherBytes = SaveCrypto.Encrypt(json);
            File.WriteAllBytes(SaveFilePath, cipherBytes);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 세이브 파일 저장 실패. 예외={ex}");
        }
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
        _upgradeViewModel.CurrentCash = GetSaveFileData().UpgradeData.Cash;
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
        UpgradeSaveData loadedSaveData = GetSaveFileData().UpgradeData;

        if (loadedSaveData == null || loadedSaveData.SlotList == null)
        {
            return;
        }

        foreach (UpgradeSlotSaveData slotSaveData in loadedSaveData.SlotList)
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

        GetSaveFileData().UpgradeData = CreateUpgradeSaveData();
        WriteSaveFile();
    }

    private UpgradeSaveData CreateUpgradeSaveData()
    {
        UpgradeSaveData saveData = new UpgradeSaveData();
        Dictionary<string, int> savedLevelDic = new Dictionary<string, int>();

        UpgradeSaveData loadedSaveData = GetSaveFileData().UpgradeData;
        if (loadedSaveData != null && loadedSaveData.SlotList != null)
        {
            foreach (UpgradeSlotSaveData loadedSlotData in loadedSaveData.SlotList)
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

#if UNITY_EDITOR
    public void Debug_ResetAllProgressData()
    {
        _saveFileData = new SaveFileData();
        WriteSaveFile();

        if (GameManager.NetworkUpgradeService != null)
        {
            GameManager.NetworkUpgradeService.Debug_ResetUpgradeState();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameStage(GameStage.Stage1);
        }

        Debug.Log("[SaveManager] 전체 진행 데이터를 초기화했습니다.");
    }
#endif
}

[Serializable]
public class SaveFileData
{
    public bool HasSeenFirstPlayNotice;
    public int TotalPlayCount;
    public bool HasClearedAllStagesSpecial;
    public GameStage CurrentGameStage = GameStage.Stage1;
    public UpgradeSaveData UpgradeData = new UpgradeSaveData();
    public LifetimeStatsData LifetimeStats = new LifetimeStatsData();
    public SettingsSaveData Settings = new SettingsSaveData();
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
    public float BgmVolume = 0.5f;
    public float SfxVolume = 0.5f;
    public int DisplayMode = 1;
    public int Language = (int)LanguageType.English;
}

