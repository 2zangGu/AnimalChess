using UnityEngine;
using AnimalChess.Game;

namespace AnimalChess.Board
{
    /// <summary>
    /// 보드 위에 자동 배치된 적 유닛 하나를 나타내는 비주얼(아이콘을 보여주는 빌보드 스프라이트).
    ///
    /// EnemyUnitVisualsController가 EnemySpawner.BoardUnits와 매 프레임 비교하며 생성/삭제하므로
    /// 씬에 직접 추가할 필요는 없다. 항상 자신이 배치된 HexTile의 자식으로 만들어진다.
    ///
    /// 플레이어 유닛(BoardUnitView)과 달리 드래그로 옮기지 않으므로 콜라이더나 받침 디스크는
    /// 두지 않는다.
    /// </summary>
    public class EnemyUnitView : MonoBehaviour
    {
        public HexCoord Coord { get; private set; }
        public EnemyUnitInstance Unit { get; private set; }

        private static Material _sharedSpriteMaterial;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private Transform _spriteQuad;
        private MeshRenderer _spriteRenderer;
        private MaterialPropertyBlock _mpb;
        private Sprite _lastIcon;

        public void Initialize(EnemyUnitInstance unit, HexCoord coord)
        {
            Unit = unit;
            Coord = coord;

            BuildSpriteQuad();
            RefreshVisual();
        }

        /// <summary>아이콘이 실제로 바뀐 경우에만 갱신한다.</summary>
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
            var cam = Camera.main;
            if (cam == null || _spriteQuad == null) return;
            _spriteQuad.rotation = Quaternion.LookRotation(_spriteQuad.position - cam.transform.position, Vector3.up);
        }

        private void BuildSpriteQuad()
        {
            float spriteSize = GetSpriteSize();

            var quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGO.name = "Sprite";

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
        /// 유닛 스프라이트 한 변의 길이(월드 단위). BoardManager의 enemyIconScale 배율을 따른다.
        /// 동물 유닛(BoardUnitView, unitIconScale)과는 별도의 값이라, 인스펙터에서 적 유닛만
        /// 따로 크게/작게 조절할 수 있다.
        /// </summary>
        private static float GetSpriteSize()
        {
            float radius = BoardManager.Instance != null
                ? BoardManager.Instance.hexSize * (1f - BoardManager.Instance.tileGap)
                : 1.33f;
            float scale = BoardManager.Instance != null ? BoardManager.Instance.enemyIconScale : 1f;
            return radius * Mathf.Sqrt(3f) * scale;
        }

        /// <summary>
        /// 아이콘 스프라이트를 보여주는 반투명 Unlit 머티리얼. 색/텍스처는 MaterialPropertyBlock으로
        /// 렌더러마다 다르게 지정하므로, 머티리얼 자체는 공유한다.
        /// </summary>
        private static Material GetOrCreateSpriteMaterial()
        {
            if (_sharedSpriteMaterial != null) return _sharedSpriteMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "EnemyIconMaterial" };
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
