using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.Environment
{
    /// <summary>
    /// 서식지 시너지에 따라 하늘/안개/주변광을 부드럽게 바꾸는 배경 테마 매니저.
    ///
    /// 지금은 보드에 실제 유닛을 배치하는 시스템이 없어서 SetDominantHabitat()을
    /// 테스트용(우클릭 컨텍스트 메뉴)으로만 호출해볼 수 있다.
    /// 나중에 보드/유닛 배치 시스템이 만들어지면 "라운드 종료 시 보드 위 동물들의
    /// 서식지를 집계해서 가장 많은 서식지로 SetDominantHabitat() 호출"하는 코드를
    /// 어딘가(예: 라운드 매니저)에서 연결해주면 된다.
    /// </summary>
    public class BackgroundThemeManager : MonoBehaviour
    {
        public static BackgroundThemeManager Instance { get; private set; }

        [Header("테마 애셋 (Tools/AnimalChess/Create Default Habitat Themes 로 생성)")]
        public HabitatBackgroundTheme defaultTheme;
        public List<HabitatBackgroundTheme> habitatThemes = new List<HabitatBackgroundTheme>();

        [Header("전환 속도 (초)")]
        public float transitionDuration = 2f;

        [Header("조형물 (비워두면 같은 오브젝트에서 자동으로 찾음)")]
        public HabitatDecorationSpawner decorationSpawner;

        [Header("바닥 (비워두면 씬에서 'GroundBase' 이름으로 자동으로 찾음)")]
        [Tooltip("Tools > AnimalChess > 기본 배경(바닥) 적용 으로 만든 GroundBase 오브젝트.")]
        public Renderer groundRenderer;
        [Tooltip("바닥 텍스처를 바꿀 때 유지할 가로 폭(월드 유닛). 세로는 텍스처 비율에 맞춰 자동 계산한다. " +
                 "0이면 지금 GroundBase의 현재 가로 스케일을 그대로 기준으로 쓴다.")]
        public float groundWidth = 0f;

        [Header("하늘 배경 (선택)")]
        [Tooltip("체스판/바닥 바깥쪽에 보이는 하늘 배경 이미지(구름 낀 하늘 등). " +
                 "비워두면 예전처럼 단색 그라디언트(Skybox/Procedural)를 쓰고, " +
                 "넣으면 이 이미지를 하늘 전체(파노라마)에 씌운다.")]
        public Texture2D skyCloudsTexture;

        private Material _skyboxMaterial;
        private Material _groundMaterialInstance;
        private Texture _defaultGroundTexture;
        private Quaternion _defaultGroundRotation;
        private Vector3 _defaultGroundPosition;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private Coroutine _transitionRoutine;
        private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        private static readonly int GroundColorId = Shader.PropertyToID("_GroundColor");

        private void Awake()
        {
            Instance = this;

            if (decorationSpawner == null)
            {
                decorationSpawner = GetComponent<HabitatDecorationSpawner>();
            }

            if (groundRenderer == null)
            {
                var groundGO = GameObject.Find("GroundBase");
                if (groundGO != null) groundRenderer = groundGO.GetComponent<Renderer>();
            }

            if (groundRenderer != null)
            {
                // 원본 애셋을 건드리지 않도록 런타임 전용 머티리얼 인스턴스로 복제해서 쓴다.
                _groundMaterialInstance = groundRenderer.material;
                _defaultGroundTexture = _groundMaterialInstance.GetTexture(BaseMapId);
                if (groundWidth <= 0f) groundWidth = groundRenderer.transform.localScale.x;
                // 오버라이드가 없는 서식지로 돌아갈 때 되돌릴 "원래 각도/위치"로 기억해둔다.
                _defaultGroundRotation = groundRenderer.transform.localRotation;
                _defaultGroundPosition = groundRenderer.transform.localPosition;
            }

            if (skyCloudsTexture == null)
            {
                // Inspector에 직접 안 넣어놨으면 Resources 폴더에서 자동으로 찾아 쓴다.
                skyCloudsTexture = Resources.Load<Texture2D>("SkyClouds");
            }

            // 하늘/안개 색상용 절차적 스카이박스는 그대로 유지한다(안개/주변광 색과 어울리는 은은한 배경용).
            _skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
            RenderSettings.skybox = _skyboxMaterial;
            RenderSettings.fog = true;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            // 체스판/바닥 바깥쪽에 실제로 눈에 보이는 "구름 낀 하늘"은, 스카이박스 대신
            // 카메라가 항상 보는 먼 방향에 커다란 평면을 하나 세워서 그 위에 이미지를 씌운다.
            // (이 게임 카메라는 회전이 없고 줌만 가능해서, 이 방식이 훨씬 확실하게 보인다.)
            if (skyCloudsTexture != null)
            {
                CreateSkyBackdrop();
            }

            if (defaultTheme != null)
            {
                ApplyThemeInstant(defaultTheme);
                decorationSpawner?.SpawnForTheme(defaultTheme);
            }
        }

        private void ApplyGroundTexture(HabitatBackgroundTheme theme)
        {
            if (groundRenderer == null) return;

            // 기본(중립) 테마일 때만 기본 바닥 텍스처로 대체한다.
            // 그 외 서식지 테마는 자기 groundTexture가 없으면 바닥 자체를 숨긴다
            // (기본 배경이 대신 나오지 않게).
            bool isDefaultTheme = theme == defaultTheme;
            Texture tex = theme.groundTexture != null
                ? (Texture)theme.groundTexture
                : (isDefaultTheme ? _defaultGroundTexture : null);

            if (tex == null)
            {
                groundRenderer.enabled = false;
                return;
            }

            groundRenderer.enabled = true;
            if (_groundMaterialInstance == null) return;
            _groundMaterialInstance.SetTexture(BaseMapId, tex);

            // 이 서식지가 위치/크기/각도를 따로 지정했으면 그 값을, 아니면 기본 배경 값을 쓴다.
            // 이렇게 하면 "사막 바닥만" 위치/크기/각도를 바꿔도 기본 배경(다른 서식지)은 전혀 영향받지 않는다.
            bool useOverride = theme.overrideGroundTransform;
            float width = useOverride ? theme.groundWidthOverride : groundWidth;
            Quaternion rotation = useOverride
                ? Quaternion.Euler(theme.groundRotationOverride)
                : _defaultGroundRotation;
            Vector3 position = useOverride ? theme.groundPositionOverride : _defaultGroundPosition;

            // 텍스처마다 원본 비율이 다르므로, 가로 폭은 유지하고 세로는 비율로 자동 계산하되,
            // 오버라이드에서 깊이(세로)를 따로 지정했으면 그 값을 그대로 쓴다.
            float aspect = tex.height > 0 ? (float)tex.width / tex.height : 1f;
            float depth = (useOverride && theme.groundDepthOverride > 0f) ? theme.groundDepthOverride : width / aspect;

            groundRenderer.transform.localScale = new Vector3(width, depth, 1f);
            groundRenderer.transform.localRotation = rotation;
            groundRenderer.transform.localPosition = position;
        }

        /// <summary>
        /// 가장 많은 서식지 시너지(dominant habitat)에 맞는 배경으로 부드럽게 전환한다.
        /// habitat이 null이면 기본(중립) 테마로 돌아간다.
        /// </summary>
        public void SetDominantHabitat(Habitat? habitat)
        {
            HabitatBackgroundTheme theme = defaultTheme;
            if (habitat.HasValue)
            {
                theme = habitatThemes.Find(t => t != null && t.habitat == habitat.Value) ?? defaultTheme;
            }

            if (theme == null) return;

            decorationSpawner?.SpawnForTheme(theme);
            ApplyGroundTexture(theme);

            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(TransitionTo(theme, transitionDuration));
        }

        private void ApplyThemeInstant(HabitatBackgroundTheme theme)
        {
            _skyboxMaterial.SetColor(SkyTintId, theme.skyTint);
            _skyboxMaterial.SetColor(GroundColorId, theme.groundColor);
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.ambientLight = theme.ambientColor;
            ApplyGroundTexture(theme);
        }

        private IEnumerator TransitionTo(HabitatBackgroundTheme theme, float duration)
        {
            Color startSky = _skyboxMaterial.GetColor(SkyTintId);
            Color startGround = _skyboxMaterial.GetColor(GroundColorId);
            Color startFog = RenderSettings.fogColor;
            Color startAmbient = RenderSettings.ambientLight;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);

                _skyboxMaterial.SetColor(SkyTintId, Color.Lerp(startSky, theme.skyTint, k));
                _skyboxMaterial.SetColor(GroundColorId, Color.Lerp(startGround, theme.groundColor, k));
                RenderSettings.fogColor = Color.Lerp(startFog, theme.fogColor, k);
                RenderSettings.ambientLight = Color.Lerp(startAmbient, theme.ambientColor, k);

                yield return null;
            }
        }

        /// <summary>
        /// 체스판/바닥 바깥쪽(먼 배경)에 보이는 "구름 낀 하늘" 평면을 하나 만든다.
        /// 이 게임 카메라(FixedIsoCamera)는 항상 같은 각도만 보고 회전이 없어서,
        /// 스카이박스 대신 카메라가 바라보는 먼 방향에 커다란 사각형 하나만 세워두면
        /// 화면 어디서 봐도(줌 인/아웃해도) 자연스럽게 하늘처럼 보인다.
        /// </summary>
        private void CreateSkyBackdrop()
        {
            try
            {
                if (GameObject.Find("SkyBackdrop") != null) return; // 이미 있으면 다시 안 만든다.

                float pitch = 50f;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    var iso = cam.GetComponent<AnimalChess.CameraSystem.FixedIsoCamera>();
                    if (iso != null) pitch = iso.pitchAngle;
                }
                else
                {
                    Debug.LogWarning("[BackgroundThemeManager] Camera.main을 못 찾아서 하늘 배경판 각도를 기본값(50도)으로 만듭니다.");
                }

                Quaternion camRot = Quaternion.Euler(pitch, 0f, 0f);
                Vector3 camForward = camRot * Vector3.forward; // 카메라가 바라보는 방향(아래+앞쪽)

                // 카메라는 아래를 내려다보므로, 정면 방향을 그대로 멀리 뻗으면 땅속으로 들어가 버린다.
                // "하늘"은 카메라 시야의 위쪽 가장자리가 유한한 바닥(GroundBase)의 가장자리를
                // 넘어가서 아무것도 없는 먼 곳을 보는 부분이므로, 수평 방향으로 아주 멀리 세워둔
                // 커다란 "지평선 벽"으로 채워주는 방식이 맞다. 정확한 각도 계산이 조금 어긋나도
                // 문제없게 거리/크기를 넉넉하게 잡는다.
                Vector3 horizontalForward = new Vector3(camForward.x, 0f, camForward.z);
                if (horizontalForward.sqrMagnitude < 0.0001f) horizontalForward = Vector3.forward;
                horizontalForward.Normalize();

                Vector3 centerPos = groundRenderer != null ? groundRenderer.transform.position : transform.position;

                const float farDistance = 300f;
                const float width = 900f;
                const float height = 600f;

                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "SkyBackdrop";
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                go.transform.SetParent(transform, false);
                go.transform.position = centerPos + horizontalForward * farDistance;
                go.transform.rotation = Quaternion.LookRotation(-horizontalForward, Vector3.up);
                go.transform.localScale = new Vector3(width, height, 1f);

                var quadRenderer = go.GetComponent<MeshRenderer>();
                quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                quadRenderer.receiveShadows = false;

                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                                ?? Shader.Find("Unlit/Texture")
                                ?? Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    Debug.LogError("[BackgroundThemeManager] 하늘 배경판에 쓸 셰이더를 하나도 못 찾았습니다. " +
                                   "SkyBackdrop 오브젝트는 만들어졌지만 텍스처가 안 보일 수 있습니다.");
                    return;
                }

                var mat = new Material(shader);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", skyCloudsTexture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", skyCloudsTexture);
                quadRenderer.material = mat;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BackgroundThemeManager] 하늘 배경판(SkyBackdrop) 생성 중 오류: {e}");
            }
        }

#if UNITY_EDITOR
        private int _cycleIndex = -1;

        [ContextMenu("테스트: 기본 테마로")]
        private void TestDefault() => SetDominantHabitat(null);

        [ContextMenu("테스트: 다음 서식지로 순환")]
        private void TestCycleNext()
        {
            if (habitatThemes.Count == 0) return;
            _cycleIndex = (_cycleIndex + 1) % habitatThemes.Count;
            SetDominantHabitat(habitatThemes[_cycleIndex].habitat);
        }
#endif
    }
}
