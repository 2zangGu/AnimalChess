#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 새 아트 리소스 없이, 지금 씬의 라이팅/포스트 프로세싱만 다듬어서
    /// 도형 기반 비주얼을 최대한 자연스럽게 보이게 만드는 도구.
    /// 메뉴: Tools > AnimalChess > Apply Graphics Polish
    /// </summary>
    public static class GraphicsPolishSetup
    {
        [MenuItem("Tools/AnimalChess/Apply Graphics Polish")]
        public static void Apply()
        {
            bool volumeOk = ApplyVolumeProfile();
            bool lightOk = ApplyMainLight();
            ApplyCameraPostProcessing();

            string msg = "라이팅/포스트 프로세싱을 적용했습니다.\n\n" +
                         "추가로 원하시면 Project 창에서 Settings 폴더의 렌더러 애셋(예: PC_Renderer)을 선택 → " +
                         "Add Renderer Feature → Screen Space Ambient Occlusion 을 눌러서 그림자 디테일을 더 살릴 수 있어요.";
            if (!volumeOk) msg += "\n\n(주의: Volume Profile을 찾지 못해 포스트 프로세싱은 건너뛰었습니다.)";
            if (!lightOk) msg += "\n\n(주의: 씬에서 Directional Light를 찾지 못했습니다.)";

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");
        }

        private static bool ApplyVolumeProfile()
        {
            string[] candidatePaths =
            {
                "Assets/Settings/SampleSceneProfile.asset",
                "Assets/Settings/DefaultVolumeProfile.asset"
            };

            VolumeProfile profile = null;
            foreach (var path in candidatePaths)
            {
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (profile != null) break;
            }

            if (profile == null)
            {
                Debug.LogWarning("[AnimalChess] Volume Profile을 찾지 못했습니다. 씬에 Global Volume이 있는지 확인해주세요.");
                return false;
            }

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.9f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.25f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.6f;

            if (!profile.TryGet(out ColorAdjustments colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.overrideState = true; colorAdjustments.postExposure.value = 0.1f;
            colorAdjustments.contrast.overrideState = true; colorAdjustments.contrast.value = 8f;
            colorAdjustments.saturation.overrideState = true; colorAdjustments.saturation.value = 6f;

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true; vignette.intensity.value = 0.22f;
            vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.4f;

            if (!profile.TryGet(out Tonemapping tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.overrideState = true; tonemapping.mode.value = TonemappingMode.ACES;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool ApplyMainLight()
        {
            Light sun = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) { sun = light; break; }
            }

            if (sun == null)
            {
                Debug.LogWarning("[AnimalChess] 씬에서 Directional Light를 찾지 못했습니다.");
                return false;
            }

            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.color = new Color(1f, 0.96f, 0.9f); // 살짝 따뜻한 태양광 톤

            EditorUtility.SetDirty(sun);
            EditorUtility.SetDirty(sun.gameObject);
            EditorSceneManager.MarkSceneDirty(sun.gameObject.scene);
            return true;
        }

        private static void ApplyCameraPostProcessing()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    cam = c;
                    break;
                }
            }
            if (cam == null) return;

            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                EditorUtility.SetDirty(camData);
            }
            EditorUtility.SetDirty(cam);
            EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        }
    }
}
#endif
