using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 플레이어가 보유한 유닛들을 관리한다. 상점에서 산 유닛은 벤치(보관 칸) 9칸에 들어간다.
    ///
    /// 아직 보드에 유닛을 직접 배치하는 시스템이 없어서, 지금은 벤치에 있는 살아있는 유닛을
    /// 전부 "전투에 참여하는 내 유닛"으로 취급한다 (골드 보너스 계산, 사망 처리 대상 등).
    /// 나중에 실제 보드 배치 시스템이 생기면, UnitInstance에 "보드에 있는지" 여부를 추가해서
    /// 그 기준으로만 계산하도록 좁히면 된다.
    /// </summary>
    public class PlayerRoster : MonoBehaviour
    {
        public static PlayerRoster Instance { get; private set; }

        public const int BenchSize = 9;

        public UnitInstance[] Bench { get; private set; } = new UnitInstance[BenchSize];

        private void Awake()
        {
            Instance = this;
        }

        public bool IsBenchFull()
        {
            for (int i = 0; i < BenchSize; i++)
            {
                if (Bench[i] == null) return false;
            }
            return true;
        }

        /// <summary>빈 벤치 칸에 새로 산 동물을 추가한다. 벤치가 가득 차 있으면 false를 반환한다.</summary>
        public bool TryAddToBench(AnimalData animal)
        {
            for (int i = 0; i < BenchSize; i++)
            {
                if (Bench[i] == null)
                {
                    Bench[i] = new UnitInstance(animal);
                    return true;
                }
            }
            return false;
        }

        /// <summary>지금 살아있는 채로 벤치에 있는 유닛 수 (골드 보너스 계산용).</summary>
        public int CountAliveUnits()
        {
            int count = 0;
            for (int i = 0; i < BenchSize; i++)
            {
                if (Bench[i] != null && Bench[i].isAlive) count++;
            }
            return count;
        }

        /// <summary>
        /// 라운드 전투 후 죽은 것으로 표시된(isAlive = false) 유닛들을 처리한다.
        /// 3성이면 2성으로, 2성이면 1성으로 강등시키고 다시 살아있는 상태로 되돌린다.
        /// 1성이었으면(더 내려갈 곳이 없으면) 벤치에서 완전히 사라진다.
        /// </summary>
        public void ProcessDeaths()
        {
            for (int i = 0; i < BenchSize; i++)
            {
                var unit = Bench[i];
                if (unit == null || unit.isAlive) continue;

                if (unit.currentData != null && unit.currentData.previousEvolution != null)
                {
                    unit.currentData = unit.currentData.previousEvolution;
                    unit.isAlive = true;
                }
                else
                {
                    Bench[i] = null;
                }
            }
        }

        /// <summary>
        /// 테스트용: 실제 전투 시스템이 생기기 전까지, 벤치의 살아있는 유닛 중 하나를
        /// 무작위로 "전투 중 사망"으로 표시한다. RoundManager의 디버그 키(M)가 이걸 호출한다.
        /// </summary>
        public void DebugKillRandomUnit()
        {
            var candidates = new List<int>();
            for (int i = 0; i < BenchSize; i++)
            {
                if (Bench[i] != null && Bench[i].isAlive) candidates.Add(i);
            }
            if (candidates.Count == 0) return;

            int idx = candidates[Random.Range(0, candidates.Count)];
            Bench[idx].isAlive = false;
        }
    }
}
