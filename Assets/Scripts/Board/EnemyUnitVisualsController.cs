using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Game;

namespace AnimalChess.Board
{
    /// <summary>
    /// EnemySpawner.BoardUnits(자동 배치된 적 유닛들)를 매 프레임 확인해서, 새로 배치된 적은
    /// EnemyUnitView를 만들어 붙이고, 라운드가 바뀌어 치워진 적은 비주얼을 없애준다.
    /// BoardManager가 Awake 시점에 자동으로 붙여주므로 씬에 직접 추가할 필요는 없다.
    /// </summary>
    public class EnemyUnitVisualsController : MonoBehaviour
    {
        private readonly Dictionary<HexCoord, EnemyUnitView> _views = new Dictionary<HexCoord, EnemyUnitView>();
        private List<HexCoord> _staleBuffer;

        private void Update()
        {
            var spawner = EnemySpawner.Instance;
            var board = BoardManager.Instance;
            if (spawner == null || board == null) return;

            RemoveStaleViews(spawner);
            CreateMissingViews(spawner, board);
            RefreshExistingViews();
        }

        private void RemoveStaleViews(EnemySpawner spawner)
        {
            _staleBuffer?.Clear();
            foreach (var kvp in _views)
            {
                bool stillValid = spawner.BoardUnits.TryGetValue(kvp.Key, out var unit) &&
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

        private void CreateMissingViews(EnemySpawner spawner, BoardManager board)
        {
            foreach (var kvp in spawner.BoardUnits)
            {
                if (_views.ContainsKey(kvp.Key)) continue;
                if (!board.TryGetTile(kvp.Key, out HexTile tile)) continue;

                var viewGO = new GameObject($"Enemy_{kvp.Key.q}_{kvp.Key.r}");
                viewGO.transform.SetParent(tile.transform, false);

                var view = viewGO.AddComponent<EnemyUnitView>();
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
