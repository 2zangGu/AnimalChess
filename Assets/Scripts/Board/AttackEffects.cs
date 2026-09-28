using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.Board
{
    /// <summary>
    /// 유닛 176종(적 26 + 동물 150) 전체에 대해 개별 애셋을 하나도 만들지 않고,
    /// 기존 데이터(Species/Habitat/EnemyTier/사거리로 판별한 근접·원거리/StarLevel)만으로
    /// "그 동물/유닛과 어울리는" 공격 이펙트를 절차적으로 골라 재생하는 정적 헬퍼.
    ///
    /// 규칙:
    /// - 근접: 공격자가 대상 쪽으로 살짝 돌진(Lunge)했다 돌아오고, 타격 지점에서 히트 플래시가 터진다.
    /// - 원거리: 공격자에서 대상까지 작은 발사체(총알/화염 등)가 날아가고, 착탄 지점에서 히트 플래시가 터진다.
    /// - 색상은 적 유닛이면 티어(원시무기=갈색, 총기=빨강, 기계화=시안), 아군 원거리는 서식지,
    ///   아군 근접은 종족 기준으로 정해진다 - 유닛마다 손으로 지정할 필요가 전혀 없다.
    /// - 별(★) 단계가 오를수록(1★→2★→3★) 크기/파티클 수가 커지고, 2★부터 빛나는 링이,
    ///   3★부터는 이중 펄스 링(추가 장식)까지 더해진다(사용자가 선택한 '단계적 진화 이펙트').
    ///
    /// 새 텍스처/모델은 전혀 쓰지 않는다 - HexTile의 호버 테두리, BoardUnitView/EnemyUnitView의
    /// 아이콘 스프라이트 쿼드와 똑같은 방식(Quad + LineRenderer + URP Unlit 반투명 머티리얼 +
    /// MaterialPropertyBlock)으로만 그린다.
    /// </summary>
    public static class AttackEffects
    {
        /// <summary>공격 하나를 재생하는 데 필요한 스타일 정보. CombatManager가 매 공격마다 채워서 넘긴다.</summary>
        public struct StyleInfo
        {
            public bool isPlayerSide;
            public bool isMelee;
            public Species? species;
            public Habitat? habitat;
            public EnemyTier? tier;
            /// <summary>1~3. 적 유닛은 성장 개념이 없으므로 항상 1.</summary>
            public int starLevel;
        }

        private struct VisualStyle
        {
            public Color color;
            public int particleCount;
            public float sizeScale;
            public bool glowRing;
            public bool extraFlourish;
        }

        private static Material _sharedMaterial;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>다른 이펙트 조각(AttackEffectMotions)에서도 같은 공유 머티리얼을 쓰기 위한 공개 접근자.</summary>
        internal static Material SharedMaterial => GetOrCreateMaterial();

        /// <summary>
        /// attackerCoord에서 targetCoord를 공격하는 이펙트를 재생한다. 두 좌표 모두 실제 타일이
        /// 존재해야 하며(없으면 조용히 무시), 타일의 월드 위치 + 유닛 스프라이트 높이만큼 띄운
        /// 지점 사이에서 이펙트가 벌어진다.
        /// </summary>
        public static void PlayAttack(HexCoord attackerCoord, HexCoord targetCoord, StyleInfo style)
        {
            if (BoardManager.Instance == null) return;
            if (!BoardManager.Instance.TryGetTile(attackerCoord, out HexTile fromTile)) return;
            if (!BoardManager.Instance.TryGetTile(targetCoord, out HexTile toTile)) return;

            Vector3 from = fromTile.transform.position + Vector3.up * UnitHeight();
            Vector3 to = toTile.transform.position + Vector3.up * UnitHeight();

            var visual = ResolveVisualStyle(style);

            if (style.isMelee) PlayMelee(from, to, visual);
            else PlayRanged(from, to, visual);
        }

        private static float UnitHeight()
        {
            float radius = BoardManager.Instance.hexSize * (1f - BoardManager.Instance.tileGap);
            return radius * Mathf.Sqrt(3f) * 0.5f + 0.15f;
        }

        /// <summary>
        /// Species/Habitat/EnemyTier + StarLevel만 보고 색/크기/파티클 수/링 유무/추가 장식을 정한다.
        /// 이 규칙 하나가 176개 유닛 전체를 커버하므로, 새 유닛이 추가돼도 손댈 필요가 없다.
        /// </summary>
        private static VisualStyle ResolveVisualStyle(StyleInfo style)
        {
            Color baseColor;

            if (!style.isPlayerSide && style.tier.HasValue)
            {
                baseColor = style.tier.Value switch
                {
                    EnemyTier.Hunter => new Color(0.75f, 0.55f, 0.25f),     // 원시 무기 - 갈색/황토색
                    EnemyTier.Soldier => new Color(0.85f, 0.2f, 0.15f),      // 총기 - 빨강
                    EnemyTier.Mechanized => new Color(0.25f, 0.85f, 0.95f),  // 메크/전투기 - 시안
                    _ => Color.white,
                };
            }
            else if (style.isPlayerSide && !style.isMelee && style.habitat.HasValue)
            {
                // 아군 원거리 유닛(총/화염방사기 등 대신 서식지에 어울리는 원소로 표현) 색은 서식지 기준.
                baseColor = style.habitat.Value switch
                {
                    Habitat.Forest => new Color(0.35f, 0.75f, 0.3f),
                    Habitat.Sea => new Color(0.25f, 0.55f, 0.95f),
                    Habitat.Swamp => new Color(0.55f, 0.45f, 0.2f),
                    Habitat.Desert => new Color(0.9f, 0.75f, 0.35f),
                    Habitat.Grassland => new Color(0.65f, 0.85f, 0.35f),
                    Habitat.Tundra => new Color(0.7f, 0.9f, 0.95f),
                    _ => Color.white,
                };
            }
            else if (style.isPlayerSide && style.species.HasValue)
            {
                // 아군 근접 유닛 색은 종족 기준.
                baseColor = style.species.Value switch
                {
                    Species.Mammal => new Color(0.85f, 0.6f, 0.35f),
                    Species.Fish => new Color(0.3f, 0.65f, 0.9f),
                    Species.Reptile => new Color(0.35f, 0.75f, 0.45f),
                    Species.Bird => new Color(0.95f, 0.8f, 0.3f),
                    Species.Amphibian => new Color(0.45f, 0.85f, 0.65f),
                    Species.Insect => new Color(0.75f, 0.35f, 0.85f),
                    _ => Color.white,
                };
            }
            else
            {
                baseColor = Color.white;
            }

            int star = Mathf.Clamp(style.starLevel, 1, 3);
            return new VisualStyle
            {
                color = baseColor,
                particleCount = star == 1 ? 4 : star == 2 ? 7 : 10,
                sizeScale = star == 1 ? 1f : star == 2 ? 1.3f : 1.6f,
                glowRing = star >= 2,
                extraFlourish = star >= 3,
            };
        }

        private static void PlayMelee(Vector3 from, Vector3 to, VisualStyle visual)
        {
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();
            Vector3 lungeTarget = from + dir * 0.6f;

            var lungeGO = SpawnQuad("MeleeLunge", from, 0.35f * visual.sizeScale, visual.color);
            lungeGO.AddComponent<AttackLungeMotion>().Setup(from, lungeTarget, 0.18f);

            SpawnHitFlash(to, visual);
        }

        private static void PlayRanged(Vector3 from, Vector3 to, VisualStyle visual)
        {
            var projGO = SpawnQuad("Projectile", from, 0.22f * visual.sizeScale, visual.color);
            float travelTime = Mathf.Clamp(Vector3.Distance(from, to) * 0.05f, 0.08f, 0.4f);
            projGO.AddComponent<AttackProjectileMotion>().Setup(from, to, travelTime, () => SpawnHitFlash(to, visual));
        }

        private static void SpawnHitFlash(Vector3 pos, VisualStyle visual)
        {
            var flashGO = SpawnQuad("HitFlash", pos, 0.5f * visual.sizeScale, visual.color);
            flashGO.AddComponent<AttackFadeScaleMotion>().Setup(0.25f, 1.6f);

            if (visual.particleCount > 0)
            {
                var burstGO = new GameObject("Burst") { transform = { position = pos } };
                burstGO.AddComponent<AttackBurstParticleMotion>().Setup(visual.particleCount, visual.sizeScale, visual.color);
            }

            if (visual.glowRing)
            {
                var ringGO = new GameObject("GlowRing") { transform = { position = pos } };
                ringGO.AddComponent<AttackRingExpandMotion>().Setup(visual.sizeScale, visual.color);
            }

            if (visual.extraFlourish)
            {
                // 3성: 살짝 지연된 두 번째 링으로 이중 펄스 효과를 낸다.
                var delayedGO = new GameObject("FlourishRingDelay") { transform = { position = pos } };
                delayedGO.AddComponent<AttackDelayedRingMotion>().Setup(pos, visual.sizeScale, visual.color, 0.12f);
            }
        }

        private static GameObject SpawnQuad(string name, Vector3 pos, float size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;

            var cam = Camera.main;
            if (cam != null)
            {
                go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.transform.position, Vector3.up);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetOrCreateMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(mpb);

            return go;
        }

        private static Material GetOrCreateMaterial()
        {
            if (_sharedMaterial != null) return _sharedMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "AttackEffectMaterial" };
            mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);   // 0 = Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor(BaseColorId, Color.white);

            _sharedMaterial = mat;
            return _sharedMaterial;
        }
    }
}
