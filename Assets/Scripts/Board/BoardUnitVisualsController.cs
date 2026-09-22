using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Game;

namespace AnimalChess.Board
{
    /// <summary>
    /// PlayerRoster.BoardUnits(보드 위에 배치된 유닛들)를 매 프레임 확인해서, 새로 배치된 유닛은
    /// BoardUnitView를 만들어 붙이고, 치워지거나(벤치로 복귀/다른 칸으로 이동) 죽어서 사라진 유닛은
    /// 비주얼을 없애준다. BoardManager가 Awake 시점에 자동으로 붙여주므로 씬에 직접 추가할 필요는 없다.
    /// </summary>
    public class BoardUnitVisualsController : MonoBehaviour
    {
        private readonly Dictionary<HexCoord, BoardUnitView> _views = new Dictionary<HexCoord, BoardUnitView>();
        private List<HexCoord> _staleBuffer;

        private void Update()
        {
            var roster = PlayerRoster.Instance;
            var board = BoardManager.Instance;
            if (roster == null || board == null) return;

            RemoveStaleViews(roster);
            CreateMissingViews(roster, board);
            RefreshExistingViews();
        }

        private void RemoveStaleViews(PlayerRoster roster)
        {
            _staleBuffer?.Clear();
            foreach (var kvp in _views)
            {
                bool stillValid = roster.BoardUnits.TryGetValue(kvp.Key, out var unit) &&
                                   kvp.Value != null && unit == kvp.Value.Unit;
                if (!stillValid)
                {
                    (_staleBuffer ??= new List<HexCoord>()).Add(kvp.Key);
                }
            }

            if (_staleBuffer == null) return;
            foreach (var coord in _staleBuffer)
            {
                if (_views.TryGetValue(coord, out var view) && view != null)
                {
                    Destroy(view.gameObject);
                }
                _views.Remove(coord);
            }
        }

        private void CreateMissingViews(PlayerRoster roster, BoardManager board)
        {
            foreach (var kvp in roster.BoardUnits)
            {
                if (_views.ContainsKey(kvp.Key)) continue;
                if (!board.TryGetTile(kvp.Key, out HexTile tile)) continue;

                var viewGO = new GameObject($"Unit_{kvp.Key.q}_{kvp.Key.r}");
                viewGO.transform.SetParent(tile.transform, false);

                var view = viewGO.AddComponent<BoardUnitView>();
                view.Initialize(kvp.Value, kvp.Key);
                _views[kvp.Key] = view;
            }
        }

        private void RefreshExistingViews()
        {
            foreach (var view in _views.Values)
            {
                if (view != null) view.RefreshVisual();
            }
        }
    }
}
