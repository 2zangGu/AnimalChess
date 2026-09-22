using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 플레이어가 실제로 보유한 유닛 한 마리(런타임 상태).
    /// AnimalData는 "이런 동물이 있다"는 데이터일 뿐이고, 이 클래스는
    /// 상점에서 산 그 동물 개체 하나를 가리킨다 (벤치 칸에 저장됨).
    ///
    /// 전투에서 죽으면 isAlive = false로 표시되고, 다음 라운드 준비 시간에
    /// PlayerRoster.ProcessDeaths()가 이걸 보고 강등(3성→2성→1성)시키거나
    /// 1성이면 아예 없애버린다.
    /// </summary>
    public class UnitInstance
    {
        public AnimalData currentData;
        public bool isAlive = true;

        public UnitInstance(AnimalData data)
        {
            currentData = data;
        }

        public int StarLevel => currentData != null ? currentData.starLevel : 0;
        public string DisplayName => currentData != null ? currentData.displayName : "";
    }
}
