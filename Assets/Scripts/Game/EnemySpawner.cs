using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;
using AnimalChess.Board;

namespace AnimalChess.Game
{
    /// <summary>
    /// 라운드가 시작되거나 바뀔 때, 그 라운드에 등장 가능한 적 유닛들(EnemyUnitData의
    /// minRound~maxRound 구간에 지금 라운드가 들어가는 유닛)을 자동으로 골라
    /// 보드의 적 존 타일 위에 배치한다.
    ///
    /// 배치 규칙: 사거리가 짧은(근접, meleeRangeThreshold 이하) 유닛은 적 존의 앞줄
    /// (플레이어 존과 가까운 프론트라인)부터, 사거리가 긴(원거리) 유닛은 뒷줄(플레이어에서
    /// 가장 먼 줄)부터 채워나간다. 실제 전투/공격 로직은 아직 없고, 이 스크립트는
    /// "이번 라운드에 누가 어디에 서 있는가"만 담당한다.
    ///
    /// Resources/Enemies 폴더의 EnemyUnitData 애셋들을 읽으므로, 먼저
    /// 'Tools > AnimalChess > 적 유닛 로스터(26종) 만들기'를 한 번 실행해둬야 한다.
    ///
    /// PlayerRoster/RoundManager 등과 같은 방식으로, 씬에는
    /// 'Tools > AnimalChess > 적 웨이브 스포너 만들기'로 한 번만 추가하면 된다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        private const string EnemyResourcesFolder = "Enemies";

        [Tooltip("이 값 이하의 사거리를 가진 유닛은 근접으로 보고 적 존의 앞줄에 배치한다.\n" +
                 "이 값을 넘으면 원거리로 보고 뒷줄에 배치한다.")]
        public float meleeRangeThreshold = 1.5f;

        private readonly Dictionary<HexCoord, EnemyUnitInstance> _boardUnits = new Dictionary<HexCoord, EnemyUnitInstance>();
        private EnemyUnitData[] _allEnemies;
        private int _lastSpawnedRound = -1;

        /// <summary>보드 위에 배치된 적 유닛들 (타일 좌표 -> 유닛). 읽기 전용으로만 노출한다.</summary>
        public IReadOnlyDictionary<HexCoord, EnemyUnitInstance> BoardUnits => _boardUnits;

        private void Awake()
        {
            Instance = this;
            _allEnemies = Resources.LoadAll<EnemyUnitData>(EnemyResourcesFolder);
        }

        private void Start()
        {
            // RoundManager.Instance / BoardManager.Instance는 각자의 Awake에서 세팅되므로,
            // 모든 Awake가 끝난 뒤인 Start 시점에 첫 웨이브를 배치한다.
            SpawnWaveForCurrentRound();
        }

        private void Update()
        {
            int round = RoundManager.Instance != null ? RoundManager.Instance.CurrentRound : 1;
            if (round != _lastSpawnedRound)
            {
                SpawnWaveForCurrentRound();
            }
        }

        /// <summary>
        /// 지금 라운드에 맞는 적 웨이브를 새로 계산해서 배치한다. 기존에 배치돼 있던 적은 모두 치운다.
        /// </summary>
        public void SpawnWaveForCurrentRound()
        {
            int round = RoundManager.Instance != null ? RoundManager.Instance.CurrentRound : 1;
            _lastSpawnedRound = round;

            ClearBoard();

            if (BoardManager.Instance == null || _allEnemies == null || _allEnemies.Length == 0) return;

            var eligible = new List<EnemyUnitData>();
            foreach (var data in _allEnemies)
            {
                if (data == null) continue;
                if (round >= data.minRound && round <= data.maxRound) eligible.Add(data);
            }
            if (eligible.Count == 0) return;

            // 등장 시작 라운드가 빠른(약한) 순 -> 이름순으로 정렬해서 배치 결과가 실행할 때마다 안정적으로 나오게 한다.
            eligible.Sort((a, b) =>
            {
                int byRound = a.minRound.CompareTo(b.minRound);
                return byRound != 0 ? byRound : string.CompareOrdinal(a.displayName, b.displayName);
            });

            var melee = new List<EnemyUnitData>();
            var ranged = new List<EnemyUnitData>();
            foreach (var data in eligible)
            {
                if (data.baseStats.attackRange <= meleeRangeThreshold) melee.Add(data);
                else ranged.Add(data);
            }

            // 적 웨이브 전체 마리 수는 플레이어가 지금 레벨에서 배치할 수 있는 유닛 수(PlayerRoster.MaxBoardUnits)를
            // 넘지 않게 잘라낸다. 근접/원거리 비율은 원래 eligible 풀의 비율을 최대한 그대로 유지한다.
            int capacity = PlayerRoster.Instance != null ? PlayerRoster.Instance.MaxBoardUnits : eligible.Count;
            TrimToCapacity(melee, ranged, capacity);

            var frontToBack = GetEnemyTilesFrontToBack();
            if (frontToBack.Count == 0) return;

            var claimed = new HashSet<HexCoord>();

            // 근접: 앞줄(적 존에서 플레이어와 가장 가까운 줄)부터 채운다.
            int frontIdx = 0;
            foreach (var data in melee)
            {
                while (frontIdx < frontToBack.Count && claimed.Contains(frontToBack[frontIdx].Coord)) frontIdx++;
                if (frontIdx >= frontToBack.Count) break;
                PlaceUnit(frontToBack[frontIdx], data, claimed);
                frontIdx++;
            }

            // 원거리: 뒷줄(적 존에서 플레이어와 가장 먼 줄)부터 채운다.
            int backIdx = frontToBack.Count - 1;
            foreach (var data in ranged)
            {
                while (backIdx >= 0 && claimed.Contains(frontToBack[backIdx].Coord)) backIdx--;
                if (backIdx < 0) break;
                PlaceUnit(frontToBack[backIdx], data, claimed);
                backIdx--;
            }
        }

