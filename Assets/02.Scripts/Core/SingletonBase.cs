using UnityEngine;

public class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] 이미 인스턴스가 있어 이 컴포넌트를 제거합니다.");
            Destroy(this);
            return;
        }
        _instance = this as T;
        transform.SetParent(null);
    }
}
