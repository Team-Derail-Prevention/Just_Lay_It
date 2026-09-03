using UnityEngine;
using System.Collections.Generic;

public class MapSkirtMaker : MonoBehaviour
{
    [Header("스커트 지형 설정")]
    [SerializeField] private Material _skirtMaterial;
    [SerializeField] private int _ringCount = 3;
    [SerializeField] private float _ringWidth = 30f;
    [SerializeField] private float _heightDropPerRing = 1.5f;
    [SerializeField] private float _uvScale = 30f;
    [SerializeField] private int _segmentsPerSide = 8;

    [Header("타일 경계 보정 (눈으로 맞추는 값)")]
    [SerializeField] private float _innerHeight = 1f;
    [SerializeField] private Vector2 _boundaryOffset = new Vector2(-1f, -1f);

    [Header("듄(모래언덕) 굴곡 설정")]
    [SerializeField] private float _duneNoiseScale = 0.05f;
    [SerializeField] private float _duneNoiseHeight = 0.6f;

    [Header("장애물 스캐터 설정")]
    [SerializeField] private List<GameObject> _obstaclePrefabs = new List<GameObject>();
    [SerializeField] private int _obstacleCountPerRing = 32;
    [SerializeField] private string _decorationLayerName = "Default";
    [SerializeField] private float _innerRingBias = 2.5f;

    private GameObject _skirtObject;

    public void GenerateSkirt(int mapSize, float tileSpacing, Transform parent)
    {
        RemoveSkirt();

        float innerHalfSize = (mapSize * tileSpacing) / 2f;
        Mesh skirtMesh = BuildSkirtMesh(innerHalfSize);

        _skirtObject = new GameObject("MapSkirt");
        _skirtObject.transform.SetParent(parent);
        _skirtObject.transform.localPosition = Vector3.zero;

        MeshFilter meshFilter = _skirtObject.AddComponent<MeshFilter>();
        meshFilter.mesh = skirtMesh;

        MeshRenderer meshRenderer = _skirtObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = _skirtMaterial;

        ScatterObstacles(innerHalfSize, _skirtObject.transform);
    }

    public void RemoveSkirt()
    {
        if (_skirtObject != null)
        {
            Destroy(_skirtObject);
            _skirtObject = null;
        }
    }

    private Mesh BuildSkirtMesh(float innerHalfSize)
    {
        int loopCount = _ringCount + 1;
        int pointsPerRing = _segmentsPerSide * 4;

        List<Vector3> vertexList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();

        for (int ringIndex = 0; ringIndex < loopCount; ringIndex++)
        {
            float ringHalfSize = innerHalfSize + (ringIndex * _ringWidth);
            List<Vector2> perimeterPoints = GeneratePerimeterPoints(ringHalfSize);

            for (int pointIndex = 0; pointIndex < perimeterPoints.Count; pointIndex++)
            {
                float worldX = perimeterPoints[pointIndex].x + _boundaryOffset.x;
                float worldZ = perimeterPoints[pointIndex].y + _boundaryOffset.y;
                float height = CalculateHeight(worldX, worldZ, innerHalfSize);

                vertexList.Add(new Vector3(worldX, height, worldZ));
                uvList.Add(new Vector2(worldX / _uvScale, worldZ / _uvScale));
            }
        }

        List<int> triangleList = new List<int>();

        for (int ringIndex = 0; ringIndex < _ringCount; ringIndex++)
        {
            int innerRingStart = ringIndex * pointsPerRing;
            int outerRingStart = (ringIndex + 1) * pointsPerRing;

            for (int pointIndex = 0; pointIndex < pointsPerRing; pointIndex++)
            {
                int nextPointIndex = (pointIndex + 1) % pointsPerRing;

                int innerA = innerRingStart + pointIndex;
                int innerB = innerRingStart + nextPointIndex;
                int outerA = outerRingStart + pointIndex;
                int outerB = outerRingStart + nextPointIndex;

                triangleList.Add(innerA);
                triangleList.Add(outerA);
                triangleList.Add(outerB);

                triangleList.Add(innerA);
                triangleList.Add(outerB);
                triangleList.Add(innerB);
            }
        }

        Mesh skirtMesh = new Mesh();
        skirtMesh.name = "MapSkirtMesh";
        skirtMesh.SetVertices(vertexList);
        skirtMesh.SetUVs(0, uvList);
        skirtMesh.SetTriangles(triangleList, 0);
        skirtMesh.RecalculateNormals();
        skirtMesh.RecalculateBounds();

        return skirtMesh;
    }

    private List<Vector2> GeneratePerimeterPoints(float halfSize)
    {
        List<Vector2> pointList = new List<Vector2>();

        Vector2 cornerA = new Vector2(halfSize, halfSize);
        Vector2 cornerB = new Vector2(halfSize, -halfSize);
        Vector2 cornerC = new Vector2(-halfSize, -halfSize);
        Vector2 cornerD = new Vector2(-halfSize, halfSize);

        AppendEdgePoints(pointList, cornerA, cornerB);
        AppendEdgePoints(pointList, cornerB, cornerC);
        AppendEdgePoints(pointList, cornerC, cornerD);
        AppendEdgePoints(pointList, cornerD, cornerA);

        return pointList;
    }

