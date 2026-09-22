using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;
using AnimalChess.Board;

namespace AnimalChess.Game
{
    /// <summary>
    /// 플레이어가 보유한 유닛들을 관리한다. 상점에서 산 유닛은 벤치(보관 칸) 9칸에 들어가고,
    /// 거기서 드래그로 보드의 내 존(player zone) 타일 위에 배치하면 그 칸으로 옮겨간다
    /// (UnitDragController가 이 클래스의 TryPlaceOnBoard/TryMoveOnBoard/TryReturnToBench를 호출한다).
    ///
    /// 한 유닛은 항상 벤치 칸 또는 보드 칸 둘 중 하나에만 있다(둘 다에 동시에 있지 않음).
    /// 실제로 전투에 참여하는 건 보드에 배치된 유닛뿐이라서, 골드 보너스 계산(CountAliveUnits)과
    /// 사망 처리(ProcessDeaths)도 보드 위의 유닛만 대상으로 한다.
    /// </summary>
    public class PlayerRoster : MonoBehaviour
    {
        public static PlayerRoster Instance { get; private set; }

        public const int BenchSize = 9;

        [Header("레벨별 배치 가능 유닛 수")]
        [Tooltip("1레벨일 때 보드에 배치할 수 있는 유닛 수. 레벨이 1 오를 때마다 이 값에서 1씩 늘어난다.\n" +
                 "(예: 이 값이 2면 1레벨=2마리, 2레벨=3마리, 3레벨=4마리...)")]
        [SerializeField] private int boardCapacityAtLevel1 = 2;

        public UnitInstance[] Bench { get; private set; } = new UnitInstance[BenchSize];

        private readonly Dictionary<HexCoord, UnitInstance> _boardUnits = new Dictionary<HexCoord, UnitInstance>();

        /// <summary>보드 위에 배치된 유닛들 (타일 좌표 -> 유닛). 읽기 전용으로만 노출한다.</summary>
        public IReadOnlyDictionary<HexCoord, UnitInstance> BoardUnits => _boardUnits;

