using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class ResourceManager : SingletonBase<ResourceManager>
{
    private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

    public async UniTask<T> LoadAsset<T>(string address) where T : UnityEngine.Object
    {
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            if (handle.IsValid() == false)
            {
                _handles.Remove(address);
            }
            else
            {
                try
                {
                    T cachedResult = await handle.Convert<T>().ToUniTask();
                    return cachedResult;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ResourceManager:LoadAsset] (cached) {address} / Error: {e.Message}");
                    _handles.Remove(address);
                }
            }
        }

        AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(address);
        _handles[address] = loadHandle;

        try
        {
            T result = await loadHandle.ToUniTask();
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($" [ResourceManager:LoadAsset] {address} / Error: {e.Message}");
            _handles.Remove(address);

            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
            return null;
        }
    }

    public async UniTask<T> LoadAssetWithRetry<T>(string address, int maxRetryCount = 3, int retryDelayMs = 200) where T : UnityEngine.Object
    {
        for (int attempt = 0; attempt <= maxRetryCount; attempt++)
        {
            T result = await LoadAsset<T>(address);
            if (result != null)
            {
                return result;
            }

            if (attempt < maxRetryCount)
            {
                Debug.LogWarning($"[ResourceManager:LoadAssetWithRetry] {address} 로드 실패, 재시도 {attempt + 1}/{maxRetryCount}");
                await UniTask.Delay(retryDelayMs);
            }
        }

        Debug.LogError($"[ResourceManager:LoadAssetWithRetry] {address} 최종 로드 실패 (최대 재시도 초과)");
        return null;
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
