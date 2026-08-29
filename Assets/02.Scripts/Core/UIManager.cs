using UnityEngine;
using System.Collections.Generic;

public class UIManager : SingletonBase<UIManager>
{
    [SerializeField] Canvas Canvas_BgRoot;
    [SerializeField] Canvas Canvas_MainRoot;
    [SerializeField] Canvas Canvas_ContentRoot;
    [SerializeField] Canvas Canvas_PopupRoot;
    [SerializeField] Canvas Canvas_VeryFrontRoot;

    private Dictionary<UIType, UIBase> _createdUIDic = new Dictionary<UIType, UIBase>();
    private HashSet<UIType> _openedUIDic = new HashSet<UIType>();

    private void Start()
    {
        this.ShowStartupUIOnGameStart();
    }

    public UIBase OpenUI(UIRootType uiRootType, UIType uiType, bool isInitialHide = false)
    {

        var openedUI = GetCreatedUI(uiRootType, uiType);

        if (openedUI == null)
        {
            return null;
        }

        bool isSetActiveOnOpen = (isInitialHide == false);
        if (_openedUIDic.Contains(uiType) == false)
        {
            openedUI.gameObject.SetActive(isSetActiveOnOpen);
            _openedUIDic.Add(uiType);
        }

        openedUI.transform.SetAsLastSibling();

        return openedUI;
    }

    public void CloseUI(UIRootType uiRootType, UIType uiType)
    {
        if (_openedUIDic.Contains(uiType) == true)
        {
            var openedUi = _createdUIDic[uiType];
            openedUi.gameObject.SetActive(false);
            _openedUIDic.Remove(uiType);
        }
    }

    public bool IsOpenUI(UIType uiType)
    {
        return _openedUIDic.Contains(uiType);
    }

    private Transform GetRootTransform(UIRootType uiRootType)
    {
        Transform root = null;
        switch (uiRootType)
        {
            case UIRootType.BackgroundUI:
                root = Canvas_BgRoot.transform;
                break;
            case UIRootType.MainUI:
                root = Canvas_MainRoot.transform;
                break;
            case UIRootType.ContentUI:
                root = Canvas_ContentRoot.transform;
                break;
            case UIRootType.PopupUI:
                root = Canvas_PopupRoot.transform;
                break;
            case UIRootType.VeryFrontUI:
                root = Canvas_VeryFrontRoot.transform;
                break;
        }
        return root;
    }

    private void CreateUI(UIRootType uiRootType, UIType uiType)
    {
        if (_createdUIDic.ContainsKey(uiType) == false)
        {
            string path = this.GetUIPath(uiRootType, uiType);
            GameObject loadedObj = Resources.Load<GameObject>(path);

            if (loadedObj == null)
            {
                Debug.LogError($"[UIManager] Resources 경로에서 UI 프리팹을 찾지 못했습니다. path=Resources/{path}");
                return;
            }

            Transform root = GetRootTransform(uiRootType);
            GameObject gObj = Instantiate(loadedObj, root);
            if (gObj != null)
            {
                var uiBase = gObj.GetComponent<UIBase>();

                if (uiBase == null)
                {
                    Debug.LogError($"[UIManager] {uiType} 프리팹({path})에 UIBase 컴포넌트가 없습니다.");
                    Destroy(gObj);
                    return;
                }

                _createdUIDic.Add(uiType, uiBase);
            }
        }
    }

    private UIBase GetCreatedUI(UIRootType uiRootType, UIType uiType)
    {
        if (_createdUIDic.ContainsKey(uiType) == false)
        {
            CreateUI(uiRootType, uiType);
        }

        _createdUIDic.TryGetValue(uiType, out var uiBase);
        return uiBase;
    }

    public UIBase GetOpenedUI(UIRootType uiRootType, UIType uiType)
    {
        return GetCreatedUI(uiRootType, uiType);
    }

    public UIBase OpenContentUI(UIType uiType, bool isInitialHide = false)
    {
        return OpenUI(UIRootType.ContentUI, uiType, isInitialHide);
    }

    public UIBase OpenPopupUI(UIType uiType, bool isInitialHide = false)
    {
        return OpenUI(UIRootType.PopupUI, uiType, isInitialHide);
    }

    public UIBase OpenMainUI(UIType uiType, bool isInitialHide = false)
    {
        return OpenUI(UIRootType.MainUI, uiType, isInitialHide);
    }

    public void CloseContentUI(UIType uiType)
    {
        CloseUI(UIRootType.ContentUI, uiType);
    }

    public void ClosePopupUI(UIType uiType)
    {
        CloseUI(UIRootType.PopupUI, uiType);
    }

    public void CloseMainUI(UIType uiType)
    {
        CloseUI(UIRootType.MainUI, uiType);
    }
}
