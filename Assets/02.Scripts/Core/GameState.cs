public enum GameState
{
    Ready = 0,            // 로비 대기 및 로딩 중
    Playing = 1,          // 필드 전투 및 생존 탐색
    EventPaused = 2,      // 역 도달 및 이벤트 (시간 정지)
    ExitSelected = 3,     // 출구 방향 선택 완료 후 재개 준비
    GameOver = 4          // 게임 오버
}