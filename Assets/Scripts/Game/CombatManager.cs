using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;
using AnimalChess.Board;

namespace AnimalChess.Game
{
    /// <summary>
    /// 준비 단계가 끝나면(RoundManager.IsPreparing: true → false) 보드 위에 배치된 내 유닛과
    /// 적 유닛들이 자동으로 서로를 향해 다가가 싸우는 실제 전투를 시뮬레이션한다.
    ///
    /// 규칙(전부 기존 UnitStats 5개 값 - 체력/공격력/방어력/공속/사거리 - 만으로 결정되는
    /// 심플한 오토배틀 로직):
    /// - 매 틱마다 각 유닛은 "아직 살아있는 적 중 가장 가까운 대상"을 고른다.
    /// - 대상이 자신의 사거리(attackRange, 타일 단위) 안에 있으면 공격 쿨다운을 확인해서 공격한다
    ///   (실제 피해 = 공격력 - 방어력, 최소 1). 공격할 때마다 AttackEffects로 근접/원거리 이펙트를
    ///   재생한다(어떤 이펙트가 나올지는 그 유닛의 종족/서식지/티어/사거리/별 단계로 자동 결정됨).
    /// - 사거리 밖이면 대상 쪽으로 한 칸(가장 가까워지는, 비어 있는 이웃 타일) 다가간다.
    /// - 체력이 0 이하가 되면 죽은 것으로 표시하고 비주얼을 회색으로 바꾼다(실제 강등/제거는
    ///   라운드가 끝날 때 PlayerRoster.ProcessDeaths / 다음 웨이브 스폰이 처리한다).
    /// - 한쪽 진영이 전멸하면 전투 종료: 내 진영이 이겼으면 RoundManager.EndRound(true), 아니면 false.
    ///
    /// 체력/공격 쿨다운 등 전투 중에만 필요한 상태는 UnitInstance/EnemyUnitInstance를 건드리지 않고
    /// 이 클래스가 별도로 들고 있다가, 전투가 끝나면 버린다(라운드마다 새로 시작).
    /// RoundManager가 Awake에서 자동으로 붙여주므로 씬에 직접 추가할 필요는 없다.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Tooltip("전투 중 유닛이 한 칸 이동하는 데 걸리는 시간(초). 이 값마다 한 번씩만 이동 판정을 한다.")]
        public float moveTickSeconds = 0.35f;

        /// <summary>지금 전투가 실제로 진행 중인지. RoundHUD가 화면에 표시하는 데 쓴다.</summary>
        public bool IsBattleActive => _battleActive;

        /// <summary>지금 전투에서 살아있는 아군/적 수. RoundHUD가 "아군 N · 적 M"으로 보여주는 데 쓴다.</summary>
        public int AlivePlayerCount { get; private set; }
        public int AliveEnemyCount { get; private set; }

        private class Combatant
        {
            public HexCoord coord;
            public UnitStats stats;
            public float currentHp;
            public float attackCooldown;
            public bool isPlayerSide;
            public bool isAlive = true;
            public UnitInstance playerUnit;      // isPlayerSide == true 일 때만 채워짐
            public EnemyUnitInstance enemyUnit;   // isPlayerSide == false 일 때만 채워짐
            public Species? species;
            public Habitat? habitat;
            public EnemyTier? tier;
            public int starLevel;
        }

        private readonly List<Combatant> _combatants = new List<Combatant>();
        private readonly Dictionary<HexCoord, Combatant> _byCoord = new Dictionary<HexCoord, Combatant>();
        private List<Combatant> _snapshotBuffer;
        private float _moveTickTimer;
        private bool _battleActive;
        private bool _wasPreparing = true;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            var round = RoundManager.Instance;
            if (round == null) return;

            // 준비 단계 -> 전투 단계로 막 넘어간 순간을 감지해서 전투를 시작한다.
            if (_wasPreparing && !round.IsPreparing)
            {
                Debug.LogWarning("[CombatManager] 준비 단계 종료 감지 - 전투를 시작합니다.");
                BeginBattle();
            }
            _wasPreparing = round.IsPreparing;

            if (!_battleActive) return;

            bool isGameOver = PlayerLives.Instance != null && PlayerLives.Instance.IsGameOver;
            if (isGameOver)
            {
                _battleActive = false;
                return;
            }

