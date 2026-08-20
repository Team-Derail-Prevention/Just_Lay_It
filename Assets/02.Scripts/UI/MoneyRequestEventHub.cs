using UnityEngine;
using System;

public class MoneyRequestEventHub : SingletonBase<MoneyRequestEventHub>
{
    public event Action<int, Action<bool>> OnRequestSpendMoney;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void RequestSpendMoney(int amount, Action<bool> onResult)
    {
        if (OnRequestSpendMoney == null)
        {
            Debug.LogWarning("[MoneyRequestEventHub] 구독하는 외부 시스템이 없습니다. 재화 검증이 항상 실패로 처리됩니다.");
            onResult?.Invoke(false);
            return;
        }

        OnRequestSpendMoney.Invoke(amount, onResult);
    }
}
