using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimalChess.Board
{
    /// <summary>
    /// 매 프레임 마우스 아래에 있는 HexTile을 찾아 SetHover를 호출해주는 컨트롤러.
    /// 롤토체스처럼 타일 위에 마우스를 올리면 그 칸만 살짝 투명 + 테두리로 강조된다.
    ///
    /// 나중에 "유닛을 들고 배치할 때 놓을 수 있는 칸 강조" 기능을 만들 때도,
    /// HexTile.SetHover(true/false)를 그대로 재사용하면 된다
    /// (예: 유닛 드래그 스크립트에서 배치 가능한 칸들에 SetHover(true)를 걸어주는 식).
    ///
    /// BoardManager가 Awake 시점에 자동으로 이 컴포넌트를 붙여주므로 따로 씬에 추가할 필요는 없다.
    /// </summary>
    public class HexTileHoverController : MonoBehaviour
    {
        [Tooltip("레이캐스트에 사용할 카메라. 비워두면 Camera.main을 사용한다.")]
        public Camera raycastCamera;
        [Tooltip("레이캐스트 최대 거리")]
        public float maxDistance = 100f;

        private HexTile _currentHover;

        private void Update()
        {
            Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null || Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePos);

            HexTile hitTile = null;
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                hitTile = hit.collider.GetComponentInParent<HexTile>();
            }

            if (hitTile != _currentHover)
            {
                if (_currentHover != null) _currentHover.SetHover(false);
                _currentHover = hitTile;
                if (_currentHover != null) _currentHover.SetHover(true);
            }
        }
    }
}
