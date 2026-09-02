using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.UI;

public class HudMinimapUI : UIBase
{
    [Header("미니맵 영역")]
    [SerializeField] private RectTransform Rect_MinimapArea;
    [SerializeField, Range(0f, 0.5f)] private float _worldPaddingRatio = 0.15f;

    [Header("베이스 아이콘")]
    [SerializeField] private RectTransform Icon_Base;

    [Header("기차 아이콘")]
    [SerializeField] private RectTransform Icon_Train;
    [SerializeField] private bool _rotateTrainIcon = true;

    [Header("스테이션 마커")]
    [SerializeField] private GameObject Prefab_StationMarker;
    [SerializeField] private Transform Transform_StationMarkerRoot;

    [Header("스테이션 마커 색상")]
    [SerializeField] private Color _stationMarkerColor = new Color(1f, 0.6f, 0f);

    private readonly MinimapViewModel _vm = new MinimapViewModel();
    private readonly List<StationMarkerEntry> _stationMarkerEntries = new List<StationMarkerEntry>();
    private StationObject[] _currentStations = new StationObject[0];

    private class StationMarkerEntry
    {
        public StationObject Station;
        public Image MarkerImage;
        public bool IsVisited;
    }

    private void OnEnable()
    {
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        _vm.OnStationPositionsChanged += RefreshStationMarkers;

        if (MapManager.Instance == null)
        {
            Debug.LogError("[HudMinimapUI] MapManager.Instance가 null입니다. 씬에 배치했는지 확인하세요.");
            return;
        }

        MapManager.Instance.OnMapGenerated += HandleMapGenerated;

        Dictionary<Vector3Int, int> existingMapData = MapManager.Instance.GetMapTypeData();
        if (existingMapData != null && existingMapData.Count > 0)
        {
            HandleMapGenerated(existingMapData);
        }
    }

