using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"[ResourceManager:Awake] 현재 인스턴스가 존재하여 중복 오브젝트를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public async UniTask<T> LoadAsset<T>(string address) where T : UnityEngine.Object
    {
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            return handle.Result as T;
        }

        AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(address);

        try
        {
            T result = await loadHandle.ToUniTask();

            _handles[address] = loadHandle;
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($" [ResourceManager:LoadAsset] {address} / Error: {e.Message}");

            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
            return null;
        }
    }

    public void Release(string address)
    {
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle) == false)
            return;

        Addressables.Release(handle);
        _handles.Remove(address);
        Debug.Log($"[ResourceManager:Release] {address}");
    }
}