        /// <summary>
        /// melee/ranged 목록(등장 시작 라운드 순으로 이미 정렬돼 있음)을 합쳐서 capacity마리를
        /// 넘지 않도록 뒤쪽(더 늦게 등장하는, 즉 더 강한 쪽)부터 잘라낸다. 두 목록의 비율은
        /// 원래 비율에 최대한 가깝게 유지한다.
        /// </summary>
        private static void TrimToCapacity(List<EnemyUnitData> melee, List<EnemyUnitData> ranged, int capacity)
        {
            capacity = Mathf.Max(0, capacity);
            int total = melee.Count + ranged.Count;
            if (total <= capacity) return;

            int meleeTake = total > 0 ? Mathf.RoundToInt(capacity * (melee.Count / (float)total)) : 0;
            meleeTake = Mathf.Clamp(meleeTake, 0, melee.Count);
            int rangedTake = Mathf.Clamp(capacity - meleeTake, 0, ranged.Count);

            // 반올림이나 한쪽 풀이 모자라서 아직 capacity를 다 못 채웠으면, 남는 자리를 다른 쪽에서 채운다.
            int remaining = capacity - meleeTake - rangedTake;
            if (remaining > 0 && meleeTake < melee.Count)
            {
                int extra = Mathf.Min(remaining, melee.Count - meleeTake);
                meleeTake += extra;
                remaining -= extra;
            }
            if (remaining > 0 && rangedTake < ranged.Count)
            {
                int extra = Mathf.Min(remaining, ranged.Count - rangedTake);
                rangedTake += extra;
            }

            if (melee.Count > meleeTake) melee.RemoveRange(meleeTake, melee.Count - meleeTake);
            if (ranged.Count > rangedTake) ranged.RemoveRange(rangedTake, ranged.Count - rangedTake);
        }

        private void PlaceUnit(HexTile tile, EnemyUnitData data, HashSet<HexCoord> claimed)
        {
            claimed.Add(tile.Coord);
            var unit = new EnemyUnitInstance(data) { boardCoord = tile.Coord };
            _boardUnits[tile.Coord] = unit;
            tile.IsOccupied = true;
        }

        /// <summary>
        /// 적 존 타일을 앞줄(플레이어와 가까운 쪽)부터 뒷줄 순서로 정렬해서 반환한다.
        /// 같은 줄 안에서는 왼쪽에서 오른쪽 순서(월드 x좌표 기준)로 정렬한다.
        /// </summary>
        private List<HexTile> GetEnemyTilesFrontToBack()
        {
            var tiles = new List<HexTile>();
            foreach (var tile in BoardManager.Instance.AllTiles)
            {
                if (!tile.IsPlayerZone) tiles.Add(tile);
            }
            tiles.Sort((a, b) =>
            {
                int rowCompare = a.Coord.r.CompareTo(b.Coord.r);
                return rowCompare != 0 ? rowCompare : a.transform.position.x.CompareTo(b.transform.position.x);
            });
            return tiles;
        }

        /// <summary>배치돼 있던 적 유닛을 모두 치우고, 그 타일들의 점유 상태도 초기화한다.</summary>
        public void ClearBoard()
        {
            if (BoardManager.Instance != null)
            {
                foreach (var coord in _boardUnits.Keys)
                {
                    if (BoardManager.Instance.TryGetTile(coord, out HexTile tile))
                    {
                        tile.IsOccupied = false;
                    }
                }
            }
            _boardUnits.Clear();
        }
    }
}
