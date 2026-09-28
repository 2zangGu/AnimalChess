#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AnimalChess.Game;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 화면 상단에 "Round N / 30" 진행 상태를 보여주는 UI(RoundManager + RoundHUD)를
    /// 씬에 준비해주는 도구. 시간 제한 없음, 마지막 라운드는 30으로 고정.
    /// 이미 있으면 그대로 두고(중복 생성 안 함), 없는 것만 새로 만든다.
    /// 이미 만들어진 HUD에 진행도 바가 남아 있으면(예전 버전) 제거해서 텍스트만 남긴다.
    /// 메뉴: Tools > AnimalChess > 라운드 HUD 만들기
    /// </summary>
    public static class RoundHUDSetupTool
    {
        private const string PanelTexturePath = "Assets/Textures/UI/RoundHUDPanel.png";
        private const int MaxRound = 30;
        private const float PanelHeight = 60f;
        private const float DefaultPrepTimeLimit = 30f;
        private const float StartButtonWidth = 110f;
        private const float StartButtonGap = 12f;

        [MenuItem("Tools/AnimalChess/라운드 HUD 만들기")]
        public static void SetupRoundHUD()
        {
            Canvas canvas = EnsureCanvas(out bool canvasCreated);
            bool eventSystemCreated = EnsureEventSystem();
            bool managerCreated = EnsureRoundManager(out GameObject managerGO);
            bool hudCreated = EnsureRoundHUDPanel(canvas, out GameObject hudGO, out bool barRemoved);
            var hud = hudGO.GetComponent<RoundHUD>();
            bool startButtonCreated = EnsureStartButton(hudGO, hud, out GameObject startButtonGO);

            string msg = "라운드 HUD를 씬에 준비했습니다.\n\n";
            msg += canvasCreated
                ? "- UI Canvas를 새로 만들었습니다.\n"
                : "- 기존 Canvas를 그대로 사용합니다.\n";
            msg += eventSystemCreated
                ? "- EventSystem(새 Input System용)을 새로 만들었습니다.\n"
                : "- 기존 EventSystem을 그대로 사용합니다.\n";
            msg += managerCreated
                ? $"- 'RoundManager' 오브젝트를 새로 만들었습니다 (마지막 라운드 {MaxRound}, 준비 시간 {DefaultPrepTimeLimit}초).\n"
                : "- 기존 'RoundManager'를 그대로 사용합니다.\n";
            msg += hudCreated
                ? "- 화면 상단에 라운드 표시 UI를 새로 만들었습니다 (라운드 번호 + 준비 시간 카운트다운).\n"
                : "- 기존 라운드 HUD를 그대로 사용합니다.\n";
            if (barRemoved)
            {
                msg += "- 예전에 만들어졌던 진행도 바를 제거했습니다. 이제 텍스트만 남습니다.\n";
            }
            msg += startButtonCreated
                ? "- 라운드 HUD 바로 옆에 'Start' 버튼을 새로 만들었습니다. 준비 시간이 남아있어도 배치를 " +
                  "미리 끝냈으면 이 버튼을 눌러 바로 전투 단계로 넘어갈 수 있습니다.\n"
                : "- 기존 'Start' 버튼을 그대로 사용합니다.\n";
            msg += "\n라운드마다 준비 시간(기본 30초)이 다 되거나 Start 버튼을 누르면 전투 단계로 넘어갑니다 " +
                   "(RoundManager.Instance.IsPreparing이 false가 됨). 아직 실제 전투 시뮬레이션은 없어서, " +
                   "테스트로 Play 중에 N 키를 누르면 그 라운드를 이긴 것으로 처리하고 다음 라운드로 넘어갑니다 " +
                   "(다음 라운드의 준비 시간이 자동으로 다시 시작됩니다).";

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");

            Selection.activeGameObject = hudGO;
        }

        /// <summary>
        /// 이미 씬에 있는 RoundHUD에 Start 버튼만 추가한다(다이얼로그 없이).
        /// 자동화 스크립트에서 메뉴(SetupRoundHUD)를 거치지 않고 바로 호출하기 위한 진입점.
        /// RoundHUD가 씬에 없으면 아무 것도 하지 않고 false를 반환한다.
        /// </summary>
        public static bool EnsureStartButtonForExistingHud(out string message)
        {
            var hud = Object.FindFirstObjectByType<RoundHUD>();
            if (hud == null)
            {
                message = "씬에 RoundHUD가 없습니다.";
                return false;
            }

            bool created = EnsureStartButton(hud.gameObject, hud, out GameObject buttonGO);
            message = created
                ? $"Start 버튼을 새로 만들었습니다 ({buttonGO.name})."
                : "이미 Start 버튼이 있어서 참조만 다시 연결했습니다.";
            return true;
        }

        private static Canvas EnsureCanvas(out bool created)
        {
            var existing = Object.FindFirstObjectByType<Canvas>();
            if (existing != null)
            {
                created = false;
                return existing;
            }

            var canvasGO = new GameObject("UI", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create UI Canvas");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            MarkDirty(canvasGO);
            created = true;
            return canvas;
        }

        private static bool EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                // 이 프로젝트는 새 Input System 전용이라, 혹시 레거시 StandaloneInputModule이
                // 붙어 있으면 InputSystemUIInputModule로 바꿔준다.
                var legacy = existing.GetComponent<StandaloneInputModule>();
                if (legacy != null) Object.DestroyImmediate(legacy);
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                MarkDirty(existing.gameObject);
                return false;
            }

            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            MarkDirty(go);
            return true;
        }

        private static bool EnsureRoundManager(out GameObject managerGO)
        {
            var existing = Object.FindFirstObjectByType<RoundManager>();
            if (existing != null)
            {
                managerGO = existing.gameObject;
                return false;
            }

            managerGO = new GameObject("RoundManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create RoundManager");
            var manager = Undo.AddComponent<RoundManager>(managerGO);
            manager.maxRound = MaxRound;
            manager.prepTimeLimit = DefaultPrepTimeLimit;
            MarkDirty(managerGO);
            return true;
        }

        private static bool EnsureRoundHUDPanel(Canvas canvas, out GameObject hudGO, out bool barRemoved)
        {
            barRemoved = false;

            var existingHud = Object.FindFirstObjectByType<RoundHUD>();
            if (existingHud != null)
            {
                hudGO = existingHud.gameObject;
                barRemoved = RemoveProgressBarIfPresent(existingHud);
                return false;
            }

            var sprite = LoadOrCreatePanelSprite();

            // 패널 루트 (화면 상단 중앙)
            hudGO = new GameObject("RoundHUD", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(hudGO, "Create RoundHUD");
            hudGO.transform.SetParent(canvas.transform, false);

            var rootRect = hudGO.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -24f);
            rootRect.sizeDelta = new Vector2(320f, PanelHeight);

            var bg = hudGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.6f);

            // 라운드 텍스트 (패널 전체를 채우는 텍스트 하나만 사용)
            var textGO = new GameObject("RoundText", typeof(RectTransform));
            textGO.transform.SetParent(hudGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            var text = textGO.AddComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Round 1 / " + MaxRound;

            var hud = hudGO.AddComponent<RoundHUD>();
            hud.roundText = text;
            hud.progressFill = null;

            MarkDirty(hudGO);
            return true;
        }

        /// <summary>
        /// 예전 버전의 도구가 만들어둔 진행도 바(ProgressBarBG/ProgressBarFill)가 남아 있으면 지우고,
        /// 패널 높이와 텍스트 영역을 텍스트만 있는 레이아웃으로 다시 맞춘다.
        /// 이미 정리되어 있으면 아무것도 하지 않는다.
        /// </summary>
        private static bool RemoveProgressBarIfPresent(RoundHUD hud)
        {
            var panelTransform = hud.transform;
            var barBg = panelTransform.Find("ProgressBarBG");
            if (barBg == null) return false;

            Undo.DestroyObjectImmediate(barBg.gameObject);
            hud.progressFill = null;

            var rootRect = panelTransform.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(rootRect.sizeDelta.x, PanelHeight);
            }

            var textTransform = panelTransform.Find("RoundText");
            if (textTransform != null && textTransform.GetComponent<RectTransform>() is RectTransform textRect)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12f, 4f);
                textRect.offsetMax = new Vector2(-12f, -4f);
            }

            MarkDirty(hud.gameObject);
            return true;
        }

        /// <summary>
        /// 라운드 HUD 패널 바로 오른쪽에 'Start' 버튼을 만든다. 준비 시간이 남아있어도 배치를
        /// 미리 끝냈으면 눌러서 바로 전투 단계로 넘어갈 수 있다(RoundHUD.OnClickStart -> RoundManager.StartBattlePhase).
        /// 이미 있으면 새로 만들지 않고 hud.startButton 참조만 다시 연결한다(재실행 안전).
        /// </summary>
        private static bool EnsureStartButton(GameObject hudGO, RoundHUD hud, out GameObject buttonGO)
        {
            var canvasTransform = hudGO.transform.parent;
            var existing = canvasTransform != null ? canvasTransform.Find("RoundStartButton") : null;
            if (existing != null)
            {
                buttonGO = existing.gameObject;
                var existingButton = buttonGO.GetComponent<Button>();
                if (existingButton != null && hud != null) hud.startButton = existingButton;
                return false;
            }

            var sprite = LoadOrCreatePanelSprite();
            var font = GetDefaultFont();
            var hudRect = hudGO.GetComponent<RectTransform>();

            buttonGO = new GameObject("RoundStartButton", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(buttonGO, "Create Round Start Button");
            buttonGO.transform.SetParent(canvasTransform, false);

            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = hudRect.anchorMin;
            rect.anchorMax = hudRect.anchorMax;
            rect.pivot = hudRect.pivot;
            float offsetX = hudRect.sizeDelta.x * 0.5f + StartButtonGap + StartButtonWidth * 0.5f;
            rect.anchoredPosition = hudRect.anchoredPosition + new Vector2(offsetX, 0f);
            rect.sizeDelta = new Vector2(StartButtonWidth, hudRect.sizeDelta.y);

            var image = buttonGO.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.25f, 0.75f, 0.35f, 0.95f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.font = font;
            label.fontSize = 22;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "Start";

            if (hud != null)
            {
                button.onClick.AddListener(hud.OnClickStart);
                hud.startButton = button;
            }

            MarkDirty(buttonGO);
            return true;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static Sprite LoadOrCreatePanelSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);
            if (existing != null) return existing;

            EnsureFolder("Assets/Textures/UI");

            const int size = 64;
            const int radius = 20;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectAlpha(x, y, size, radius);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(PanelTexturePath, png);
            AssetDatabase.ImportAsset(PanelTexturePath);

            var importer = AssetImporter.GetAtPath(PanelTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spriteBorder = new Vector4(radius, radius, radius, radius);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);
        }

        private static float RoundedRectAlpha(int x, int y, int size, int radius)
        {
            float dx = 0f, dy = 0f;
            if (x < radius) dx = radius - x - 0.5f;
            else if (x >= size - radius) dx = x - (size - radius) + 0.5f;
            if (y < radius) dy = radius - y - 0.5f;
            else if (y >= size - radius) dy = y - (size - radius) + 0.5f;
            if (dx <= 0f || dy <= 0f) return 1f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - dist + 0.5f);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void MarkDirty(GameObject go)
        {
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
#endif
