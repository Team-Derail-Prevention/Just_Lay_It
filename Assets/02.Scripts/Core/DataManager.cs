using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DataManager : SingletonBase<DataManager>
{
    public bool IsLoaded { get; private set; } = false;

    public event Action OnDataLoadCompleted;

    private readonly Dictionary<Type, object> _dataTableList = new Dictionary<Type, object>();

    public T GetData<T>(string dataId) where T : GameDataBase
    {
        Type type = typeof(T);

        if (string.IsNullOrWhiteSpace(dataId))
        {
            Debug.LogError($"[DataManager:GetData] 요청한 ID가 틀렸거나 빈칸입니다.");
            return null;
        }

        if (!_dataTableList.TryGetValue(type, out object container))
        {
            Debug.LogError($"[DataManager:GetData] {type.Name} 타입의 컨테이너가 존재하지 않습니다. 데이터가 로드되었는지 확인하세요.");
            return null;
        }

        if (container is Dictionary<string, T> dataTable)
        {
            if (dataTable.TryGetValue(dataId, out T data))
            {
                return data;
            }

            Debug.LogError($"[DataManager:GetData] {type.Name} 컨테이너 내에 ID '{dataId}'에 해당하는 데이터가 없습니다.");
            return null;
        }

        Debug.LogError($"[DataManager:GetData] {type.Name} 컨테이너의 데이터 구조가 일치하지 않습니다.");
        return null;
    }

    public IReadOnlyList<T> GetAllData<T>() where T : GameDataBase
    {
        Type type = typeof(T);

        if (!_dataTableList.TryGetValue(type, out object container) || container is not Dictionary<string, T> dataTable)
        {
            Debug.LogError($"[DataManager:GetAllData] {type.Name} 컨테이너가 없거나 데이터 구조가 올바르지 않습니다.");
            return CollectionCache<T>.EmptyList.AsReadOnly();
        }

        if (dataTable.Count == 0)
        {
            Debug.LogWarning($"[DataManager:GetAllData] {type.Name} 데이터가 비어있습니다.");
            return CollectionCache<T>.EmptyList.AsReadOnly();
        }

        return dataTable.Values.ToList().AsReadOnly();
    }

    public async UniTask LoadAllDatasAsync(CancellationToken cancellationToken = default)
    {
        IsLoaded = false;

        await LoadDataAsync<MapData>("MapData", cancellationToken);
        await LoadDataAsync<MonsterData>("MonsterTableData", cancellationToken);
        await LoadDataAsync<MaterialObjectData>("MaterialObjectData", cancellationToken);
        await LoadDataAsync<TrainData>("TrainData", cancellationToken);
        await LoadDataAsync<LobbyTrainUpgradeData>("LobbyTrainUpgradeData", cancellationToken);
        await LoadDataAsync<InGameTrainUpgradeData>("InGameTrainUpgradeData", cancellationToken);
        await LoadDataAsync<DroneUpgradeData>("DroneUpgradeData", cancellationToken);
        await LoadDataAsync<WeaponData>("WeaponData", cancellationToken);
        await LoadDataAsync<InGameUpgradeData>("InGameUpgradeData", cancellationToken);
        await LoadDataAsync<LobbyUpgradeData>("LobbyUpgradeData", cancellationToken);
        await LoadDataAsync<CameraData>("CameraData", cancellationToken);

        IsLoaded = true;
        OnDataLoadCompleted?.Invoke();
        Debug.Log("[DataManager] 모든 데이터 로드 및 초기화 완료!");
    }

    private async UniTask LoadDataAsync<T>(string address, CancellationToken cancellationToken) where T : GameDataBase
    {
        Type type = typeof(T);

        TextAsset textAsset = await GameManager.Resource.LoadAsset<TextAsset>(address);
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager:LoadDataAsync] 데이터 로드 실패 (주소: {address})");
            return;
        }

        try
        {
            string jsonText = textAsset.text;
            string wrappedJson = "{\"items\":" + jsonText + "}";
            SerializationWrapper<T> wrapper = JsonUtility.FromJson<SerializationWrapper<T>>(wrappedJson);

            if (wrapper?.items == null)
            {
                Debug.LogError($"[DataManager:LoadDataAsync] 데이터 파싱 결과가 비어있습니다. (타입: {type.Name})");
                return;
            }

            if (_dataTableList.ContainsKey(type))
            {
                Debug.LogWarning($"[DataManager:LoadDataAsync] {type.Name} 데이터가 이미 존재합니다. 덮어씁니다.");
            }

            _dataTableList[type] = CreateDictionary(wrapper.items);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager:LoadDataAsync] 예외 상황 발생 ({type.Name}): {ex.Message}");
        }
    }

    private Dictionary<string, T> CreateDictionary<T>(List<T> items) where T : GameDataBase
    {
        Dictionary<string, T> dictionary = new Dictionary<string, T>();

        if (items == null)
        {
            Debug.LogError($"[DataManager:CreateDictionary] 전달된 리스트가 null입니다.");
            return dictionary;
        }

        foreach (T item in items)
        {
            if (item == null)
            {
                Debug.LogWarning($"[DataManager:CreateDictionary] 리스트에 null 데이터가 포함되어 건너뜁니다.");
                continue;
            }

            string idStr = item.Id.ToString();
            if (dictionary.ContainsKey(idStr))
            {
                Debug.LogWarning($"[DataManager:CreateDictionary] 중복된 ID('{idStr}')가 존재합니다. 최신 값으로 덮어씁니다.");
            }

            dictionary[idStr] = item;
        }

        return dictionary;
    }
}