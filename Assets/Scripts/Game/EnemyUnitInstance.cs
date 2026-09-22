using AnimalChess.Data;
using AnimalChess.Board;

namespace AnimalChess.Game
{
    /// <summary>
    /// 라운드 시작 시 EnemySpawner가 자동으로 배치하는 적(인간 웨이브) 유닛 한 마리(런타임 상태).
    /// EnemyUnitData는 "이런 적이 있다"는 데이터일 뿐이고, 이 클래스는 이번 라운드에
    /// 보드 위에 실제로 놓인 그 개체 하나를 가리킨다.
    ///
    /// 플레이어의 UnitInstance와 구조가 비슷하지만, 적은 강등/합성 개념이 없어서 훨씬 단순하다.
    /// </summary>
    public class EnemyUnitInstance
    {
        public EnemyUnitData currentData;
        public bool isAlive = true;

        /// <summary>이 유닛이 배치된 타일 좌표. EnemySpawner가 배치/정리하면서 관리한다.</summary>
        public HexCoord? boardCoord;

        public EnemyUnitInstance(EnemyUnitData data)
        {
            currentData = data;
        }

        public string DisplayName => currentData != null ? currentData.displayName : "";
    }
}
