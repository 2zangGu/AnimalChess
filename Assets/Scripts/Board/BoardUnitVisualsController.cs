using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Game;

namespace AnimalChess.Board
{
    /// <summary>
    /// PlayerRoster.BoardUnits(보드 위에 배치된 유닛들)를 매 프레임 확인해서, 새로 배치된 유닛은
    /// BoardUnitView를 만들어 붙이고, 치워지거나(벤치로 복귀/죽음/진화로 교체됨) 사라진 유닛은
    /// 비주얼을 없애준다. 이미 있는 유닛이 다른 칸으로만 옮겨진 경우(전투 중 자동 이동, 준비 화면
    /// 드래그 이동 등)에는 뷰를 파괴/재생성하지 않고 그 자리에서 통통 튀며 이동하는 애니메이션을
    /// 재생한다(BoardUnitView.MoveTo 참고).
    ///
    /// BoardManager가 Awake 시점에 자동으로 붙여주므로 씬에 직접 추가할 필요는 없다.
    /// </summary>
    public class BoardUnitVisualsController : MonoBehaviour
    {
        // 유닛(참조) 기준으로 뷰를 추적한다. 같은 유닛이 좌표만 바뀌었을 때(이동)와
        // 유닛 자체가 사라졌을 때(죽음/벤치 복귀/진화로 교체됨)를 구분하기 위함이다.
        private readonly Dictionary<UnitInstance, BoardUnitView> _views = new Dictionary<UnitInstance, BoardUnitView>();
        private List<UnitInstance> _staleBuffer;

        private void Update()
        {
            var roster = PlayerRoster.Instance;
            var board = BoardManager.Instance;
            if (roster == null || board == null) return;

            RemoveStaleViews(roster);
            CreateOrMoveViews(roster, board);
            RefreshExistingViews();
        }

        private void RemoveStaleViews(PlayerRoster roster)
        {
            _staleBuffer?.Clear();
            foreach (var kvp in _views)
            {
                var unit = kvp.Key;
                bool stillValid = unit != null && kvp.Value != null && unit.boardCoord.HasValue &&
                                   roster.BoardUnits.TryGetValue(unit.boardCoord.Value, out var atCoord) && atCoord == unit;
                if (!stillValid)
                {
                    (_staleBuffer ??= new List<UnitInstance>()).Add(unit);
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

        private void CreateOrMoveViews(PlayerRoster roster, BoardManager board)
        {
            foreach (var kvp in roster.BoardUnits)
            {
                HexCoord coord = kvp.Key;
                UnitInstance unit = kvp.Value;
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

                var viewGO = new GameObject($"Unit_{coord.q}_{coord.r}");
                viewGO.transform.SetParent(tile.transform, false);

                var view = viewGO.AddComponent<BoardUnitView>();
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

                // 죽었다가(SetDefeated(true)) 라운드가 끝나 PlayerRoster.ProcessDeaths가 강등시켜
                // isAlive를 다시 true로 되돌린 유닛은, 같은 UnitInstance가 같은 칸에 그대로 남기
                // 때문에(RemoveStaleViews 기준으로는 "그대로 있는 유닛") 뷰가 파괴/재생성되지 않는다.
                // 그래서 회색 반투명 표시가 다음 라운드 준비 단계까지 그대로 남아있던 버그가 있었다.
                // 매 프레임 isAlive 값을 그대로 반영해주면, ProcessDeaths가 isAlive=true로 되돌리는
                // 순간(다음 프레임) 바로 원래 텍스처로 복귀한다(SetDefeated는 상태가 실제로 바뀔
                // 때만 색을 다시 칠하므로 매 프레임 불러도 비용 문제는 없다).
                var unit = kvp.Key;
                if (unit != null) view.SetDefeated(!unit.isAlive);
            }
        }

        /// <summary>
        /// 지금 그 좌표에 그려져 있는 BoardUnitView를 찾는다. CombatManager가 공격 이펙트를
        /// 스폰할 위치를 잡거나, 죽은 유닛의 비주얼을 회색으로 바꿀 때 쓴다.
        /// 유닛이 이동 애니메이션 도중이라도 Coord는 목적지 좌표로 즉시 갱신되므로 항상 최신 값을 찾는다.
        /// </summary>
        public bool TryGetView(HexCoord coord, out BoardUnitView view)
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
