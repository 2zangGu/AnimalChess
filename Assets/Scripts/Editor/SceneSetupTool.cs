#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AnimalChess.Board;
using AnimalChess.CameraSystem;
using AnimalChess.Environment;
using AnimalChess.Data;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 현재 씬에 보드(BoardManager), 고정 카메라(FixedIsoCamera),
    /// 배경/장식 매니저(BackgroundThemeManager + HabitatDecorationSpawner)를 다시 준비해주는 도구.
    /// 오브젝트를 지웠거나 새 씬에서 다시 세팅해야 할 때, 메뉴 한 번으로 복구한다.
    /// 이미 있으면 그대로 두고(중복 생성 안 함), 없는 것만 새로 만든다.
    /// 메뉴: Tools > AnimalChess > 보드 + 카메라 + 배경 다시 만들기
    /// </summary>
    public static class SceneSetupTool
    {
        private const string ThemeFolder = "Assets/Data/HabitatThemes";

        [MenuItem("Tools/AnimalChess/보드 + 카메라 + 배경 다시 만들기")]
        public static void SetupBoardAndCamera()
        {
            bool boardCreated = EnsureBoardManager(out GameObject boardGO);
            bool cameraCreated = EnsureFixedCamera(out GameObject camGO);
            bool backgroundCreated = EnsureBackgroundManager(out GameObject bgGO, out bool themesAssigned);

            string msg = "씬에 보드/카메라/배경 오브젝트를 준비했습니다.\n\n";
            msg += boardCreated
                ? "- 'BoardManager' 오브젝트를 새로 만들었습니다.\n"
                : "- 기존 'BoardManager' 오브젝트를 그대로 사용합니다.\n";
            msg += cameraCreated
                ? "- 카메라에 FixedIsoCamera를 새로 붙였습니다.\n"
                : "- 카메라에 이미 FixedIsoCamera가 붙어 있습니다.\n";
            msg += backgroundCreated
                ? "- 'BackgroundThemeManager' 오브젝트를 새로 만들었습니다.\n"
                : "- 기존 'BackgroundThemeManager' 오브젝트를 그대로 사용합니다.\n";
            if (themesAssigned)
            {
                msg += "  (Theme_Default / 서식지별 테마 애셋을 자동으로 연결했습니다)\n";
            }
            msg += "\n보드는 Awake 시점(=Play 버튼을 눌렀을 때)에 생성되므로, " +
                   "지금 Edit 모드에서는 타일이 안 보이는 게 정상입니다. " +
                   "상단 Play 버튼을 눌러서 Game 탭으로 확인해주세요.";

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");

            Selection.activeGameObject = boardGO;
        }

        private static bool EnsureBoardManager(out GameObject boardGO)
        {
            var existing = Object.FindFirstObjectByType<BoardManager>();
            if (existing != null)
            {
                boardGO = existing.gameObject;
                return false;
            }

            boardGO = new GameObject("BoardManager");
            Undo.RegisterCreatedObjectUndo(boardGO, "Create BoardManager");
            Undo.AddComponent<BoardManager>(boardGO);
            MarkDirty(boardGO);
            return true;
        }

        private static bool EnsureFixedCamera(out GameObject camGO)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }

            if (cam == null)
            {
                camGO = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");
                camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
                camGO.tag = "MainCamera";
            }
            else
            {
                camGO = cam.gameObject;
            }

            if (camGO.GetComponent<FixedIsoCamera>() != null)
            {
                MarkDirty(camGO);
                return false;
            }

            Undo.AddComponent<FixedIsoCamera>(camGO);
            MarkDirty(camGO);
            return true;
        }

        private static bool EnsureBackgroundManager(out GameObject bgGO, out bool themesAssigned)
        {
            themesAssigned = false;

            var existing = Object.FindFirstObjectByType<BackgroundThemeManager>();
            bool created = existing == null;

            if (created)
            {
                bgGO = new GameObject("BackgroundThemeManager");
                Undo.RegisterCreatedObjectUndo(bgGO, "Create BackgroundThemeManager");
                existing = Undo.AddComponent<BackgroundThemeManager>(bgGO);
            }
            else
            {
                bgGO = existing.gameObject;
            }

            if (bgGO.GetComponent<HabitatDecorationSpawner>() == null)
            {
                Undo.AddComponent<HabitatDecorationSpawner>(bgGO);
            }

            if (existing.defaultTheme == null)
            {
                existing.defaultTheme = AssetDatabase.LoadAssetAtPath<HabitatBackgroundTheme>($"{ThemeFolder}/Theme_Default.asset");
                themesAssigned = true;
            }

            if (existing.habitatThemes == null || existing.habitatThemes.Count == 0)
            {
                existing.habitatThemes = new System.Collections.Generic.List<HabitatBackgroundTheme>();
                foreach (var name in new[] { "Theme_Forest", "Theme_Sea", "Theme_Swamp", "Theme_Desert", "Theme_Grassland", "Theme_Tundra" })
                {
                    var theme = AssetDatabase.LoadAssetAtPath<HabitatBackgroundTheme>($"{ThemeFolder}/{name}.asset");
                    if (theme != null) existing.habitatThemes.Add(theme);
                }
                themesAssigned = true;
            }

            MarkDirty(bgGO);
            return created;
        }

        private static void MarkDirty(GameObject go)
        {
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
#endif
