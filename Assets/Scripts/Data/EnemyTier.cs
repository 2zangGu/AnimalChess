namespace AnimalChess.Data
{
    /// <summary>
    /// 인간 적 유닛이 등장하는 웨이브 티어.
    /// 라운드가 진행될수록 낮은 티어에서 높은 티어로 전환된다.
    /// </summary>
    public enum EnemyTier
    {
        Hunter,      // 1~5라운드   : 사냥꾼 / 밀렵꾼
        Soldier,     // 6~15라운드  : 정찰대 / 군인
        Mechanized,  // 16~25라운드 : 기계화 부대
        Boss         // 매 10라운드 등장하는 보스급 지휘관
    }
}