    private void AppendEdgePoints(List<Vector2> pointList, Vector2 startPoint, Vector2 endPoint)
    {
        for (int segmentIndex = 0; segmentIndex < _segmentsPerSide; segmentIndex++)
        {
            float t = (float)segmentIndex / _segmentsPerSide;
            Vector2 lerpedPoint = Vector2.Lerp(startPoint, endPoint, t);
            pointList.Add(lerpedPoint);
        }
    }

    private float CalculateHeight(float worldX, float worldZ, float innerHalfSize)
    {
        float distanceFromCenter = Mathf.Max(Mathf.Abs(worldX - _boundaryOffset.x), Mathf.Abs(worldZ - _boundaryOffset.y));
        float ringPosition = (distanceFromCenter - innerHalfSize) / _ringWidth;
        float baseHeight = _innerHeight - (ringPosition * _heightDropPerRing);

        float noiseFalloff = Mathf.Clamp01(ringPosition);
        float noiseSample = Mathf.PerlinNoise(worldX * _duneNoiseScale, worldZ * _duneNoiseScale);
        float noiseOffset = (noiseSample - 0.5f) * 2f * _duneNoiseHeight * noiseFalloff;

        return baseHeight + noiseOffset;
    }

    private void ScatterObstacles(float innerHalfSize, Transform parent)
    {
        if (_obstaclePrefabs == null || _obstaclePrefabs.Count == 0)
        {
            return;
        }

        int decorationLayerIndex = LayerMask.NameToLayer(_decorationLayerName);
        float outerHalfSize = innerHalfSize + (_ringCount * _ringWidth);
        int totalObstacleCount = _obstacleCountPerRing * _ringCount;

        for (int obstacleIndex = 0; obstacleIndex < totalObstacleCount; obstacleIndex++)
        {
            Vector2 randomPoint = FindRandomPointInSkirtArea(innerHalfSize, outerHalfSize);

            float worldX = randomPoint.x;
            float worldZ = randomPoint.y;
            float worldY = CalculateHeight(worldX, worldZ, innerHalfSize);

            int prefabIndex = Random.Range(0, _obstaclePrefabs.Count);
            GameObject selectedPrefab = _obstaclePrefabs[prefabIndex];

            if (selectedPrefab == null)
            {
                continue;
            }

            Vector3 spawnPosition = new Vector3(worldX, worldY, worldZ);
            GameObject obstacleObj = Instantiate(selectedPrefab, spawnPosition, selectedPrefab.transform.rotation, parent);
            obstacleObj.name = $"{selectedPrefab.name}_SkirtDecoration";

            RemoveGameplayComponents(obstacleObj, decorationLayerIndex);
        }
    }

    private Vector2 FindRandomPointInSkirtArea(float innerHalfSize, float outerHalfSize)
    {
        float biasedT = Mathf.Pow(Random.value, _innerRingBias);
        float targetHalfSize = Mathf.Lerp(innerHalfSize, outerHalfSize, biasedT);

        Vector2 localPoint = SampleSquarePerimeterPoint(targetHalfSize);

        return new Vector2(localPoint.x + _boundaryOffset.x, localPoint.y + _boundaryOffset.y);
    }

    private Vector2 SampleSquarePerimeterPoint(float halfSize)
    {
        float perimeterT = Random.value * 4f;
        int sideIndex = Mathf.FloorToInt(perimeterT);
        float t = perimeterT - sideIndex;

        Vector2 cornerA = new Vector2(halfSize, halfSize);
        Vector2 cornerB = new Vector2(halfSize, -halfSize);
        Vector2 cornerC = new Vector2(-halfSize, -halfSize);
        Vector2 cornerD = new Vector2(-halfSize, halfSize);

        switch (sideIndex)
        {
            case 0:
                return Vector2.Lerp(cornerA, cornerB, t);
            case 1:
                return Vector2.Lerp(cornerB, cornerC, t);
            case 2:
                return Vector2.Lerp(cornerC, cornerD, t);
            default:
                return Vector2.Lerp(cornerD, cornerA, t);
        }
    }

    private void RemoveGameplayComponents(GameObject targetObject, int layerIndex)
    {
        Collider[] colliderArray = targetObject.GetComponentsInChildren<Collider>();

        for (int colliderIndex = 0; colliderIndex < colliderArray.Length; colliderIndex++)
        {
            Destroy(colliderArray[colliderIndex]);
        }

        MapTileInfo tileInfo = targetObject.GetComponent<MapTileInfo>();

        if (tileInfo != null)
        {
            Destroy(tileInfo);
        }

        if (layerIndex != -1)
        {
            SetLayerRecursively(targetObject, layerIndex);
        }
    }

    private void SetLayerRecursively(GameObject targetObject, int layerIndex)
    {
        targetObject.layer = layerIndex;

        foreach (Transform childTransform in targetObject.transform)
        {
            SetLayerRecursively(childTransform.gameObject, layerIndex);
        }
    }
}
