using UnityEngine;

namespace AnimalChess.Data
{
    /// <summary>
    /// 인간 적 유닛 한 종의 데이터.
    /// 동물과 달리 합성/성장 시스템은 적용하지 않으며, 티어와 등장 라운드 범위만 가진다.
    /// AnimalData 와 동일하게 5개 스탯(UnitStats)만으로 표현해 전투 로직을 공유한다.
    ///
    /// 인스턴스 생성: 프로젝트 창에서 우클릭 → Create → AnimalChess → Enemy Unit Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "AnimalChess/Enemy Unit Data", order = 1)]
    public class EnemyUnitData : ScriptableObject
    {
        [Header("기본 정보")]
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject modelPrefab;

        [Header("티어 / 등장 라운드")]
        public EnemyTier tier;
        public int minRound = 1;
        public int maxRound = 5;

        [Header("전투 스탯")]
        public UnitStats baseStats;
    }
}