        /// <summary>
        /// 지금 레벨에서 보드에 배치할 수 있는 최대 유닛 수. PlayerEconomy.Level을 기준으로 계산한다
        /// (PlayerEconomy가 없으면 1레벨 기준값을 그대로 쓴다). EnemySpawner도 이 값을 참고해서
        /// 적 웨이브 규모를 맞춘다.
        /// </summary>
        public int MaxBoardUnits
        {
            get
            {
                int level = PlayerEconomy.Instance != null ? PlayerEconomy.Instance.Level : 1;
                return boardCapacityAtLevel1 + Mathf.Max(0, level - 1);
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        public bool TryGetUnitAt(HexCoord coord, out UnitInstance unit) => _boardUnits.TryGetValue(coord, out unit);

        /// <summary>
        /// 벤치 칸(benchIndex)의 유닛을 보드 위의 내 존 타일(coord)에 배치한다.
        /// 그 타일에 이미 다른 유닛이 있으면 서로 자리를 맞바꾼다(그 유닛은 이 벤치 칸으로 돌아온다).
        /// 타일이 없거나 적 존이면 실패한다. 이미 자리를 맞바꾸는 게 아니라 새로 보드 위 유닛 수를
        /// 늘리는 배치인데 MaxBoardUnits(레벨별 한도)를 넘으면 실패한다.
        /// </summary>
        public bool TryPlaceOnBoard(int benchIndex, HexCoord coord)
        {
            if (benchIndex < 0 || benchIndex >= BenchSize) return false;
            var unit = Bench[benchIndex];
            if (unit == null) return false;
            if (BoardManager.Instance == null || !BoardManager.Instance.TryGetTile(coord, out HexTile tile)) return false;
            if (!tile.IsPlayerZone) return false;

            bool isSwap = _boardUnits.TryGetValue(coord, out var occupant) && occupant != unit;

            // 자리 맞바꿈이 아니라 빈 칸에 새로 놓는 경우에만 보드 위 유닛 수가 늘어나므로,
            // 그 경우에만 레벨별 배치 한도를 확인한다.
            if (!isSwap && _boardUnits.Count >= MaxBoardUnits) return false;

            if (isSwap)
            {
                // 이미 다른 유닛이 있으면, 그 유닛을 벤치의 이 자리로 돌려보낸다(자리 맞바꾸기).
                Bench[benchIndex] = occupant;
                occupant.boardCoord = null;
            }
            else
            {
                Bench[benchIndex] = null;
            }

            _boardUnits[coord] = unit;
            unit.boardCoord = coord;
            tile.IsOccupied = true;
            return true;
        }

        /// <summary>
        /// 보드 위의 유닛(from)을 다른 보드 타일(to)로 옮긴다.
        /// 대상 타일에 이미 다른 유닛이 있으면 서로 자리를 맞바꾼다. 같은 자리면 아무 일도 하지 않는다.
        /// </summary>
        public bool TryMoveOnBoard(HexCoord from, HexCoord to)
        {
            if (from == to) return true;
            if (!_boardUnits.TryGetValue(from, out var unit)) return false;
            if (BoardManager.Instance == null || !BoardManager.Instance.TryGetTile(to, out HexTile toTile)) return false;
            if (!toTile.IsPlayerZone) return false;

            if (_boardUnits.TryGetValue(to, out var occupant) && occupant != unit)
            {
                _boardUnits[from] = occupant;
                occupant.boardCoord = from;
            }
            else
            {
                _boardUnits.Remove(from);
                if (BoardManager.Instance.TryGetTile(from, out HexTile fromTile))
                {
                    fromTile.IsOccupied = false;
                }
            }

            _boardUnits[to] = unit;
            unit.boardCoord = to;
            toTile.IsOccupied = true;
            return true;
        }

        /// <summary>보드 위의 유닛(from)을 벤치의 빈 자리로 되돌린다. 벤치가 가득 차 있으면 실패한다.</summary>
        public bool TryReturnToBench(HexCoord from)
        {
            if (!_boardUnits.TryGetValue(from, out var unit)) return false;
            if (!TryInsertExistingUnit(unit)) return false;

            _boardUnits.Remove(from);
            unit.boardCoord = null;
            if (BoardManager.Instance != null && BoardManager.Instance.TryGetTile(from, out HexTile tile))
            {
                tile.IsOccupied = false;
            }
            return true;
        }

        /// <summary>
        /// 벤치의 두 칸(a, b)에 있는 유닛을 서로 맞바꾼다. 벤치 안에서 자유롭게 재배치할 때 쓴다.
        /// </summary>
        public bool TrySwapBenchSlots(int a, int b)
        {
            if (a < 0 || a >= BenchSize || b < 0 || b >= BenchSize) return false;
            if (a == b) return true;
            (Bench[a], Bench[b]) = (Bench[b], Bench[a]);
            return true;
        }

        /// <summary>
        /// 이미 존재하는 UnitInstance(별 단계/생존 여부 등 상태를 그대로 유지)를 벤치의 빈 자리에
        /// 끼워 넣는다. TryAddToBench와 달리 새 UnitInstance를 만들지 않는다 (보드->벤치 복귀용).
        /// </summary>
        private bool TryInsertExistingUnit(UnitInstance unit)
        {
            for (int i = 0; i < BenchSize; i++)
            {
                if (Bench[i] == null)
                {
                    Bench[i] = unit;
                    return true;
                }
            }
            return false;
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

        /// <summary>지금 살아있는 채로 보드에 배치된 유닛 수 (골드 보너스 계산용). 벤치에만 있는
        /// (아직 배치하지 않은) 유닛은 전투에 참여하지 않으므로 세지 않는다.</summary>
        public int CountAliveUnits()
        {
            int count = 0;
            foreach (var unit in _boardUnits.Values)
            {
                if (unit != null && unit.isAlive) count++;
            }
            return count;
        }

        /// <summary>
        /// 라운드 전투 후 죽은 것으로 표시된(isAlive = false) 보드 위 유닛들을 처리한다.
        /// 3성이면 2성으로, 2성이면 1성으로 강등시키고 다시 살아있는 상태로 되돌린다(그 타일에 그대로 남는다).
        /// 1성이었으면(더 내려갈 곳이 없으면) 보드에서 완전히 사라지고 그 타일도 다시 빈 칸이 된다.
        /// </summary>
        public void ProcessDeaths()
        {
            List<HexCoord> removed = null;
            foreach (var kvp in _boardUnits)
            {
                var unit = kvp.Value;
                if (unit == null || unit.isAlive) continue;

                if (unit.currentData != null && unit.currentData.previousEvolution != null)
                {
                    unit.currentData = unit.currentData.previousEvolution;
                    unit.isAlive = true;
                }
                else
                {
                    (removed ??= new List<HexCoord>()).Add(kvp.Key);
                }
            }

            if (removed == null) return;
            foreach (var coord in removed)
            {
                _boardUnits.Remove(coord);
                if (BoardManager.Instance != null && BoardManager.Instance.TryGetTile(coord, out HexTile tile))
                {
                    tile.IsOccupied = false;
                }
            }
        }

        /// <summary>
        /// 테스트용: 실제 전투 시스템이 생기기 전까지, 보드에 배치된 살아있는 유닛 중 하나를
        /// 무작위로 "전투 중 사망"으로 표시한다. RoundManager의 디버그 키(M)가 이걸 호출한다.
        /// </summary>
        public void DebugKillRandomUnit()
        {
            var candidates = new List<HexCoord>();
            foreach (var kvp in _boardUnits)
            {
                if (kvp.Value != null && kvp.Value.isAlive) candidates.Add(kvp.Key);
            }
            if (candidates.Count == 0) return;

            var coord = candidates[Random.Range(0, candidates.Count)];
            _boardUnits[coord].isAlive = false;
        }
    }
}
