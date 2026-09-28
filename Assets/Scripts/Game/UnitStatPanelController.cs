using UnityEngine;
using UnityEngine.InputSystem;
using AnimalChess.Board;

namespace AnimalChess.Game
{
    /// <summary>
    /// 마우스 오른쪽 버튼으로 보드 위(내 유닛/적 유닛) 또는 벤치의 유닛을 클릭하면
    /// UnitStatPanelUI에 그 유닛의 아이콘과 스탯을 띄워준다.
    /// 유닛이 없는 곳을 오른쪽 클릭하면 정보창을 닫는다.
    /// BoardManager가 Awake 시점에 자동으로 붙여주므로 씬에 직접 추가할 필요는 없다.
    /// </summary>
    public class UnitStatPanelController : MonoBehaviour
    {
        [Tooltip("레이캐스트에 사용할 카메라. 비워두면 Camera.main을 사용한다.")]
        public Camera raycastCamera;
        [Tooltip("보드 레이캐스트 최대 거리")]
        public float maxDistance = 100f;

        private BenchSlotUI[] _benchSlotsCache;

        private void Update()
        {
            if (Mouse.current == null) return;
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (TryGetBenchUnitAt(mousePos, out UnitInstance benchUnit))
            {
                UnitStatPanelUI.Instance?.ShowAnimal(benchUnit);
                return;
            }

            if (TryRaycastUnit(mousePos))
            {
                return;
            }

            // 유닛이 없는 곳을 오른쪽 클릭하면 정보창을 닫는다.
            UnitStatPanelUI.Instance?.Hide();
        }

        private bool TryRaycastUnit(Vector2 screenPos)
        {
            Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance)) return false;

            var boardView = hit.collider.GetComponentInParent<BoardUnitView>();
            if (boardView != null && boardView.Unit != null)
            {
                UnitStatPanelUI.Instance?.ShowAnimal(boardView.Unit);
                return true;
            }

            var enemyView = hit.collider.GetComponentInParent<EnemyUnitView>();
            if (enemyView != null && enemyView.Unit != null)
            {
                UnitStatPanelUI.Instance?.ShowEnemy(enemyView.Unit);
                return true;
            }

            return false;
        }

        private bool TryGetBenchUnitAt(Vector2 screenPos, out UnitInstance unit)
        {
            unit = null;
            EnsureBenchSlotsCache();
            if (_benchSlotsCache == null) return false;

            var roster = PlayerRoster.Instance;
            if (roster == null) return false;

            foreach (var slot in _benchSlotsCache)
            {
                if (slot == null) continue;
                var rect = slot.GetComponent<RectTransform>();
                if (rect == null) continue;

                var canvas = slot.GetComponentInParent<Canvas>();
                var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                if (!RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam)) continue;

                if (slot.slotIndex < 0 || slot.slotIndex >= roster.Bench.Length) return false;
                unit = roster.Bench[slot.slotIndex];
                return unit != null;
            }
            return false;
        }

        private void EnsureBenchSlotsCache()
        {
            if (_benchSlotsCache != null && _benchSlotsCache.Length > 0) return;
            _benchSlotsCache = FindObjectsByType<BenchSlotUI>(FindObjectsSortMode.None);
        }
    }
}