    private void OnDisable()
    {
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm.OnStationPositionsChanged -= RefreshStationMarkers;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnMapGenerated -= HandleMapGenerated;
        }
    }

    private void Update()
    {
        UpdateTrainPosition();
        UpdateStationVisitState();
    }

    private void HandleMapGenerated(Dictionary<Vector3Int, int> mapTypeData)
    {
        if (GameManager.Map == null || GameManager.Map.MapRoot == null)
        {
            Debug.LogWarning("[HudMinimapUI] MapRoot가 없어 미니맵을 구성할 수 없습니다.");
            return;
        }

        CentralTerminal terminal = GameManager.Map.MapRoot.GetComponentInChildren<CentralTerminal>(true);
        StationObject[] stations = GameManager.Map.MapRoot.GetComponentsInChildren<StationObject>(true);

        if (terminal == null)
        {
            Debug.LogError("[HudMinimapUI] CentralTerminal을 찾을 수 없어 미니맵을 구성할 수 없습니다.");
            return;
        }

        _vm.WorldBounds = CalculateWorldBounds(terminal, stations);
        _vm.BasePosition = _vm.NormalizeWorldPosition(terminal.transform.position);

        _currentStations = stations;

        List<Vector2> stationPositions = new List<Vector2>();
        for (int i = 0; i < stations.Length; i++)
        {
            Vector2 normalizedPosition = _vm.NormalizeWorldPosition(stations[i].transform.position);
            stationPositions.Add(normalizedPosition);
        }

        _vm.SetStationPositions(stationPositions);
    }

    private Rect CalculateWorldBounds(CentralTerminal terminal, StationObject[] stations)
    {
        float minX = terminal.transform.position.x;
        float maxX = terminal.transform.position.x;
        float minZ = terminal.transform.position.z;
        float maxZ = terminal.transform.position.z;

        for (int i = 0; i < stations.Length; i++)
        {
            Vector3 position = stations[i].transform.position;

            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }

        float width = Mathf.Max(maxX - minX, 0.01f);
        float height = Mathf.Max(maxZ - minZ, 0.01f);

        float paddingX = width * _worldPaddingRatio;
        float paddingZ = height * _worldPaddingRatio;

        return new Rect(minX - paddingX, minZ - paddingZ, width + (paddingX * 2f), height + (paddingZ * 2f));
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MinimapViewModel.BasePosition))
        {
            SetIconAnchoredPosition(Icon_Base, _vm.BasePosition);
        }
    }

    private void RefreshStationMarkers()
    {
        ClearStationMarkers();

        if (Prefab_StationMarker == null || Transform_StationMarkerRoot == null)
        {
            return;
        }

        IReadOnlyList<Vector2> positions = _vm.StationPositions;
        int markerCount = Mathf.Min(positions.Count, _currentStations.Length);

        for (int i = 0; i < markerCount; i++)
        {
            CreateStationMarker(_currentStations[i], positions[i]);
        }
    }

    private void CreateStationMarker(StationObject station, Vector2 normalizedPosition)
    {
        GameObject markerObject = Instantiate(Prefab_StationMarker, Transform_StationMarkerRoot);
        if (markerObject == null)
        {
            return;
        }

        Image markerImage = markerObject.GetComponent<Image>();
        if (markerImage == null)
        {
            Debug.LogWarning("[HudMinimapUI] Prefab_StationMarker 루트에 Image 컴포넌트가 없습니다.");
            Destroy(markerObject);
            return;
        }

        markerImage.color = _stationMarkerColor;
        SetIconAnchoredPosition(markerImage.rectTransform, normalizedPosition);

        StationMarkerEntry entry = new StationMarkerEntry
        {
            Station = station,
            MarkerImage = markerImage,
            IsVisited = false
        };

        _stationMarkerEntries.Add(entry);
    }

    private void ClearStationMarkers()
    {
        for (int i = 0; i < _stationMarkerEntries.Count; i++)
        {
            if (_stationMarkerEntries[i].MarkerImage != null)
            {
                Destroy(_stationMarkerEntries[i].MarkerImage.gameObject);
            }
        }

        _stationMarkerEntries.Clear();
    }

    private void UpdateStationVisitState()
    {
        for (int i = 0; i < _stationMarkerEntries.Count; i++)
        {
            StationMarkerEntry entry = _stationMarkerEntries[i];
            if (entry.IsVisited || entry.MarkerImage == null)
            {
                continue;
            }

            bool isStationDestroyed = entry.Station == null;
            bool isVisited = isStationDestroyed;

            if (isVisited == false && GameManager.Train != null)
            {
                isVisited = GameManager.Train.IsVisitedStation(entry.Station.transform);
            }

            if (isVisited == false)
            {
                continue;
            }

            entry.IsVisited = true;
            entry.MarkerImage.gameObject.SetActive(false);
        }
    }

    private void UpdateTrainPosition()
    {
        if (Icon_Train == null || GameManager.Train == null)
        {
            return;
        }

        Transform headTransform = GameManager.Train.HeadTransform;
        if (headTransform == null)
        {
            return;
        }

        Vector2 normalizedPosition = _vm.NormalizeWorldPosition(headTransform.position);
        _vm.TrainPosition = normalizedPosition;

        SetIconAnchoredPosition(Icon_Train, normalizedPosition);

        if (_rotateTrainIcon)
        {
            float headingAngle = Mathf.Atan2(headTransform.forward.x, headTransform.forward.z) * Mathf.Rad2Deg;
            Icon_Train.localRotation = Quaternion.Euler(0f, 0f, -headingAngle);
        }
    }

    private void SetIconAnchoredPosition(RectTransform icon, Vector2 normalizedPosition)
    {
        if (icon == null || Rect_MinimapArea == null)
        {
            return;
        }

        float areaWidth = Rect_MinimapArea.rect.width;
        float areaHeight = Rect_MinimapArea.rect.height;

        float anchoredX = (normalizedPosition.x - 0.5f) * areaWidth;
        float anchoredY = (normalizedPosition.y - 0.5f) * areaHeight;

        icon.anchoredPosition = new Vector2(anchoredX, anchoredY);
    }
}
