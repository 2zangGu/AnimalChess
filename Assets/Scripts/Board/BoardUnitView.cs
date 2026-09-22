using UnityEngine;
using AnimalChess.Game;

namespace AnimalChess.Board
{
    /// <summary>
    /// 보드 위 한 칸에 배치된 유닛 하나를 나타내는 비주얼(실제 3D 모델이 준비되기 전까지의 placeholder).
    /// 그 유닛의 아이콘을 보여주는 빌보드 스프라이트로 구성된다(타일 위 어두운 받침/그림자 디스크는 없앴다).
    ///
    /// BoardUnitVisualsController가 PlayerRoster.BoardUnits와 매 프레임 비교하며 생성/삭제하므로
    /// 씬에 직접 추가할 필요는 없다. 항상 자신이 배치된 HexTile의 자식으로 만들어지기 때문에,
    /// 타일이 boardOffset 등으로 움직이면 같이 따라간다.
    ///
    /// 이 GameObject에 붙는 BoxCollider가 UnitDragController의 "이 유닛을 집는다" 판정에 쓰인다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BoardUnitView : MonoBehaviour
    {
        public HexCoord Coord { get; private set; }
        public UnitInstance Unit { get; private set; }

        private static Material _sharedSpriteMaterial;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private Transform _spriteQuad;
        private MeshRenderer _spriteRenderer;
        private MaterialPropertyBlock _mpb;
        private Sprite _lastIcon;

        public void Initialize(UnitInstance unit, HexCoord coord)
        {
            Unit = unit;
            Coord = coord;

            BuildSpriteQuad();
            RefreshVisual();

            float spriteSize = GetSpriteSize();
            var box = GetComponent<BoxCollider>();
            box.center = new Vector3(0f, spriteSize * 0.4f, 0f);
            box.size = new Vector3(spriteSize * 0.8f, spriteSize * 0.8f, spriteSize * 0.8f);
        }

        /// <summary>currentData가 강등/진화 등으로 바뀌었을 수 있으니, 아이콘이 실제로 바뀐 경우에만 갱신한다.</summary>
        public void RefreshVisual()
        {
            if (Unit?.currentData == null || _spriteRenderer == null) return;

            var icon = Unit.currentData.icon;
            if (icon == null || icon == _lastIcon) return;

            _lastIcon = icon;
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetTexture(BaseMapId, icon.texture);
            _spriteRenderer.SetPropertyBlock(_mpb);
        }

        private void LateUpdate()
        {
            // 카메라가 회전하지 않는 고정 각도 카메라(FixedIsoCamera)이긴 하지만, 혹시 모를 변경에도
            // 항상 카메라를 바라보게 빌보드 처리를 해준다. 보드 위 유닛 수가 적어서 비용은 무시할 만하다.
            var cam = Camera.main;
            if (cam == null || _spriteQuad == null) return;
            _spriteQuad.rotation = Quaternion.LookRotation(_spriteQuad.position - cam.transform.position, Vector3.up);
        }

        private void BuildSpriteQuad()
        {
            float spriteSize = GetSpriteSize();

            var quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGO.name = "Sprite";

            // 프리미티브가 자동으로 붙여주는 콜라이더는 쓰지 않는다(픽업 판정은 이 GameObject 자신의 BoxCollider가 담당).
            var quadCollider = quadGO.GetComponent<Collider>();
            if (quadCollider != null) Destroy(quadCollider);

            quadGO.transform.SetParent(transform, false);
            quadGO.transform.localPosition = new Vector3(0f, spriteSize * 0.5f + 0.15f, 0f);
            quadGO.transform.localScale = Vector3.one * spriteSize;
            _spriteQuad = quadGO.transform;

            _spriteRenderer = quadGO.GetComponent<MeshRenderer>();
            _spriteRenderer.sharedMaterial = GetOrCreateSpriteMaterial();
            _spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _spriteRenderer.receiveShadows = false;
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 유닛 스프라이트 한 변의 길이(월드 단위). 타일 한 칸(가로 폭, flat-to-flat)에
        /// BoardManager.unitIconScale 배율을 곱한 크기로 계산한다. 유닛이 너무 크거나 작으면
        /// 코드를 다시 손댈 필요 없이 BoardManager 인스펙터의 '유닛 아이콘 크기' 슬라이더만
        /// 조절하면 된다. BoardManager가 없으면 기본 타일 크기(hexSize 1.4, tileGap 0.05, 배율 1)로 대체한다.
        /// </summary>
        private static float GetSpriteSize()
        {
            float radius = BoardManager.Instance != null
                ? BoardManager.Instance.hexSize * (1f - BoardManager.Instance.tileGap)
                : 1.33f;
            float scale = BoardManager.Instance != null ? BoardManager.Instance.unitIconScale : 1f;
            return radius * Mathf.Sqrt(3f) * scale;
        }

        /// <summary>
        /// 아이콘 스프라이트를 보여주는 반투명 Unlit 머티리얼. 색/텍스처는 MaterialPropertyBlock으로
        /// 렌더러마다 다르게 지정하므로(HexTile의 호버 머티리얼과 같은 방식), 머티리얼 자체는 공유한다.
        /// </summary>
        private static Material GetOrCreateSpriteMaterial()
        {
            if (_sharedSpriteMaterial != null) return _sharedSpriteMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "UnitIconMaterial" };
            mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);   // 0 = Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", Color.white);

            _sharedSpriteMaterial = mat;
            return _sharedSpriteMaterial;
        }
    }
}
