using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Game;

namespace AnimalChess.Board
{
    /// <summary>
    /// EnemySpawner.BoardUnits(자동 배치된 적 유닛들)를 매 프레임 확인해서, 새로 배치된 적은
    /// EnemyUnitView를 만들어 붙이고, 라운드가 바뀌어 치워진 적은 비주얼을 없애준다. 이미 있는
    /// 적이 다른 칸으로만 옮겨진 경우(전투 중 자동 이동)에는 뷰를 파괴/재생성하지 않고 그 자리에서
    /// 통통 튀며 이동하는 애니메이션을 재생한다(EnemyUnitView.MoveTo 참고).
    ///
    /// BoardManager가 Awake 시점에 자동으로 붙여주므로 씬에 직접 추가할 필요는 없다.
    /// </summary>
    public class EnemyUnitVisualsController : MonoBehaviour
    {
        // 유닛(참조) 기준으로 뷰를 추적한다. 같은 유닛이 좌표만 바뀌었을 때(이동)와
        // 유닛 자체가 사라졌을 때(죽음/웨이브 교체)를 구분하기 위함이다.
        private readonly Dictionary<EnemyUnitInstance, EnemyUnitView> _views = new Dictionary<EnemyUnitInstance, EnemyUnitView>();
        private List<EnemyUnitInstance> _staleBuffer;

        private void Update()
        {
            var spawner = EnemySpawner.Instance;
            var board = BoardManager.Instance;
            if (spawner == null || board == null) return;

            RemoveStaleViews(spawner);
            CreateOrMoveViews(spawner, board);
            RefreshExistingViews();
        }

        private void RemoveStaleViews(EnemySpawner spawner)
        {
            _staleBuffer?.Clear();
            foreach (var kvp in _views)
            {
                var unit = kvp.Key;
                bool stillValid = unit != null && kvp.Value != null && unit.boardCoord.HasValue &&
                                   spawner.BoardUnits.TryGetValue(unit.boardCoord.Value, out var atCoord) && atCoord == unit;
                if (!stillValid)
                {
                    (_staleBuffer ??= new List<EnemyUnitInstance>()).Add(unit);
                }
            }

            if (_staleBuffer == null) return;
            foreach (var unit in _staleBuffer)
            {
                if (_views.TryGetValue(unit, out var view) && view != null)
                {
                    Destroy(view.gameObject);
                }
                _views.Remove(unit);
            }
        }

        private void CreateOrMoveViews(EnemySpawner spawner, BoardManager board)
        {
            foreach (var kvp in spawner.BoardUnits)
            {
                HexCoord coord = kvp.Key;
                EnemyUnitInstance unit = kvp.Value;
                if (unit == null) continue;

                if (_views.TryGetValue(unit, out var existingView) && existingView != null)
                {
                    if (!existingView.Coord.Equals(coord) && board.TryGetTile(coord, out HexTile destTile))
                    {
                        existingView.MoveTo(destTile, coord);
                    }
                    continue;
                }

                if (!board.TryGetTile(coord, out HexTile tile)) continue;

                var viewGO = new GameObject($"Enemy_{coord.q}_{coord.r}");
                viewGO.transform.SetParent(tile.transform, false);

                var view = viewGO.AddComponent<EnemyUnitView>();
                view.Initialize(unit, coord);
                _views[unit] = view;
            }
        }

        private void RefreshExistingViews()
        {
            foreach (var kvp in _views)
            {
                var view = kvp.Value;
                if (view == null) continue;
                view.RefreshVisual();

                // BoardUnitVisualsController와 같은 이유로, isAlive 값을 매 프레임 그대로 반영해서
                // 회색 반투명 표시가 실제 생사 상태와 어긋난 채로 남지 않게 한다.
                var unit = kvp.Key;
                if (unit != null) view.SetDefeated(!unit.isAlive);
            }
        }

        /// <summary>
        /// 지금 그 좌표에 그려져 있는 EnemyUnitView를 찾는다. CombatManager가 공격 이펙트를
        /// 스폰할 위치를 잡거나, 죽은 유닛의 비주얼을 회색으로 바꿀 때 쓴다.
        /// 유닛이 이동 애니메이션 도중이라도 Coord는 목적지 좌표로 즉시 갱신되므로 항상 최신 값을 찾는다.
        /// </summary>
        public bool TryGetView(HexCoord coord, out EnemyUnitView view)
        {
            foreach (var v in _views.Values)
            {
                if (v != null && v.Coord.Equals(coord))
                {
                    view = v;
                    return true;
                }
            }
            view = null;
            return false;
        }
    }
}
