using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;

public class LocalizedTextAutoBinder
{
    [MenuItem("Tools/Localization/Auto-Bind Selected Prefab")]
    private static void AutoBindSelectedPrefab()
    {
        GameObject prefabRoot = Selection.activeGameObject;
        if (prefabRoot == null)
        {
            Debug.LogError("[LocalizedTextAutoBinder] 프리팹을 먼저 선택해주세요.");
            return;
        }

        UITextData[] allEntries = LoadUITextDataFromJson();
        if (allEntries == null)
        {
            return;
        }

        string place = prefabRoot.name;
        List<UITextData> placeEntries = allEntries.Where(entry => entry.Place == place).ToList();

        if (placeEntries.Count == 0)
        {
            Debug.LogWarning($"[LocalizedTextAutoBinder] Place '{place}'에 해당하는 항목이 없습니다. 프리팹 이름과 JSON의 Place 값이 일치하는지 확인해주세요.");
            return;
        }

        int autoBoundCount = 0;
        List<string> reviewList = new List<string>();

        TMP_Text[] allTexts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text targetText in allTexts)
        {
            if (targetText.GetComponent<LocalizedText>() != null)
            {
                continue;
            }

            List<UITextData> candidates = placeEntries.Where(entry => entry.Text_Ko == targetText.text).ToList();

            if (candidates.Count == 1)
            {
                BindLocalizedText(targetText, candidates[0].Id);
                autoBoundCount++;
            }
            else
            {
                string reason = candidates.Count == 0
                    ? "일치하는 텍스트 없음"
                    : $"중복 {candidates.Count}건 ({string.Join(", ", candidates.Select(c => c.Id))})";

                reviewList.Add($"{GetHierarchyPath(targetText.transform)} — {reason} (현재 텍스트: \"{targetText.text}\")");
            }
        }

        SavePrefabChanges(prefabRoot);

        Debug.Log($"[LocalizedTextAutoBinder] '{place}' 자동 연결 {autoBoundCount}건 완료. 수동 확인 필요 {reviewList.Count}건.");
        foreach (string line in reviewList)
        {
            Debug.LogWarning($"[LocalizedTextAutoBinder] 수동 확인 필요: {line}");
        }
    }

    private static UITextData[] LoadUITextDataFromJson()
    {
        string[] guids = AssetDatabase.FindAssets("UITextData t:TextAsset");
        if (guids.Length == 0)
        {
            Debug.LogError("[LocalizedTextAutoBinder] UITextData.json 파일을 프로젝트에서 찾지 못했습니다.");
            return null;
        }

        string jsonPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);

        string wrappedJson = "{\"items\":" + jsonAsset.text + "}";
        SerializationWrapper<UITextData> wrapper = JsonUtility.FromJson<SerializationWrapper<UITextData>>(wrappedJson);

        if (wrapper?.items == null)
        {
            Debug.LogError("[LocalizedTextAutoBinder] UITextData.json 파싱에 실패했습니다.");
            return null;
        }

        return wrapper.items.ToArray();
    }

    private static void BindLocalizedText(TMP_Text targetText, string localizationId)
    {
        LocalizedText localizedText = Undo.AddComponent<LocalizedText>(targetText.gameObject);

        SerializedObject serializedObject = new SerializedObject(localizedText);
        serializedObject.FindProperty("_localizationId").stringValue = localizationId;
        serializedObject.FindProperty("_targetText").objectReferenceValue = targetText;
        serializedObject.ApplyModifiedProperties();
    }

    private static void SavePrefabChanges(GameObject prefabRoot)
    {
        if (PrefabUtility.IsPartOfPrefabAsset(prefabRoot))
        {
            PrefabUtility.SavePrefabAsset(prefabRoot);
        }
        else
        {
            EditorUtility.SetDirty(prefabRoot);
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;

        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
