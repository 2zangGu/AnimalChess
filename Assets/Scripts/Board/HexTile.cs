using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// 보드를 구성하는 육각 타일 하나. BoardManager가 절차적으로 생성해서 붙인다.
    /// 현재는 시각적 플레이스홀더(색상 구분) + 상태값만 있고,
    /// 유닛 배치/클릭 상호작용은 다음 단계에서 이 컴포넌트를 통해 붙일 예정.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class HexTile : MonoBehaviour
    {
        public HexCoord Coord { get; private set; }
        public bool IsPlayerZone { get; private set; }
        public bool IsOccupied { get; set; }

        [Header("타일 색상")]
        public Color playerZoneColor = new Color(0.55f, 0.75f, 1f);
        public Color enemyZoneColor = new Color(1f, 0.6f, 0.55f);
        public Color hoverColor = Color.yellow;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public void Initialize(HexCoord coord, bool isPlayerZone)
        {
            Coord = coord;
            IsPlayerZone = isPlayerZone;
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
            SetColor(isPlayerZone ? playerZoneColor : enemyZoneColor);
        }

        public void SetHover(bool hovering)
        {
            SetColor(hovering ? hoverColor : (IsPlayerZone ? playerZoneColor : enemyZoneColor));
        }

        private void SetColor(Color color)
        {
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
