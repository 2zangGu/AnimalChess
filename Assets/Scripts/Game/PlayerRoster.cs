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

        [Header("라운드별 배치 가능 유닛 수")]
        [Tooltip("1라운드 기준 보드에 배치할 수 있는 유닛 수. 레벨이 아니라 라운드 번호로 계산한다.\n" +
                 "(예: 이 값이 1이고 roundsPerExtraCapacity가 2면 1~2라운드=1마리, 3~4라운드=2마리, 5~6라운드=3마리...로 늘어난다.)")]
        [SerializeField] private int boardCapacityAtRound1 = 1;

        [Tooltip("이 라운드 수가 지날 때마다 배치 가능 유닛 수가 1씩 늘어난다. 예: 2면 2라운드마다 1마리씩 증가.\n" +
                 "(EnemySpawner.roundsPerExtraEnemy와 같은 방식 - 적 웨이브 증가 커브와 맞춰 쓰는 걸 추천한다.)")]
        [SerializeField] private int roundsPerExtraCapacity = 2;

        public UnitInstance[] Bench { get; private set; } = new UnitInstance[BenchSize];

        private readonly Dictionary<HexCoord, UnitInstance> _boardUnits = new Dictionary<HexCoord, UnitInstance>();

        /// <summary>보드 위에 배치된 유닛들 (타일 좌표 -> 유닛). 읽기 전용으로만 노출한다.</summary>
        public IReadOnlyDictionary<HexCoord, UnitInstance> BoardUnits => _boardUnits;

        /// <summary>
        /// 지금 라운드에서 보드에 배치할 수 있는 최대 유닛 수. RoundManager.CurrentRound를 기준으로 계산한다
        /// (RoundManager가 없으면 1라운드 기준값을 그대로 쓴다). 레벨이 아니라 라운드 번호로 직접 계산해서,
        /// "1~2라운드=1마리, 3~4라운드=2마리, 5~6라운드=3마리..."처럼 라운드 진행에 딱 맞춰 늘어난다
        /// (EnemySpawner의 적 웨이브 증가 커브와 같은 공식).
        /// </summary>
        public int MaxBoardUnits
        {
            get
            {
                int round = RoundManager.Instance != null ? RoundManager.Instance.CurrentRound : 1;
                int roundBonus = roundsPerExtraCapacity > 0 ? (round - 1) / roundsPerExtraCapacity : 0;
                return boardCapacityAtRound1 + roundBonus;
            }
        }

        // 전투가 시작되는 순간(그 준비 단계에서 마지막으로 배치돼 있던 상태)의 보드 배치를 기억해둔다.
        // 전투 중에는 유닛들이 자동으로 이리저리 움직이므로, 전투가 끝나면 이 스냅샷을 기준으로
        // 다음 준비 단계를 "이번 전투 전에 배치했던 자리"로 되돌려준다.
        private readonly Dictionary<UnitInstance, HexCoord> _prepPhaseBoardSnapshot = new Dictionary<UnitInstance, HexCoord>();

        private void Awake()
        {
            Instance = this;
        }

        public bool TryGetUnitAt(HexCoord coord, out UnitInstance unit) => _boardUnits.TryGetValue(coord, out unit);

        /// <summary>
        /// 지금 보드 위 배치를 기억해둔다. RoundManager가 준비 단계를 끝내고 전투를 시작하는 순간
        /// (Start 버튼을 누르거나 준비 시간이 다 됐을 때) 호출해서, "이번 전투를 시작하기 직전에
        /// 플레이어가 배치했던 자리"를 남겨둔다.
        /// </summary>
        public void SnapshotBoardPositions()
        {
            _prepPhaseBoardSnapshot.Clear();
            foreach (var kvp in _boardUnits)
            {
                _prepPhaseBoardSnapshot[kvp.Value] = kvp.Key;
            }
        }

        /// <summary>
        /// SnapshotBoardPositions()가 기억해둔 자리로, 아직 보드에 살아있는 유닛들을 되돌린다.
        /// RoundManager.EndRound가 ProcessDeaths() 직후(다음 준비 단계가 시작되기 전)에 호출해서,
        /// 전투 중 자동 이동으로 흐트러진 배치를 "그 전 준비 단계 때 놓았던 자리"로 복원해준다.
        ///
        /// 전투 중 죽어서 완전히 사라졌거나(강등 없이 제거) 진화로 다른 유닛(새 UnitInstance)으로
        /// 교체된 경우엔 스냅샷에 있던 유닛 참조를 더 이상 찾을 수 없으므로 자연히 건너뛴다.
        /// 자리가 서로 얽혀 있어도(예: 두 유닛이 전투 중 자리를 바꾼 경우) 안전하게 복원하기 위해,
        /// 먼저 대상 유닛들을 전부 보드에서 내려(점유 해제) 자리를 비운 다음 원래 자리에 다시 놓는다.
        /// </summary>
        public void RestorePrepPhasePositions()
        {
            if (_prepPhaseBoardSnapshot.Count == 0) return;

            List<(UnitInstance unit, HexCoord coord)> toRestore = null;
            foreach (var kvp in _prepPhaseBoardSnapshot)
            {
                var unit = kvp.Key;
                if (unit == null || !unit.isAlive || !unit.boardCoord.HasValue) continue;
                if (!_boardUnits.TryGetValue(unit.boardCoord.Value, out var atCoord) || atCoord != unit) continue;

                (toRestore ??= new List<(UnitInstance, HexCoord)>()).Add((unit, kvp.Value));
            }

            _prepPhaseBoardSnapshot.Clear();
            if (toRestore == null) return;

            // 1단계: 대상 유닛들을 지금 자리에서 전부 내린다(점유 해제).
            foreach (var (unit, _) in toRestore)
            {
                var current = unit.boardCoord.Value;
                _boardUnits.Remove(current);
                if (BoardManager.Instance != null && BoardManager.Instance.TryGetTile(current, out HexTile currentTile))
                {
                    currentTile.IsOccupied = false;
                }
            }

            // 2단계: 원래(전투 시작 직전) 있던 자리에 다시 놓는다.
            foreach (var (unit, originalCoord) in toRestore)
            {
                _boardUnits[originalCoord] = unit;
                unit.boardCoord = originalCoord;
                if (BoardManager.Instance != null && BoardManager.Instance.TryGetTile(originalCoord, out HexTile originalTile))
                {
                    originalTile.IsOccupied = true;
                }
            }
        }

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
            TryEvolveAllDuringPrepOnly();
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
            TryEvolveAllDuringPrepOnly();
            return true;
        }

        /// <summary>
        /// 전투 중(CombatManager) 유닛이 자동으로 이동할 때 쓰는 저수준 이동. TryMoveOnBoard와 달리
        /// 자리 맞바꿈이나 "내 존인지" 같은 검사를 하지 않고, from에 있던 유닛을 to로 그대로 옮긴다
        /// (전투 로직이 이미 to가 비어 있고 유효한 타일인지 확인했다고 가정한다).
        /// </summary>
        public void CombatMoveUnit(HexCoord from, HexCoord to)
        {
            if (!_boardUnits.TryGetValue(from, out var unit)) return;

            _boardUnits.Remove(from);
            _boardUnits[to] = unit;
            unit.boardCoord = to;

            if (BoardManager.Instance != null)
            {
                if (BoardManager.Instance.TryGetTile(from, out HexTile fromTile)) fromTile.IsOccupied = false;
                if (BoardManager.Instance.TryGetTile(to, out HexTile toTile)) toTile.IsOccupied = true;
            }
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
            TryEvolveAllDuringPrepOnly();
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
                    TryEvolveAllDuringPrepOnly();
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
        /// 밸런스상 전투에서 죽었다고 강등되거나 완전히 사라지는 건 너무 가혹하므로, 강등/제거 없이
        /// 그냥 다시 살아있는 상태로 되돌린다. 원래 성장 단계(성 레벨) 그대로 그 타일에 남는다.
        /// </summary>
        public void ProcessDeaths()
        {
            foreach (var kvp in _boardUnits)
            {
                var unit = kvp.Value;
                if (unit == null || unit.isAlive) continue;
                unit.isAlive = true;
            }

            // 부활 자체는 강등/제거를 일으키지 않지만, 상점 구매 등 다른 경로에서 밀려있던
            // 합성(진화) 체크가 남아있을 수 있으므로 안전하게 한 번 더 확인해준다.
            TryEvolveAll();
        }

        /// <summary>
        /// 벤치+보드를 합쳐서 봤을 때, 같은 성장 단계(같은 AnimalData 애셋)의 유닛이 3마리 모인
        /// 조합이 있는 한 계속 반복해서 합성한다(3성까지 연쇄적으로 이어질 수 있음).
        /// 유닛의 종류(동물 species)는 상관없이 모든 유닛에 동일한 규칙으로 적용된다.
        /// </summary>
        private void TryEvolveAll()
        {
            while (TryEvolveOnce()) { }
        }

        /// <summary>
        /// 상점 구매/드래그처럼 플레이어가 직접 하는 행동 뒤에 붙는 진화 체크. 상점/드래그는 전투가
        /// 진행되는 동안에도 계속 조작할 수 있어서(RoundManager.IsPreparing과 무관하게 언제든
        /// 호출될 수 있음), 이 체크를 그냥 TryEvolveAll()로 두면 전투 중에 3마리가 모이는 순간
        /// 바로 진화해버리는 문제가 있었다. 전투 중에는 합성을 미뤄뒀다가, 그 라운드가 끝나고
        /// RoundManager.EndRound가 PlayerRoster.ProcessDeaths를 호출하는 시점(그 안에서 다시
        /// TryEvolveAll()을 무조건 호출한다)에 한꺼번에 처리되게 한다. RoundManager가 아직 없으면
        /// (예: 테스트 씬) 안전하게 그냥 바로 진화시킨다.
        /// </summary>
        private void TryEvolveAllDuringPrepOnly()
        {
            if (RoundManager.Instance != null && !RoundManager.Instance.IsPreparing) return;
            TryEvolveAll();
        }

        private readonly struct MergeSlot
        {
            public readonly bool isBoard;
            public readonly int benchIndex;
            public readonly HexCoord coord;

            public MergeSlot(int benchIndex)
            {
                isBoard = false;
                this.benchIndex = benchIndex;
                coord = default;
            }

            public MergeSlot(HexCoord coord)
            {
                isBoard = true;
                this.coord = coord;
                benchIndex = -1;
            }
        }

        /// <summary>
        /// 벤치+보드를 한 번 훑어서, 같은 AnimalData(같은 성장 단계) 유닛이 3마리 이상 모인
        /// 그룹을 찾으면 그중 3마리를 지우고 nextEvolution 데이터의 새 유닛 1마리로 바꾼다.
        /// 합성된 자리는 3마리 중 보드에 배치돼 있던 자리를 우선으로 남겨서(전투 대형이 흐트러지지
        /// 않게), 전부 벤치에만 있었으면 벤치 자리를 그대로 쓴다. 한 번에 한 그룹만 처리하고
        /// true를 반환하므로, 호출하는 쪽(TryEvolveAll)이 반복 호출해서 연쇄 진화(1성 9마리 →
        /// 2성 3마리 → 3성 1마리 같은 경우)까지 자연스럽게 처리된다.
        /// </summary>
        private bool TryEvolveOnce()
        {
            var groups = new Dictionary<AnimalData, List<MergeSlot>>();

            for (int i = 0; i < BenchSize; i++)
            {
                var unit = Bench[i];
                if (unit == null || unit.currentData == null || unit.currentData.nextEvolution == null) continue;
                if (!groups.TryGetValue(unit.currentData, out var list))
                {
                    list = new List<MergeSlot>();
                    groups[unit.currentData] = list;
                }
                list.Add(new MergeSlot(i));
            }

            foreach (var kvp in _boardUnits)
            {
                var unit = kvp.Value;
                if (unit == null || !unit.isAlive || unit.currentData == null || unit.currentData.nextEvolution == null) continue;
                if (!groups.TryGetValue(unit.currentData, out var list))
                {
                    list = new List<MergeSlot>();
                    groups[unit.currentData] = list;
                }
                list.Add(new MergeSlot(kvp.Key));
            }

            foreach (var kvp in groups)
            {
                var slots = kvp.Value;
                if (slots.Count < 3) continue;

                var evolvedData = kvp.Key.nextEvolution;

                // 보드에 있던 자리가 있으면 그 자리를 남겨서 전투 대형을 유지한다.
                int keepIndex = slots.FindIndex(s => s.isBoard);
                if (keepIndex < 0) keepIndex = 0;
                var keepSlot = slots[keepIndex];

                // keepSlot 말고 나머지에서 2마리만 더 지운다 (keepSlot 자리는 새 유닛으로 교체됨).
                int extraRemoved = 0;
                for (int i = 0; i < slots.Count && extraRemoved < 2; i++)
                {
                    if (i == keepIndex) continue;
                    var slot = slots[i];
                    if (slot.isBoard)
                    {
                        _boardUnits.Remove(slot.coord);
                        if (BoardManager.Instance != null && BoardManager.Instance.TryGetTile(slot.coord, out HexTile tile))
                        {
                            tile.IsOccupied = false;
                        }
                    }
                    else
                    {
                        Bench[slot.benchIndex] = null;
                    }
                    extraRemoved++;
                }

                var evolvedUnit = new UnitInstance(evolvedData);
                if (keepSlot.isBoard)
                {
                    evolvedUnit.boardCoord = keepSlot.coord;
                    _boardUnits[keepSlot.coord] = evolvedUnit;
                }
                else
                {
                    Bench[keepSlot.benchIndex] = evolvedUnit;
                }

                Debug.LogWarning($"[PlayerRoster] 진화: {kvp.Key.displayName} {kvp.Key.starLevel}성 3마리 -> " +
                                  $"{evolvedData.displayName} {evolvedData.starLevel}성 1마리");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 유닛 한 마리를 팔았을 때 받는 골드를 계산한다.
        /// - 1성: 그 유닛의 코스트 그대로 (상점에서 살 때 낸 돈만큼 그대로 돌려받음).
        /// - 2성/3성: 그 별 단계까지 합성하는 데 실제로 들어간 1성 마릿수(3^(별단계-1))만큼의
        ///   코스트 총합에서 1을 뺀 값. 예: 코스트1 2성은 1성 3마리(3원) 합성해서 만드니 3-1=2원,
        ///   코스트1 3성은 1성 9마리(9원)가 필요하니 9-1=8원.
        /// </summary>
        public static int GetSellPrice(UnitInstance unit)
        {
            if (unit == null || unit.currentData == null) return 0;

            int baseCost = unit.currentData.cost;
            int star = Mathf.Clamp(unit.currentData.starLevel, 1, 3);
            int investedCost = baseCost * (int)Mathf.Pow(3, star - 1); // 1성=1배, 2성=3배, 3성=9배

            return star <= 1 ? investedCost : investedCost - 1;
        }

        /// <summary>
        /// 벤치 칸(benchIndex)의 유닛을 판다: 그 칸에서 완전히 없애고 GetSellPrice만큼 골드를 지급한다.
        /// </summary>
        public bool TrySellFromBench(int benchIndex, out int soldPrice)
        {
            soldPrice = 0;
            if (benchIndex < 0 || benchIndex >= BenchSize) return false;
            var unit = Bench[benchIndex];
            if (unit == null) return false;

            soldPrice = GetSellPrice(unit);
            Bench[benchIndex] = null;
            PlayerEconomy.Instance?.AddGold(soldPrice);
            return true;
        }

        /// <summary>
        /// 보드 위의 유닛(coord)을 판다: 그 자리에서 완전히 없애고(타일도 다시 빈 칸으로)
        /// GetSellPrice만큼 골드를 지급한다.
        /// </summary>
        public bool TrySellFromBoard(HexCoord coord, out int soldPrice)
        {
            soldPrice = 0;
            if (!_boardUnits.TryGetValue(coord, out var unit)) return false;

            soldPrice = GetSellPrice(unit);
            _boardUnits.Remove(coord);
            if (BoardManager.Instance != null && BoardManager.Instance.TryGetTile(coord, out HexTile tile))
            {
                tile.IsOccupied = false;
            }
            PlayerEconomy.Instance?.AddGold(soldPrice);
            return true;
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
