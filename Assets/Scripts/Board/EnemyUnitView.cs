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
    /// 플레이어 유닛(BoardUnitView)과 달리 드래그로 옮기지 않으므로 받침 디스크는 두지 않지만,
    /// 마우스 오른쪽 버튼으로 정보창을 띄우는 레이캐스트 판정을 위해 BoxCollider는 붙여둔다
    /// (UnitStatPanelController가 이 콜라이더를 통해 유닛을 찾는다).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class EnemyUnitView : MonoBehaviour
    {
        public HexCoord Coord { get; private set; }
        public EnemyUnitInstance Unit { get; private set; }

        private static Material _sharedSpriteMaterial;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Transform _spriteQuad;
        private MeshRenderer _spriteRenderer;
        private MaterialPropertyBlock _mpb;
        private Sprite _lastIcon;
        private bool _isDefeated;

        // 이동 시 통통 튀는 모션 + 착지 스쿼시(UnitMoveMotion 참고). 실제 3D 모델/애니메이션이
        // 없는 상태에서 유닛이 덜 뻣뻣해 보이게 하려는 절차적(procedural) 연출이다.
        private bool _isHopping;
        private float _moveElapsed;
        private float _moveDuration;
        private Vector3 _moveFromWorld;
        private Vector3 _moveToWorld;
        private Transform _pendingParent;
        private HexCoord _pendingCoord;
        private bool _isLandingSquash;
        private float _landElapsed;

        /// <summary>이 유닛의 스프라이트를 보여주는 빌보드 Transform. CombatManager/AttackEffects가
        /// 공격 모션(돌진/발사체 등)이 시작될 위치를 잡을 때 참고한다.</summary>
        public Transform SpriteTransform => _spriteQuad;

        public void Initialize(EnemyUnitInstance unit, HexCoord coord)
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

        private void Update()
        {
            TickMoveMotion();
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || _spriteQuad == null) return;
            _spriteQuad.rotation = Quaternion.LookRotation(_spriteQuad.position - cam.transform.position, Vector3.up);
        }

        /// <summary>
        /// 이 유닛이 다른 칸으로 옮겨졌을 때(EnemyUnitVisualsController가 좌표 변화를 감지하면) 호출된다.
        /// 순간이동 대신, 현재 위치에서 목적지 타일까지 통통 튀며 부드럽게 이동한 뒤 착지 스쿼시를 재생한다.
        /// </summary>
        public void MoveTo(HexTile destinationTile, HexCoord newCoord)
        {
            if (destinationTile == null) return;

            float duration = CombatManager.Instance != null
                ? Mathf.Max(0.05f, CombatManager.Instance.moveTickSeconds)
                : 0.35f;

            _moveFromWorld = transform.position;
            _moveToWorld = destinationTile.transform.position;
            _pendingParent = destinationTile.transform;
            _pendingCoord = newCoord;
            _moveElapsed = 0f;
            _moveDuration = duration;
            _isHopping = true;
            _isLandingSquash = false;

            Coord = newCoord;

            // 목적지 타일이 이동 도중 자기도 움직일 수 있으니(보드 스크롤 등), 잠시 부모에서 떼어내
            // 월드 좌표 기준으로 보간한다. worldPositionStays=true라 시각적으로 순간 튀지 않는다.
            transform.SetParent(null, true);
        }

        private void TickMoveMotion()
        {
            if (_isHopping)
            {
                _moveElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_moveElapsed / _moveDuration);

                Vector3 pos = Vector3.Lerp(_moveFromWorld, _moveToWorld, t);
                pos.y += UnitMoveMotion.EvaluateHopHeight(t);
                transform.position = pos;

                ApplySpriteScale(UnitMoveMotion.EvaluateHopScale(t));

                if (t >= 1f)
                {
                    _isHopping = false;
                    transform.SetParent(_pendingParent, true);
                    _isLandingSquash = true;
                    _landElapsed = 0f;
                }
                return;
            }

            if (_isLandingSquash)
            {
                _landElapsed += Time.deltaTime;
                float u = Mathf.Clamp01(_landElapsed / UnitMoveMotion.LandingSquashDuration);
                ApplySpriteScale(UnitMoveMotion.EvaluateLandingScale(u));

                if (u >= 1f)
                {
                    _isLandingSquash = false;
                    ApplySpriteScale(Vector3.one);
                }
            }
        }

        private void ApplySpriteScale(Vector3 multiplier)
        {
            if (_spriteQuad == null) return;
            float baseSize = GetSpriteSize();
            _spriteQuad.localScale = new Vector3(baseSize * multiplier.x, baseSize * multiplier.y, baseSize * multiplier.z);
        }

        /// <summary>
        /// 전투 중 이 유닛이 죽은 것으로 처리되면(CombatManager) 스프라이트를 회색으로 물들인다.
        /// 실제 제거는 다음 웨이브가 스폰될 때(EnemySpawner.ClearBoard) 처리되므로, 여기서는
        /// 순수하게 눈에 보이는 표시만 바꾼다.
        /// </summary>
        public void SetDefeated(bool defeated)
        {
            if (_isDefeated == defeated || _spriteRenderer == null) return;
            _isDefeated = defeated;

            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, defeated ? new Color(0.35f, 0.35f, 0.35f, 0.85f) : Color.white);
            _spriteRenderer.SetPropertyBlock(_mpb);
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
