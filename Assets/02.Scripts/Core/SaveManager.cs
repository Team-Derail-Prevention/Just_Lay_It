using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class SaveManager : SingletonBase<SaveManager>
{
    private const string UpgradeSaveKey = "UpgradeSaveData";

    private readonly HashSet<UpgradeSlotViewModel> _subscribedSlotSet = new HashSet<UpgradeSlotViewModel>();

    private UpgradeViewModel _upgradeViewModel;
    private UpgradeSaveData _loadedSaveData;
    private bool _isLoading;

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
        _upgradeViewModel.CurrentGold = _loadedSaveData.Gold;
        ApplySavedSlotLevels();
        _isLoading = false;
    }

    private void OnPropertyChanged_UpgradeViewModel(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(UpgradeViewModel.CurrentGold))
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

        saveData.Gold = _upgradeViewModel.CurrentGold;

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
    public int Gold;
    public List<UpgradeSlotSaveData> SlotList = new List<UpgradeSlotSaveData>();
}

[Serializable]
public class UpgradeSlotSaveData
{
    public string SlotDataId;
    public int Level;
}