            TickBattle(Time.deltaTime);
        }

        /// <summary>
        /// 지금 보드 위(양쪽 진영)에 있는 살아있는 유닛들을 모아 전투 상태를 새로 만든다.
        /// 어느 한쪽이라도 유닛이 없으면 시뮬레이션 없이 바로 라운드 결과를 처리한다.
        /// </summary>
        private void BeginBattle()
        {
            _combatants.Clear();
            _byCoord.Clear();
            _moveTickTimer = 0f;

            if (PlayerRoster.Instance != null)
            {
                foreach (var kvp in PlayerRoster.Instance.BoardUnits)
                {
                    var unit = kvp.Value;
                    if (unit == null || !unit.isAlive || unit.currentData == null) continue;

                    var c = new Combatant
                    {
                        coord = kvp.Key,
                        stats = unit.currentData.baseStats,
                        currentHp = unit.currentData.baseStats.hp,
                        isPlayerSide = true,
                        playerUnit = unit,
                        species = unit.currentData.species,
                        habitat = unit.currentData.habitat,
                        starLevel = unit.StarLevel,
                    };
                    _combatants.Add(c);
                    _byCoord[c.coord] = c;
                }
            }

            if (EnemySpawner.Instance != null)
            {
                foreach (var kvp in EnemySpawner.Instance.BoardUnits)
                {
                    var unit = kvp.Value;
                    if (unit == null || !unit.isAlive || unit.currentData == null) continue;

                    var c = new Combatant
                    {
                        coord = kvp.Key,
                        stats = unit.currentData.baseStats,
                        currentHp = unit.currentData.baseStats.hp,
                        isPlayerSide = false,
                        enemyUnit = unit,
                        tier = unit.currentData.tier,
                        starLevel = 1,
                    };
                    _combatants.Add(c);
                    _byCoord[c.coord] = c;
                }
            }

            int playerCount = 0, enemyCount = 0;
            foreach (var c in _combatants)
            {
                if (c.isPlayerSide) playerCount++; else enemyCount++;
            }
            AlivePlayerCount = playerCount;
            AliveEnemyCount = enemyCount;

            Debug.LogWarning($"[CombatManager] 전투 시작: 아군 {playerCount}마리 vs 적 {enemyCount}마리");

            // 한쪽이라도 배치된 유닛이 없으면 굳이 틱을 돌릴 필요 없이 즉시 결과 처리.
            // (예: 보드에 유닛을 하나도 배치하지 않고 Start를 누르면 아군 0마리로 바로 패배 처리된다.)
            if (playerCount == 0 || enemyCount == 0)
            {
                Debug.LogWarning($"[CombatManager] 한쪽 진영에 유닛이 없어 전투 없이 즉시 라운드 결과를 처리합니다 " +
                          $"(아군 {playerCount}마리, 적 {enemyCount}마리). 보드에 유닛을 배치했는지 확인해주세요.");
                _battleActive = false;
                RoundManager.Instance?.EndRound(won: playerCount > 0);
                return;
            }

            _battleActive = true;
        }

        private void TickBattle(float deltaTime)
        {
            foreach (var c in _combatants)
            {
                if (c.isAlive) c.attackCooldown -= deltaTime;
            }

            _moveTickTimer -= deltaTime;
            bool canMoveThisFrame = _moveTickTimer <= 0f;
            if (canMoveThisFrame) _moveTickTimer = moveTickSeconds;

            // 공격으로 죽는 유닛이 생겨도 이번 틱 순회에는 영향이 없도록 스냅샷을 떠서 돈다.
            (_snapshotBuffer ??= new List<Combatant>()).Clear();
            _snapshotBuffer.AddRange(_combatants);

            foreach (var c in _snapshotBuffer)
            {
                if (!c.isAlive) continue;

                var target = FindNearestEnemy(c);
                if (target == null) continue;

                int range = Mathf.Max(1, Mathf.RoundToInt(c.stats.attackRange));
                int distance = HexCoord.Distance(c.coord, target.coord);

                if (distance <= range)
                {
                    if (c.attackCooldown <= 0f)
                    {
                        PerformAttack(c, target);
                        c.attackCooldown = c.stats.attackSpeed > 0.01f ? 1f / c.stats.attackSpeed : 1f;
                    }
                }
                else if (canMoveThisFrame)
                {
                    StepToward(c, target);
                }
            }

            CheckOutcome();
        }

        private Combatant FindNearestEnemy(Combatant c)
        {
            Combatant best = null;
            int bestDist = int.MaxValue;
            foreach (var other in _combatants)
            {
                if (!other.isAlive || other.isPlayerSide == c.isPlayerSide) continue;
                int d = HexCoord.Distance(c.coord, other.coord);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = other;
                }
            }
            return best;
        }

        /// <summary>
        /// 대상 사거리 안으로 들어가는 최단 경로를 BFS로 찾아 그 경로의 첫 칸으로 한 칸 옮긴다.
        /// 다른 살아있는 유닛(아군이든 적이든)이 서 있는 칸은 지나갈 수 없다고 보고 길을 찾으므로,
        /// 같은 편 유닛이 일렬로 앞을 막고 있으면 자동으로 옆 칸으로 돌아가는 경로를 고른다.
        /// (예: 아군 두 마리가 한 줄로 서서 같은 적을 노릴 때, 뒤에 있는 유닛이 가만히 있지 않고
        /// 알아서 옆으로 돌아 들어간다.) 경로를 아예 찾지 못하면(완전히 둘러싸인 경우 등)
        /// 예전 방식(직선 거리를 가장 많이 줄여주는 빈 이웃 칸)으로 한 번 더 시도한다.
        /// </summary>
        private void StepToward(Combatant c, Combatant target)
        {
            int range = Mathf.Max(1, Mathf.RoundToInt(c.stats.attackRange));

            HexCoord? next = FindStepTowardRange(c, target.coord, range);
            if (!next.HasValue) next = FindNearestOpenNeighbor(c, target.coord);

            if (!next.HasValue)
            {
                Debug.LogWarning($"[CombatManager] {(c.isPlayerSide ? "아군" : "적")} {c.coord}가 대상({target.coord})에게 다가갈 " +
                          "길을 찾지 못해 멈춰 있습니다 (다른 유닛들에 완전히 둘러싸였을 수 있음).");
                return;
            }

            MoveCombatant(c, next.Value);
        }

        /// <summary>
        /// c.coord에서 시작해서, target 좌표로부터 range 칸 이내(=공격 가능 범위)에 들어가는 가장 가까운
        /// "빈 칸"까지의 최단 경로를 BFS로 찾는다. 다른 살아있는 유닛이 서 있는 칸은 지나갈 수 없는
        /// 벽으로 취급하므로, 앞이 막혀 있으면 자연히 옆으로 돌아가는 경로가 나온다.
        /// 경로를 찾으면 그 경로의 "첫 칸"(이번 틱에 옮길 한 칸)을, 못 찾으면 null을 돌려준다.
        /// </summary>
        private HexCoord? FindStepTowardRange(Combatant c, HexCoord targetCoord, int range)
        {
            if (BoardManager.Instance == null) return null;

            var cameFrom = new Dictionary<HexCoord, HexCoord> { [c.coord] = c.coord };
            var frontier = new Queue<HexCoord>();
            frontier.Enqueue(c.coord);

            int safety = 0;
            while (frontier.Count > 0 && safety < 512)
            {
                safety++;
                var cur = frontier.Dequeue();

                if (HexCoord.Distance(cur, targetCoord) <= range)
                {
                    return ReconstructFirstStep(cameFrom, c.coord, cur);
                }

                for (int dir = 0; dir < 6; dir++)
                {
                    var next = cur.GetNeighbor(dir);
                    if (cameFrom.ContainsKey(next)) continue;
                    if (!BoardManager.Instance.TryGetTile(next, out _)) continue;
                    if (_byCoord.TryGetValue(next, out var occupant) && occupant.isAlive) continue;

                    cameFrom[next] = cur;
                    frontier.Enqueue(next);
                }
            }

            return null;
        }

        private static HexCoord ReconstructFirstStep(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord start, HexCoord goal)
        {
            HexCoord step = goal;
            while (!cameFrom[step].Equals(start))
            {
                step = cameFrom[step];
            }
            return step;
        }

        /// <summary>
        /// BFS로 경로를 못 찾았을 때(완전히 둘러싸인 경우 등)의 대체 수단: 대상과의 직선 거리를
        /// 가장 많이 줄여주는 빈 이웃 칸으로 옮긴다(원래 있던 방식). 그런 칸도 없으면 null.
        /// </summary>
        private HexCoord? FindNearestOpenNeighbor(Combatant c, HexCoord targetCoord)
        {
            HexCoord? best = null;
            int bestDist = HexCoord.Distance(c.coord, targetCoord);

            for (int dir = 0; dir < 6; dir++)
            {
                var next = c.coord.GetNeighbor(dir);
                if (_byCoord.TryGetValue(next, out var occupant) && occupant.isAlive) continue;
                if (BoardManager.Instance == null || !BoardManager.Instance.TryGetTile(next, out _)) continue;

                int d = HexCoord.Distance(next, targetCoord);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = next;
                }
            }

            return best;
        }

        private void MoveCombatant(Combatant c, HexCoord to)
        {
            var from = c.coord;
            _byCoord.Remove(from);
            c.coord = to;
            _byCoord[to] = c;

            if (c.isPlayerSide) PlayerRoster.Instance?.CombatMoveUnit(from, to);
            else EnemySpawner.Instance?.CombatMoveUnit(from, to);
        }

        private void PerformAttack(Combatant attacker, Combatant target)
        {
            float rawDamage = attacker.stats.attackPower - target.stats.defense;
            float damage = Mathf.Max(1f, rawDamage);
            target.currentHp -= damage;

            Debug.LogWarning($"[CombatManager] {(attacker.isPlayerSide ? "아군" : "적")} {attacker.coord} -> " +
                      $"{(target.isPlayerSide ? "아군" : "적")} {target.coord} 공격: {damage} 피해 (남은 HP {target.currentHp:0.#})");

            AttackEffects.PlayAttack(attacker.coord, target.coord, BuildStyleInfo(attacker));

            if (target.currentHp <= 0f)
            {
                target.currentHp = 0f;
                MarkDefeated(target);
            }
        }

        private AttackEffects.StyleInfo BuildStyleInfo(Combatant c)
        {
            float meleeThreshold = EnemySpawner.Instance != null ? EnemySpawner.Instance.meleeRangeThreshold : 1.5f;
            return new AttackEffects.StyleInfo
            {
                isPlayerSide = c.isPlayerSide,
                isMelee = c.stats.attackRange <= meleeThreshold,
                species = c.species,
                habitat = c.habitat,
                tier = c.tier,
                starLevel = Mathf.Clamp(c.starLevel, 1, 3),
            };
        }

        private void MarkDefeated(Combatant c)
        {
            if (!c.isAlive) return;
            c.isAlive = false;

            SetViewDefeated(c);
            _byCoord.Remove(c.coord);

            if (c.isPlayerSide)
            {
                if (c.playerUnit != null) c.playerUnit.isAlive = false;
            }
            else
            {
                if (c.enemyUnit != null) c.enemyUnit.isAlive = false;
            }
        }

        private void SetViewDefeated(Combatant c)
        {
            if (BoardManager.Instance == null) return;

            if (c.isPlayerSide)
            {
                var controller = BoardManager.Instance.GetComponent<BoardUnitVisualsController>();
                if (controller != null && controller.TryGetView(c.coord, out var view) && view != null)
                {
                    view.SetDefeated(true);
                }
            }
            else
            {
                var controller = BoardManager.Instance.GetComponent<EnemyUnitVisualsController>();
                if (controller != null && controller.TryGetView(c.coord, out var view) && view != null)
                {
                    view.SetDefeated(true);
                }
            }
        }

        private void CheckOutcome()
        {
            int playerAlive = 0, enemyAlive = 0;
            foreach (var c in _combatants)
            {
                if (!c.isAlive) continue;
                if (c.isPlayerSide) playerAlive++; else enemyAlive++;
            }
            AlivePlayerCount = playerAlive;
            AliveEnemyCount = enemyAlive;

            if (playerAlive > 0 && enemyAlive > 0) return;

            bool won = playerAlive > 0;

            Debug.LogWarning($"[CombatManager] 전투 종료: {(won ? "아군 승리" : "적 승리")} " +
                      $"(남은 아군 {playerAlive}마리, 남은 적 {enemyAlive}마리)");

            _battleActive = false;
            RoundManager.Instance?.EndRound(won: won);
        }
    }
}
