using UnityEngine;

public class TestGameRun : MonoBehaviour
{
    private async void Start()
    {
        Debug.Log("데이터 로딩 시작...");

        // 1. DataManager가 데이터를 다 불러올 때까지 비동기로 기다립니다.
        await DataManager.Instance.LoadAllDatasAsync();

        // 2. 로드가 끝난 후, 몬스터 데이터를 꺼내서 확인해봅니다.
        MonsterData firstMonster = DataManager.Instance.GetData<MonsterData>("Monster_01");

        if (firstMonster != null)        {
            Debug.Log($"불러오기 성공! 이름: {firstMonster.MonsterName}, 체력: {firstMonster.Hp}, 공격력: {firstMonster.Atk}");
        }
    }
}
