namespace AnimalChess.Data
{
    /// <summary>
    /// 동물이 주로 살아가는 서식지.
    /// 종족 시너지가 스탯 중심이라면, 서식지 시너지는 전투 방식(이동/디버프 등)에
    /// 영향을 주는 방향으로 차별화한다.
    /// </summary>
    public enum Habitat
    {
        Forest,     // 숲   - 2/4/6: 아군 최대 HP 증가
        Sea,        // 바다 - 2/4/6: 공격 속도 증가
        Swamp,      // 늪   - 2/4/6: 적 공격 속도 감소
        Desert,     // 사막 - 2/4/6: 받는 피해 감소
        Grassland,  // 초원 - 2/4/6: 전투 시작 시 아군 이동(전진 → 적 후방 기습)
        Tundra      // 극지 - 2/4/6: 전투 초반 적 공격 속도 감소
    }
}
