#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AnimalChess.Data;
using AnimalChess.Environment;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 6개 서식지 + 기본(중립) 배경 테마 애셋을 한 번에 만들어주는 에디터 도구.
    /// 메뉴: Tools > AnimalChess > Create Default Habitat Themes
    /// 이미 애셋이 있으면 값만 최신 상태로 덮어쓰므로, 스크립트에 필드를 추가한 뒤
    /// 다시 실행해도 안전하다(애셋을 지우고 다시 만들 필요 없음).
    /// 색상/조형물 값은 대략적인 시작점이니 마음에 안 들면 애셋을 선택해서 인스펙터에서 바로 조정하면 된다.
    /// </summary>
    public static class HabitatThemeGenerator
    {
        private const string OutputFolder = "Assets/Data/HabitatThemes";

        [MenuItem("Tools/AnimalChess/Create Default Habitat Themes")]
        public static void CreateDefaultThemes()
        {
            EnsureFolder(OutputFolder);

            CreateTheme("Theme_Default", null,
                sky: new Color(0.55f, 0.6f, 0.65f), ground: new Color(0.4f, 0.4f, 0.4f),
                fog: new Color(0.6f, 0.62f, 0.65f), ambient: new Color(0.45f, 0.45f, 0.45f),
                variants: new[]
                {
                    V(PropShape.Rock, new Color(0.5f,0.5f,0.5f), new Color(0.35f,0.35f,0.35f), 5, 0.6f, 1.1f),
                    V(PropShape.Boulder, new Color(0.45f,0.45f,0.45f), new Color(0.3f,0.3f,0.3f), 2, 0.8f, 1.1f),
                });

            CreateTheme("Theme_Forest", Habitat.Forest,
                sky: new Color(0.45f, 0.7f, 0.55f), ground: new Color(0.25f, 0.35f, 0.2f),
                fog: new Color(0.5f, 0.65f, 0.5f), ambient: new Color(0.35f, 0.45f, 0.3f),
                variants: new[]
                {
                    V(PropShape.Tree, new Color(0.35f,0.22f,0.12f), new Color(0.22f,0.45f,0.22f), 10, 0.9f, 1.7f),
                    V(PropShape.Bush, new Color(0.28f,0.5f,0.25f), new Color(0.22f,0.4f,0.2f), 6, 0.6f, 1.0f),
                    V(PropShape.Log, new Color(0.32f,0.22f,0.14f), new Color(0.25f,0.4f,0.2f), 3, 0.8f, 1.2f),
                    V(PropShape.Rock, new Color(0.4f,0.42f,0.35f), new Color(0.3f,0.32f,0.26f), 3, 0.5f, 0.9f),
                });

            CreateTheme("Theme_Sea", Habitat.Sea,
                sky: new Color(0.35f, 0.55f, 0.8f), ground: new Color(0.15f, 0.3f, 0.5f),
                fog: new Color(0.5f, 0.65f, 0.8f), ambient: new Color(0.3f, 0.4f, 0.5f),
                variants: new[]
                {
                    V(PropShape.Rock, new Color(0.3f,0.4f,0.5f), new Color(0.18f,0.28f,0.4f), 6, 0.6f, 1.2f),
                    V(PropShape.Boulder, new Color(0.28f,0.36f,0.45f), new Color(0.2f,0.28f,0.38f), 3, 0.9f, 1.3f),
                    V(PropShape.Coral, new Color(0.85f,0.45f,0.5f), new Color(0.35f,0.6f,0.65f), 5, 0.5f, 1.0f),
                });

            CreateTheme("Theme_Swamp", Habitat.Swamp,
                sky: new Color(0.5f, 0.55f, 0.35f), ground: new Color(0.3f, 0.28f, 0.15f),
                fog: new Color(0.55f, 0.58f, 0.4f), ambient: new Color(0.35f, 0.38f, 0.25f),
                variants: new[]
                {
                    V(PropShape.DeadTree, new Color(0.3f,0.27f,0.2f), new Color(0.32f,0.36f,0.2f), 6, 0.8f, 1.5f),
                    V(PropShape.Mushroom, new Color(0.65f,0.3f,0.28f), new Color(0.85f,0.82f,0.7f), 8, 0.4f, 0.8f),
                    V(PropShape.Reed, new Color(0.4f,0.32f,0.18f), new Color(0.4f,0.5f,0.25f), 6, 0.6f, 1.1f),
                    V(PropShape.Rock, new Color(0.35f,0.36f,0.25f), new Color(0.28f,0.3f,0.2f), 3, 0.5f, 0.9f),
                });

            CreateTheme("Theme_Desert", Habitat.Desert,
                sky: new Color(0.85f, 0.65f, 0.4f), ground: new Color(0.6f, 0.45f, 0.25f),
                fog: new Color(0.8f, 0.65f, 0.45f), ambient: new Color(0.55f, 0.45f, 0.3f),
                variants: new[]
                {
                    V(PropShape.Cactus, new Color(0.3f,0.5f,0.25f), new Color(0.25f,0.42f,0.2f), 6, 0.8f, 1.4f),
                    V(PropShape.Boulder, new Color(0.6f,0.48f,0.3f), new Color(0.5f,0.4f,0.25f), 4, 0.8f, 1.3f),
                    V(PropShape.SandDune, new Color(0.72f,0.58f,0.38f), new Color(0.65f,0.52f,0.32f), 5, 0.8f, 1.6f),
                    V(PropShape.Rock, new Color(0.55f,0.45f,0.3f), new Color(0.45f,0.36f,0.24f), 3, 0.5f, 0.9f),
                });

            CreateTheme("Theme_Grassland", Habitat.Grassland,
                sky: new Color(0.75f, 0.8f, 0.45f), ground: new Color(0.55f, 0.55f, 0.2f),
                fog: new Color(0.8f, 0.8f, 0.55f), ambient: new Color(0.5f, 0.5f, 0.3f),
                variants: new[]
                {
                    V(PropShape.GrassTuft, new Color(0.55f,0.7f,0.25f), new Color(0.65f,0.75f,0.35f), 18, 0.5f, 1.0f),
                    V(PropShape.Bush, new Color(0.5f,0.6f,0.25f), new Color(0.45f,0.55f,0.22f), 5, 0.6f, 1.0f),
                    V(PropShape.Rock, new Color(0.55f,0.5f,0.35f), new Color(0.45f,0.42f,0.3f), 2, 0.5f, 0.8f),
                });

            CreateTheme("Theme_Tundra", Habitat.Tundra,
                sky: new Color(0.8f, 0.88f, 0.95f), ground: new Color(0.75f, 0.8f, 0.85f),
                fog: new Color(0.85f, 0.9f, 0.95f), ambient: new Color(0.65f, 0.7f, 0.75f),
                variants: new[]
                {
                    V(PropShape.Crystal, new Color(0.75f,0.85f,0.95f), new Color(0.9f,0.95f,1f), 5, 0.8f, 1.6f),
                    V(PropShape.SnowMound, new Color(0.92f,0.95f,0.98f), new Color(0.85f,0.9f,0.95f), 6, 0.6f, 1.1f),
                    V(PropShape.Boulder, new Color(0.65f,0.7f,0.75f), new Color(0.55f,0.62f,0.68f), 3, 0.8f, 1.3f),
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AnimalChess] 서식지 배경 테마 7개를 생성/갱신했습니다: {OutputFolder}");
        }

        private static PropVariant V(PropShape shape, Color primary, Color secondary, int count, float minScale, float maxScale)
        {
            return new PropVariant
            {
                shape = shape,
                primaryColor = primary,
                secondaryColor = secondary,
                count = count,
                minScale = minScale,
                maxScale = maxScale
            };
        }

        private static void CreateTheme(
            string assetName, Habitat? habitat,
            Color sky, Color ground, Color fog, Color ambient,
            PropVariant[] variants)
        {
            string path = $"{OutputFolder}/{assetName}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<HabitatBackgroundTheme>(path);
            bool isNew = theme == null;
            if (isNew)
            {
                theme = ScriptableObject.CreateInstance<HabitatBackgroundTheme>();
            }

            if (habitat.HasValue) theme.habitat = habitat.Value;
            theme.skyTint = sky;
            theme.groundColor = ground;
            theme.fogColor = fog;
            theme.ambientColor = ambient;
            theme.propVariants = new List<PropVariant>(variants);

            if (isNew)
            {
                AssetDatabase.CreateAsset(theme, path);
            }
            else
            {
                EditorUtility.SetDirty(theme);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
