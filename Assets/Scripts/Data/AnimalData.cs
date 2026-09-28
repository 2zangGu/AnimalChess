using UnityEngine;

namespace AnimalChess.Data
{
    /// <summary>
    /// 동물 한 종(정확히는 성장 계통의 한 단계)에 대한 데이터.
    ///
    /// 강아지(1성) → 웰시 코기(2성) → 도사견(3성) 처럼
    /// 성장 계통의 각 단계는 서로 다른 AnimalData 애셋으로 만들고,
    /// previousEvolution / nextEvolution 필드로 체인을 연결한다.
    /// 같은 단계 3마리를 합성하면 nextEvolution 이 가리키는 애셋으로 교체되는 식으로 구현하면 된다.
    ///
    /// 인스턴스 생성: 프로젝트 창에서 우클릭 → Create → AnimalChess → Animal Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimal", menuName = "AnimalChess/Animal Data", order = 0)]
    public class AnimalData : ScriptableObject
    {
        [Header("기본 정보")]
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject modelPrefab;

        [Header("분류")]
        public Species species;
        public Habitat habitat;

        [Header("코스트 (1~5)")]
        [Range(1, 5)] public int cost = 1;

        [Header("성장 단계 (1~3)")]
        [Range(1, 3)] public int starLevel = 1;

        [Tooltip("성장 계통에서 이 동물의 이전 단계. 1성이면 비워둔다.")]
        public AnimalData previousEvolution;

        [Tooltip("성장 계통에서 이 동물의 다음 단계. 3성이면 비워둔다.")]
        public AnimalData nextEvolution;

        [Header("전투 스탯")]
        public UnitStats baseStats;

        /// <summary>
        /// 이 동물이 속한 성장 계통의 "뿌리"(1성) AnimalData를 찾는다. 서식지/종족 시너지
        /// 마릿수를 셀 때 "같은 계열"을 구별하는 기준으로 쓴다 - 예를 들어 강아지(1성)와
        /// 웰시 코기(2성)는 previousEvolution 체인을 타고 올라가면 같은 뿌리(강아지)가 나오므로
        /// 같은 계열로 취급해서, 시너지 마릿수는 실제 몇 마리든 상관없이 1로만 센다.
        /// </summary>
        public AnimalData GetFamilyRoot()
        {
            var current = this;
            int guard = 0; // previousEvolution이 실수로 순환 참조되는 경우를 대비한 안전장치.
            while (current.previousEvolution != null && guard < 10)
            {
                current = current.previousEvolution;
                guard++;
            }
            return current;
        }
    }
}
