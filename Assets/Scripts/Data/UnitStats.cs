using System;
using UnityEngine;

namespace AnimalChess.Data
{
    /// <summary>
    /// 모든 유닛(동물 / 인간 적)이 공유하는 5개 기본 전투 스탯.
    /// 개별 스킬 없이 이 5개 값만으로 유닛의 역할이 자연스럽게 결정된다.
    /// (예: HP+방어력이 높으면 탱커, 공격력+공속이 높으면 딜러, 사거리가 길면 후방형)
    /// </summary>
    [Serializable]
    public struct UnitStats
    {
        [Tooltip("체력")]
        public float hp;

        [Tooltip("공격력. 실제 피해 = 공격력 - 방어력 (최소 피해값 적용)")]
        public float attackPower;

        [Tooltip("방어력. 받는 피해를 감소시킨다")]
        public float defense;

        [Tooltip("초당 공격 횟수")]
        public float attackSpeed;

        [Tooltip("공격 가능 거리 (타일 기준)")]
        public float attackRange;
    }
}
