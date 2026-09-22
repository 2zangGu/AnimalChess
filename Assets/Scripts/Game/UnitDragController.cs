using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AnimalChess.Board;

namespace AnimalChess.Game
{
    /// <summary>
    /// 롤토체스처럼 벤치 &lt;-&gt; 보드 사이에서 유닛을 마우스 드래그로 자유롭게 옮기는 컨트롤러.
    ///
    /// - 벤치 칸의 유닛을 잡아서 보드의 내 존(player zone) 타일 위에 놓으면 그 칸에 배치된다.
    /// - 보드 위의 유닛을 잡아서 벤치 쪽에 놓으면 벤치의 빈 자리로 되돌아온다.
    /// - 보드 위의 유닛을 다른 보드 타일 위에 놓으면 그 자리로 옮겨진다.
    /// - 벤치 유닛을 다른 벤치 칸에 놓으면 두 칸이 서로 자리를 맞바꾼다.
    /// - 이미 다른 유닛이 있는 보드 칸에 놓으면 서로 자리를 맞바꾼다.
    /// - 적 존이나 화면의 빈 공간처럼 유효하지 않은 곳에 놓으면 원래 자리 그대로 취소된다.
    ///
    /// 실제 이동/배치 로직은 전부 PlayerRoster에 있고, 이 클래스는 입력(마우스 드래그) 처리와
    /// 드래그 중 보여주는 아이콘/타일 강조만 담당한다. BoardManager가 Awake 시점에 자동으로
    /// 붙여주므로 씬에 직접 추가할 필요는 없다.
    /// </summary>
    public class UnitDragController : MonoBehaviour
    {
        private enum DragSource { Bench, Board }

        [Tooltip("레이캐스트에 사용할 카메라. 비워두면 Camera.main을 사용한다.")]
        public Camera raycastCamera;
        [Tooltip("보드 레이캐스트 최대 거리")]
        public float maxDistance = 100f;

        private bool _isDragging;
        private DragSource _dragSource;
        private int _dragBenchIndex = -1;
        private HexCoord _dragFromCoord;

        private Canvas _canvas;
        private RectTransform _dragIconRect;
        private Image _dragIconImage;

        private HexTile _lastHoveredTile;
        private BenchSlotUI[] _benchSlotsCache;

