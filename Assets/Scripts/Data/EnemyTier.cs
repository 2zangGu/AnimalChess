namespace AnimalChess.Data
{
    /// <summary>
    /// 인간 적 유닛의 대략적인 기술 수준(무기/장비 컨셉)을 나타내는 분류.
    ///
    /// 실제 등장 시점은 이 티어가 아니라 EnemyUnitData의 minRound/maxRound가 직접 결정한다.
    /// 1라운드부터 26종이 점진적으로 섞여 들어오고 나가는 방식이라(EnemyRosterGenerator 참고),
    /// 특정 라운드에서 티어가 갑자기 뒤바뀌거나 별도의 "보스 전용" 유닛이 튀어나오지 않는다.
    /// 이 enum은 UI 표시나 로그 등에서 참고용으로 쓰는 느슨한 분류일 뿐이다.
    /// </summary>
    public enum EnemyTier
    {
        Hunter,      // 원시적인 무기(맨주먹/도끼/활/돌 등)를 쓰는 초반 잡졸
        Soldier,     // 총기/에너지 무기로 무장한 현대적 병력
        Mechanized,  // 메크/전함/전투기 등 최상위 병기급 유닛
    }
}
