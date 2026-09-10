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

        private Material _skyboxMaterial;
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

            _skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
            RenderSettings.skybox = _skyboxMaterial;
            RenderSettings.fog = true;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            if (defaultTheme != null)
            {
                ApplyThemeInstant(defaultTheme);
                decorationSpawner?.SpawnForTheme(defaultTheme);
            }
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

            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(TransitionTo(theme, transitionDuration));
        }

        private void ApplyThemeInstant(HabitatBackgroundTheme theme)
        {
            _skyboxMaterial.SetColor(SkyTintId, theme.skyTint);
            _skyboxMaterial.SetColor(GroundColorId, theme.groundColor);
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.ambientLight = theme.ambientColor;
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
