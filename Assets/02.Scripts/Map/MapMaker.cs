using System.Collections.Generic;
using UnityEngine;

public class MapMaker : MonoBehaviour
{
    [SerializeField] private Transform _mapRoot;
    [SerializeField] private List<GameObject> _cubePrefabs = new List<GameObject>();
    [SerializeField] private int _gridSizeX = 14;
    [SerializeField] private int _gridSizeZ = 14;
    [SerializeField] private float _spacing = 2f;

    [ContextMenu("Generate 14x14 Grid Map")]
    public void GenerateMap()
    {
        if (_cubePrefabs == null || _cubePrefabs.Count == 0)
        {
            Debug.LogError("[MapMaker] 배치할 큐브 프리팹 리스트가 비어 있습니다.");
            return;
        }

        if (_mapRoot == null)
        {
            GameObject rootObj = new GameObject("@MapRoot_14x14");
            _mapRoot = rootObj.transform;
        }
        else
        {
            ClearExistingChildren();
        }

        System.Random rand = new System.Random();

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int z = 0; z < _gridSizeZ; z++)
            {
                int randomIndex = rand.Next(0, _cubePrefabs.Count);
                GameObject selectedPrefab = _cubePrefabs[randomIndex];

                if (selectedPrefab == null)
                {
                    continue;
                }

                float posX = (x - _gridSizeX / 2f) * _spacing;
                float posZ = (z - _gridSizeZ / 2f) * _spacing;
                Vector3 spawnPos = new Vector3(posX, 0f, posZ);

                Quaternion prefabRotation = selectedPrefab.transform.rotation;

                GameObject cubeObj = Instantiate(selectedPrefab, spawnPos, prefabRotation, _mapRoot);
                cubeObj.name = $"Cube_{x}_{z}_{selectedPrefab.name}";
            }
        }

        Debug.Log($"[MapMaker] 14x14 큐브 맵 생성 (사용된 프리팹 수: {_cubePrefabs.Count}개)");
    }

    private void ClearExistingChildren()
    {
        for (int i = _mapRoot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(_mapRoot.GetChild(i).gameObject);
        }
    }
}