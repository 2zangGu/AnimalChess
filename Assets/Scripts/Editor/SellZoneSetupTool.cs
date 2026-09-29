#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Game;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 유닛(벤치/보드 어느 쪽이든)을 드래그해서 화면 오른쪽 아래로 갖다놓으면 파는 "판매 칸"을
    /// 씬에 준비해주는 도구. 유닛 정보창(UnitStatPanel) 바로 아래, 화면 오른쪽 아래 빈 공간에
    /// 만든다. 이미 있으면 그대로 두고(중복 생성 안 함), 없는 것만 새로 만든다.
    ///
    /// 실제 판매/골드 지급 로직은 UnitDragController/PlayerRoster에 있고, 이 도구는 그 드롭
    /// 대상이 되는 UI(배경 + 안내 문구 + SellZoneUI 컴포넌트)만 만든다.
    ///
    /// 메뉴: Tools &gt; AnimalChess &gt; 판매 칸 만들기
    /// </summary>
    public static class SellZoneSetupTool
    {
        private const string PanelTexturePath = "Assets/Textures/UI/RoundHUDPanel.png";
        private const float ZoneWidth = 200f;
        private const float ZoneHeight = 150f;

        [MenuItem("Tools/AnimalChess/판매 칸 만들기")]
        public static void SetupSellZone()
        {
            bool created = EnsureSellZone(out GameObject zoneGO);

            string msg = created
                ? "'SellZone'(판매 칸)을 화면 오른쪽 아래에 새로 만들었습니다.\n\n" +
                  "벤치나 보드에 있는 유닛을 드래그해서 이 칸 위에 놓으면 그 유닛이 팔리고 골드를 받습니다.\n" +
                  "판매 금액: 1성은 코스트 그대로, 2성은 (코스트x3)-1, 3성은 (코스트x9)-1."
                : "이미 판매 칸이 있어서 그대로 사용합니다.";

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");
            Selection.activeGameObject = zoneGO;
        }

        private static bool EnsureSellZone(out GameObject zoneGO)
        {
            var existing = Object.FindFirstObjectByType<SellZoneUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                zoneGO = existing.gameObject;
                return false;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                zoneGO = null;
                EditorUtility.DisplayDialog("AnimalChess",
                    "씬에 Canvas가 없습니다. 먼저 다른 UI 도구(상점 UI 만들기 등)를 한 번 실행해주세요.", "확인");
                return false;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);
            var font = GetDefaultFont();

            zoneGO = new GameObject("SellZone", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(zoneGO, "Create SellZone");
            zoneGO.transform.SetParent(canvas.transform, false);

            // 유닛 정보창(화면 오른쪽 중앙)과 겹치지 않게, 화면 오른쪽 아래 구석에 둔다.
            var rect = zoneGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-16f, 16f);
            rect.sizeDelta = new Vector2(ZoneWidth, ZoneHeight);

            var bg = zoneGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(zoneGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 8f);
            labelRect.offsetMax = new Vector2(-8f, -8f);
            var label = labelGO.AddComponent<Text>();
            label.font = font;
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "판매\n(유닛을 여기로 드래그)";

            var zoneUI = zoneGO.AddComponent<SellZoneUI>();
            zoneUI.background = bg;

            MarkDirty(zoneGO);
            return true;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static void MarkDirty(GameObject go)
        {
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
#endif
