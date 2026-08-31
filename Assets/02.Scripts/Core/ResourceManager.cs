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
                T cachedResult = await handle.Convert<T>().ToUniTask();
                return cachedResult;
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

    public void Release(string address)
    {
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle) == false)
            return;

        Addressables.Release(handle);
        _handles.Remove(address);
        Debug.Log($"[ResourceManager:Release] {address}");
    }
}
