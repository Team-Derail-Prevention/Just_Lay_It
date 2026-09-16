using Enums;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LocalizationManager : SingletonBase<LocalizationManager>
{
    public event Action OnLocalizationReady;

    private readonly Dictionary<string, UITextData> _textDataDic = new Dictionary<string, UITextData>();

    private bool _isCacheBuilt;

    public bool IsReady => _isCacheBuilt;

    private LanguageType _previewLanguage;

    public LanguageType CurrentLanguage => _previewLanguage;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterManager(this);
        }

        _previewLanguage = SaveManager.Instance.Language;

        if (DataManager.Instance.IsLoaded)
        {
            BuildCache();
        }
        else
        {
            DataManager.Instance.OnDataLoadCompleted += OnDataLoadCompleted;
        }
    }

    private void OnDataLoadCompleted()
    {
        DataManager.Instance.OnDataLoadCompleted -= OnDataLoadCompleted;
        BuildCache();
    }

    private void BuildCache()
    {
        _textDataDic.Clear();

        IReadOnlyList<UITextData> dataList = DataManager.Instance.GetAllData<UITextData>();

        foreach (UITextData data in dataList)
        {
            _textDataDic[data.Id] = data;
        }

        _isCacheBuilt = true;
        OnLocalizationReady?.Invoke();
    }

    public string GetText(string id)
    {
        if (TryGetData(id, out UITextData data) == false)
        {
            return id;
        }

        return CurrentLanguage == LanguageType.Korean ? data.Text_Ko : data.Text_En;
    }

    public int GetFontSize(string id)
    {
        if (TryGetData(id, out UITextData data) == false)
        {
            return 0;
        }

        return CurrentLanguage == LanguageType.Korean ? data.FontSiz_Ko : data.FontSiz_En;
    }

    private bool TryGetData(string id, out UITextData data)
    {
        if (_isCacheBuilt == false)
        {
            Debug.LogWarning($"[LocalizationManager] 아직 UITextData 캐시가 준비되지 않았습니다. (Id: {id})");
            data = null;
            return false;
        }

        if (_textDataDic.TryGetValue(id, out data) == false)
        {
            Debug.LogError($"[LocalizationManager] Id '{id}'에 해당하는 UI 텍스트 데이터가 없습니다.");
            return false;
        }

        return true;
    }

    public void PreviewLanguage(LanguageType language)
    {
        if (_previewLanguage == language)
        {
            return;
        }

        _previewLanguage = language;
        LocalizationEventHub.Instance.NotifyLanguageChanged(language);
    }

    public void CommitLanguage()
    {
        SaveManager.Instance.Language = _previewLanguage;
    }

    public void RevertLanguage()
    {
        _previewLanguage = SaveManager.Instance.Language;
        LocalizationEventHub.Instance.NotifyLanguageChanged(_previewLanguage);
    }
}
