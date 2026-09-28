using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// 보드를 구성하는 육각 타일 하나. BoardManager가 절차적으로 생성해서 붙인다.
    ///
    /// 평소에는 플레이어/적 존 색으로 불투명하게 채워져 있다가,
    /// 롤토체스처럼 마우스를 올리거나(또는 나중에 유닛을 들고 있을 때) SetHover(true)가
    /// 호출되면 기본 채우기를 숨기고, 살짝 투명한 오버레이 + 테두리만 보이게 바뀐다.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class HexTile : MonoBehaviour
    {
        public HexCoord Coord { get; private set; }
        public bool IsPlayerZone { get; private set; }
        public bool IsOccupied { get; set; }

        [Header("타일 색상 (평소 상태)")]
        [Tooltip("이 타일 기본 재질(URP/Lit, 불투명)은 알파를 무시하므로, 옅게 보이려면 RGB 자체를 " +
                 "흰색에 가깝게 밝고 채도 낮은 색으로 잡아야 한다. 거의 안 보일 만큼 옅은 파스텔톤.")]
        public Color playerZoneColor = new Color(0.94f, 0.965f, 1f);
        public Color enemyZoneColor = new Color(1f, 0.955f, 0.945f);

        [Header("호버 강조 (마우스 오버 / 유닛 선택 중)")]
        [Tooltip("호버 시 채워지는 반투명 색 (알파를 낮게 잡아서 살짝 비치게)")]
        public Color hoverFillColor = new Color(1f, 0.92f, 0.45f, 0.16f);
        [Tooltip("호버 시 테두리 색")]
        public Color hoverBorderColor = new Color(1f, 0.85f, 0.25f, 0.95f);
        [Tooltip("테두리 두께")]
        public float borderWidth = 0.05f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material _hoverSharedMaterial;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;
        private MeshRenderer _hoverFillRenderer;
        private LineRenderer _border;

        public void Initialize(HexCoord coord, bool isPlayerZone, float radius)
        {
            Coord = coord;
            IsPlayerZone = isPlayerZone;
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
            SetBaseColor(isPlayerZone ? playerZoneColor : enemyZoneColor);

            BuildHoverFill(radius);
            BuildBorder(radius);
            SetHover(false);
        }

        /// <summary>
        /// hovering=true: 평소 채우기를 숨기고, 살짝 투명한 오버레이 + 테두리만 보이게 한다.
        /// hovering=false: 원래 존 색으로 복원한다.
        /// 마우스 호버 외에, 나중에 "유닛을 들고 있을 때 배치 가능한 칸 강조"에도 그대로 재사용하면 된다.
        /// </summary>
        public void SetHover(bool hovering)
        {
            if (_renderer != null) _renderer.enabled = !hovering;
            if (_hoverFillRenderer != null) _hoverFillRenderer.enabled = hovering;
            if (_border != null) _border.enabled = hovering;
        }

        private void SetBaseColor(Color color)
        {
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_mpb);
        }

        private void BuildHoverFill(float radius)
        {
            var overlayGO = new GameObject("HoverFill");
            overlayGO.transform.SetParent(transform, false);
            overlayGO.transform.localPosition = new Vector3(0f, 0.01f, 0f);

            var mesh = HexMeshUtility.CreatePointyTopHex(radius);
            overlayGO.AddComponent<MeshFilter>().sharedMesh = mesh;
            _hoverFillRenderer = overlayGO.AddComponent<MeshRenderer>();
            _hoverFillRenderer.sharedMaterial = GetSharedHoverMaterial();
            _hoverFillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _hoverFillRenderer.receiveShadows = false;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, hoverFillColor);
            _hoverFillRenderer.SetPropertyBlock(mpb);
        }

        private void BuildBorder(float radius)
        {
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(transform, false);
            borderGO.transform.localPosition = new Vector3(0f, 0.015f, 0f);

            _border = borderGO.AddComponent<LineRenderer>();
            _border.useWorldSpace = false;
            _border.loop = true;
            _border.positionCount = 6;
            _border.SetPositions(HexMeshUtility.GetCorners(radius));
            _border.widthMultiplier = borderWidth;
            _border.numCapVertices = 2;
            _border.numCornerVertices = 2;
            _border.sharedMaterial = GetSharedHoverMaterial();
            _border.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _border.receiveShadows = false;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, hoverBorderColor);
            _border.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// 호버 오버레이/테두리가 공용으로 쓰는 반투명 Unlit 머티리얼.
        /// 색은 각 렌더러의 MaterialPropertyBlock으로 개별 지정하므로, 머티리얼 자체는 하나만 만들어 공유한다.
        /// </summary>
        private static Material GetSharedHoverMaterial()
        {
            if (_hoverSharedMaterial != null) return _hoverSharedMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            mat.SetColor(BaseColorId, Color.white);

            // URP Unlit을 반투명(Alpha Blend) 서페이스로 전환하는 데 필요한 설정 일체.
            mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);   // 0 = Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            _hoverSharedMaterial = mat;
            return _hoverSharedMaterial;
        }
    }
}
