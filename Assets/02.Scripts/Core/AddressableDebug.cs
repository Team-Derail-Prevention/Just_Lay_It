#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class AddressableDebug
{
    [MenuItem("Tools/Addressables/Print Default Local Group")]
    public static void PrintDefaultLocalGroup()
    {
        AddressableAssetSettings settings =
            AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings를 찾을 수 없습니다.");
            return;
        }

        AddressableAssetGroup group = settings.FindGroup("Default Local Group");

        if (group == null)
        {
            Debug.LogError("Default Local Group 그룹을 찾을 수 없습니다.");
            return;
        }

        Debug.Log("========== Test Assets ==========");

        foreach (AddressableAssetEntry entry in group.entries)
        {
            Debug.Log(
                $"[Addressables] " +
                $"Name: {entry.MainAsset.name} | " +
                $"Path: {entry.AssetPath} | " +
                $"Address: {entry.address}"
            );
        }

        Debug.Log("=================================");
    }
}

#endif