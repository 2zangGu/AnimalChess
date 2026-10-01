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

        [Header("라운드 진행에 따른 웨이브 규모")]
        [Tooltip("1라운드 기준 적 웨이브 마릿수. 플레이어가 레벨업을 전혀 안 해도 최소 이만큼은 나온다.")]
        public int baseWaveSize = 1;

        [Tooltip("이 라운드 수가 지날 때마다 웨이브에 적이 1마리씩 더 늘어난다(레벨을 안 올려도 라운드가 " +
                 "진행되면 자동으로 늘어남). 예: 2면 2라운드마다 1마리씩 증가. 0이면 라운드에 따른 증가 없음.\n" +
                 "(baseWaveSize=1, 이 값=2 기준: 1~2라운드 1마리 -> 3~4라운드 2마리 -> 5~6라운드 3마리 " +
                 "-> ... -> 29~30라운드 15마리로 늘어나는 커브. 라운드 후반부라 등장 가능한 적 종류 수 " +
                 "자체가 목표 마릿수보다 적어지면, 모자란 만큼 같은 종류를 반복 배치해서라도 목표 " +
                 "마릿수를 채운다(SizeToCapacity 참고) - 적 하나하나는 약해도 물량으로 난이도를 " +
                 "유지하는 방향.)")]
        public int roundsPerExtraEnemy = 2;

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

            // 적 웨이브 전체 마리 수는 라운드 진행에 따라 자연히 늘어나는 기본 웨이브 크기
            // (baseWaveSize + 라운드 보너스)를 그대로 쓴다.
            // (예전에는 "플레이어가 지금 레벨에서 배치할 수 있는 유닛 수(PlayerRoster.MaxBoardUnits)"와
            // 둘 중 더 큰 값을 썼는데, 그러면 1~2라운드처럼 라운드 커브가 의도적으로 적게 잡아둔
            // 구간에서도 레벨 기준 값(레벨1=2마리)이 더 커서 커브가 무시되는 문제가 있었다.
            // "1라운드 1마리 -> 2라운드마다 1마리씩 증가"라는 라운드 커브를 그대로 지키기 위해
            // 레벨 기준 하한선은 제거했다 - 플레이어가 아주 빠르게 레벨업하면 일시적으로 적보다
            // 아군이 많아질 수 있지만, 그건 플레이어가 골드를 경험치에 투자한 전략적 선택으로 본다.)
            // 근접/원거리 비율은 원래 eligible 풀의 비율을 최대한 그대로 유지한다. 라운드 후반부라
            // eligible 풀 자체가 목표 마릿수보다 적을 수 있으므로, 그럴 땐 SizeToCapacity가 같은
            // 종류를 반복 배치해서라도 목표 마릿수를 채운다.
            int roundBonus = roundsPerExtraEnemy > 0 ? (round - 1) / roundsPerExtraEnemy : 0;
            int capacity = baseWaveSize + roundBonus;
            SizeToCapacity(melee, ranged, capacity, round);

            // 근접은 앞줄(플레이어와 가까운 쪽)부터, 원거리는 뒷줄(플레이어와 먼 쪽)부터 채우되,
            // 각각 독립적으로 "그 줄의 가운데부터 좌우로 고르게" 퍼지는 순서를 쓴다. 그래야 마릿수가
            // 적을 때도 한쪽 구석(근접=전방 왼쪽, 원거리=후방 오른쪽)에 몰리지 않고 고르게 보인다.
            var meleeOrder = GetEnemyTilesOrdered(frontFirst: true);
            var rangedOrder = GetEnemyTilesOrdered(frontFirst: false);
            if (meleeOrder.Count == 0 && rangedOrder.Count == 0) return;

            var claimed = new HashSet<HexCoord>();

            int meleeIdx = 0;
            foreach (var data in melee)
            {
                while (meleeIdx < meleeOrder.Count && claimed.Contains(meleeOrder[meleeIdx].Coord)) meleeIdx++;
                if (meleeIdx >= meleeOrder.Count) break;
                PlaceUnit(meleeOrder[meleeIdx], data, claimed);
                meleeIdx++;
            }

            int rangedIdx = 0;
            foreach (var data in ranged)
            {
                while (rangedIdx < rangedOrder.Count && claimed.Contains(rangedOrder[rangedIdx].Coord)) rangedIdx++;
                if (rangedIdx >= rangedOrder.Count) break;
                PlaceUnit(rangedOrder[rangedIdx], data, claimed);
                rangedIdx++;
            }
        }

        /// <summary>
        /// melee/ranged 목록(등장 시작 라운드 순으로 이미 정렬돼 있음)을 원래 비율을 최대한
        /// 유지하면서 정확히 capacity마리가 되도록 맞춘다. 목표 마릿수가 원래 목록 크기보다 크면
        /// (라운드 후반부라 등장 가능한 적 종류 수 자체가 목표보다 모자랄 때) 같은 목록을 처음부터
        /// 다시 순환시켜 같은 종류를 반복 배치해서라도 capacity를 채운다. 목표가 더 작으면 라운드
        /// 번호에 따라 회전된 위치에서 targetCount개를 뽑는다(Resize 참고) - 그래야 후보 풀이 같아도
        /// 라운드마다 다른 조합이 나온다.
        /// </summary>
        private static void SizeToCapacity(List<EnemyUnitData> melee, List<EnemyUnitData> ranged, int capacity, int round)
        {
            capacity = Mathf.Max(0, capacity);
            int total = melee.Count + ranged.Count;
            if (total == 0) return;

            // melee.Count==0이면 비율도 자연히 0이 되므로, 원래 비어 있던 쪽에 목표치가 잘못
            // 배정되는 일은 없다.
            int meleeTarget = Mathf.Clamp(Mathf.RoundToInt(capacity * (melee.Count / (float)total)), 0, capacity);
            int rangedTarget = capacity - meleeTarget;

            Resize(melee, meleeTarget, round);
            Resize(ranged, rangedTarget, round);
        }

        /// <summary>
        /// list를 정확히 targetCount 길이로 맞춘다. 늘려야 하면 원래 목록을 처음부터 다시 순환하며
        /// 반복 추가하고(같은 종류가 여러 마리 중복 배치됨), 줄여야 하면 항상 앞쪽(가장 먼저 등장하는,
        /// 즉 가장 약한 쪽)만 남기는 대신 라운드 번호로 목록을 회전시켜서 targetCount개를 뽑는다.
        /// 이렇게 하면 같은 후보 풀 안에서도 라운드가 바뀔 때마다 다른 조합이 나와서(예: 1라운드는
        /// 후보가 1종류뿐이라 어쩔 수 없지만, 2라운드부터는 후보가 늘어난 만큼 매 라운드 다른 유닛이
        /// 섞여 나온다), "1, 2라운드 적이 항상 똑같다" 같은 정체 현상이 사라진다. 라운드 번호만으로
        /// 계산하므로 같은 라운드를 다시 봐도 결과는 항상 같다(결정적).
        /// </summary>
        private static void Resize(List<EnemyUnitData> list, int targetCount, int round)
        {
            targetCount = Mathf.Max(0, targetCount);
            if (list.Count == 0 || targetCount == list.Count) return;

            if (targetCount < list.Count)
            {
                int n = list.Count;
                int offset = ((round - 1) % n + n) % n;
                var rotated = new List<EnemyUnitData>(targetCount);
                for (int i = 0; i < targetCount; i++)
                {
                    rotated.Add(list[(offset + i) % n]);
                }
                list.Clear();
                list.AddRange(rotated);
                return;
            }

            int sourceCount = list.Count;
            for (int i = sourceCount; i < targetCount; i++)
            {
                list.Add(list[i % sourceCount]);
            }
        }

        private void PlaceUnit(HexTile tile, EnemyUnitData data, HashSet<HexCoord> claimed)
        {
            claimed.Add(tile.Coord);
            var unit = new EnemyUnitInstance(data) { boardCoord = tile.Coord };
            _boardUnits[tile.Coord] = unit;
            tile.IsOccupied = true;
        }

        /// <summary>
        /// 적 존 타일을 줄 단위로 순서대로(frontFirst=true면 앞줄->뒷줄, false면 뒷줄->앞줄) 반환하되,
        /// 같은 줄 안에서는 왼쪽부터 채우는 대신 "가운데 -> 좌우 번갈아 바깥쪽"으로 순서를 매긴다.
        /// 이렇게 하면 그 줄에 몇 마리만 놓여도 한쪽 구석에 몰리지 않고 중앙 위주로 고르게 퍼진다.
        /// </summary>
        private List<HexTile> GetEnemyTilesOrdered(bool frontFirst)
        {
            var rows = new SortedDictionary<int, List<HexTile>>();
            foreach (var tile in BoardManager.Instance.AllTiles)
            {
                if (tile.IsPlayerZone) continue;
                if (!rows.TryGetValue(tile.Coord.r, out var rowTiles))
                {
                    rowTiles = new List<HexTile>();
                    rows[tile.Coord.r] = rowTiles;
                }
                rowTiles.Add(tile);
            }

            var rowKeys = new List<int>(rows.Keys); // SortedDictionary라 이미 오름차순(앞줄->뒷줄) 정렬됨.
            if (!frontFirst) rowKeys.Reverse();

            var result = new List<HexTile>();
            foreach (var key in rowKeys)
            {
                var rowTiles = rows[key];
                rowTiles.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                result.AddRange(CenterOutOrder(rowTiles));
            }
            return result;
        }

        /// <summary>
        /// 왼쪽->오른쪽으로 정렬된 한 줄의 타일 목록을, 가운데 타일부터 시작해서 오른쪽/왼쪽을
        /// 번갈아 가며 바깥쪽으로 넓혀가는 순서로 재배열한다(가운데부터 고르게 퍼지는 배치용).
        /// </summary>
        private static List<HexTile> CenterOutOrder(List<HexTile> leftToRight)
        {
            int n = leftToRight.Count;
            var ordered = new List<HexTile>(n);
            if (n == 0) return ordered;

            int mid = (n - 1) / 2;
            ordered.Add(leftToRight[mid]);

            int left = mid - 1;
            int right = mid + 1;
            bool takeRight = true;
            while (left >= 0 || right < n)
            {
                if (takeRight && right < n)
                {
                    ordered.Add(leftToRight[right]);
                    right++;
                }
                else if (!takeRight && left >= 0)
                {
                    ordered.Add(leftToRight[left]);
                    left--;
                }
                else if (right < n)
                {
                    ordered.Add(leftToRight[right]);
                    right++;
                }
                else if (left >= 0)
                {
                    ordered.Add(leftToRight[left]);
                    left--;
                }
                takeRight = !takeRight;
            }
            return ordered;
        }

        /// <summary>
        /// 전투 중(CombatManager) 적 유닛이 자동으로 이동할 때 쓰는 저수준 이동. from에 있던 유닛을
        /// to로 그대로 옮기기만 한다(전투 로직이 이미 to가 비어 있고 유효한 타일인지 확인했다고 가정한다).
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