        private void Update()
        {
            if (Mouse.current == null) return;

            if (!_isDragging)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryBeginDrag(Mouse.current.position.ReadValue());
                }
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            UpdateDragIconPosition(mousePos);
            UpdateHoverHighlight(mousePos);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                EndDrag(mousePos);
            }
        }

        private void TryBeginDrag(Vector2 mousePos)
        {
            if (TryGetBenchSlotAt(mousePos, out int benchIndex))
            {
                var unit = PlayerRoster.Instance != null ? PlayerRoster.Instance.Bench[benchIndex] : null;
                if (unit == null) return;
                BeginDrag(unit, DragSource.Bench, benchIndex, default);
                return;
            }

            if (TryRaycastUnit(mousePos, out HexCoord coord, out UnitInstance boardUnit))
            {
                BeginDrag(boardUnit, DragSource.Board, -1, coord);
            }
        }

        private void BeginDrag(UnitInstance unit, DragSource source, int benchIndex, HexCoord fromCoord)
        {
            _isDragging = true;
            _dragSource = source;
            _dragBenchIndex = benchIndex;
            _dragFromCoord = fromCoord;

            CreateDragIcon(unit);
            SetTileHoverEnabled(false);
        }

        private void EndDrag(Vector2 mousePos)
        {
            SetTileHoverEnabled(true);
            ClearHoveredTile();
            DestroyDragIcon();

            var roster = PlayerRoster.Instance;
            if (roster != null)
            {
                if (TryGetBenchSlotAt(mousePos, out int targetBenchIndex))
                {
                    if (_dragSource == DragSource.Board)
                    {
                        roster.TryReturnToBench(_dragFromCoord);
                    }
                    else if (_dragSource == DragSource.Bench && targetBenchIndex != _dragBenchIndex)
                    {
                        roster.TrySwapBenchSlots(_dragBenchIndex, targetBenchIndex);
                    }
                }
                else if (TryRaycastTile(mousePos, out HexTile tile) && tile.IsPlayerZone)
                {
                    if (_dragSource == DragSource.Bench)
                    {
                        roster.TryPlaceOnBoard(_dragBenchIndex, tile.Coord);
                    }
                    else
                    {
                        roster.TryMoveOnBoard(_dragFromCoord, tile.Coord);
                    }
                }
                // 그 외의 곳(적 존, 화면 빈 공간 등)에 놓으면 아무 것도 하지 않고 취소된다.
            }

            _isDragging = false;
            _dragBenchIndex = -1;
        }

        private void UpdateDragIconPosition(Vector2 mousePos)
        {
            if (_dragIconRect == null || _canvas == null) return;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)_canvas.transform, mousePos, _canvas.worldCamera, out Vector3 worldPoint);
            _dragIconRect.position = worldPoint;
        }

        private void UpdateHoverHighlight(Vector2 mousePos)
        {
            HexTile hovered = null;
            if (!TryGetBenchSlotAt(mousePos, out _))
            {
                TryRaycastTile(mousePos, out hovered);
            }

            HexTile validHovered = (hovered != null && hovered.IsPlayerZone) ? hovered : null;
            if (validHovered == _lastHoveredTile) return;

            if (_lastHoveredTile != null) _lastHoveredTile.SetHover(false);
            _lastHoveredTile = validHovered;
            if (_lastHoveredTile != null) _lastHoveredTile.SetHover(true);
        }

        private void ClearHoveredTile()
        {
            if (_lastHoveredTile != null) _lastHoveredTile.SetHover(false);
            _lastHoveredTile = null;
        }

        private void SetTileHoverEnabled(bool isEnabled)
        {
            var hover = BoardManager.Instance != null ? BoardManager.Instance.GetComponent<HexTileHoverController>() : null;
            if (hover == null) return;
            if (!isEnabled) hover.ClearHover();
            hover.enabled = isEnabled;
        }

        private bool TryRaycastTile(Vector2 screenPos, out HexTile tile)
        {
            tile = null;
            Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                tile = hit.collider.GetComponentInParent<HexTile>();
            }
            return tile != null;
        }

        private bool TryRaycastUnit(Vector2 screenPos, out HexCoord coord, out UnitInstance unit)
        {
            coord = default;
            unit = null;
            Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                var view = hit.collider.GetComponentInParent<BoardUnitView>();
                if (view != null && view.Unit != null)
                {
                    coord = view.Coord;
                    unit = view.Unit;
                    return true;
                }
            }
            return false;
        }

        private bool TryGetBenchSlotAt(Vector2 screenPos, out int slotIndex)
        {
            slotIndex = -1;
            EnsureBenchSlotsCache();
            if (_benchSlotsCache == null) return false;

            foreach (var slot in _benchSlotsCache)
            {
                if (slot == null) continue;
                var rect = slot.GetComponent<RectTransform>();
                if (rect == null) continue;

                var canvas = slot.GetComponentInParent<Canvas>();
                var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam))
                {
                    slotIndex = slot.slotIndex;
                    return true;
                }
            }
            return false;
        }

        private void EnsureBenchSlotsCache()
        {
            if (_benchSlotsCache != null && _benchSlotsCache.Length > 0) return;
            _benchSlotsCache = FindObjectsByType<BenchSlotUI>(FindObjectsSortMode.None);
        }

        private void CreateDragIcon(UnitInstance unit)
        {
            _canvas = FindFirstObjectByType<Canvas>();
            if (_canvas == null) return;

            var go = new GameObject("UnitDragIcon", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            go.transform.SetAsLastSibling();

            _dragIconRect = go.GetComponent<RectTransform>();
            _dragIconRect.sizeDelta = new Vector2(72f, 72f);

            _dragIconImage = go.AddComponent<Image>();
            _dragIconImage.raycastTarget = false;
            _dragIconImage.preserveAspect = true;
            _dragIconImage.color = new Color(1f, 1f, 1f, 0.85f);

            var icon = unit?.currentData != null ? unit.currentData.icon : null;
            if (icon != null) _dragIconImage.sprite = icon;
        }

        private void DestroyDragIcon()
        {
            if (_dragIconRect != null) Destroy(_dragIconRect.gameObject);
            _dragIconRect = null;
            _dragIconImage = null;
        }
    }
}
