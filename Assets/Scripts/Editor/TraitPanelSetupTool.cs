#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Data;
using AnimalChess.Game;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 화면 왼쪽에, 지금 몇 마리가 모여서 어떤 서식지 시너지가 몇 단계(동/은/금)까지
    /// 활성화됐는지 보여주는 패널(TraitPanel)을 준비해주는 도구.
    ///
    /// 따로 실행할 필요 없이 'Tools > AnimalChess > 상점 UI 만들기'(ShopSetupTool)에서
    /// 자동으로 같이 호출된다. 코스트 동전 아이콘과 같은 스프라이트(CoinIcon.png)를
    /// 재사용하므로, 그 상점 UI 도구가 먼저 한 번은 실행돼 있어야 한다.
    /// </summary>
    public static class TraitPanelSetupTool
    {
        private const string PanelTexturePath = "Assets/Textures/UI/RoundHUDPanel.png";
        private const string CoinTexturePath = "Assets/Textures/UI/CoinIcon.png";
        private const float RowHeight = 26f;
        private const float RowSpacing = 4f;

        /// <summary>이미 만들어져 있으면 아무것도 하지 않는다 (재실행 안전).</summary>
        public static bool EnsureTraitPanel(Canvas canvas)
        {
            var existing = Object.FindFirstObjectByType<TraitPanelUI>();
            if (existing != null)
            {
                EnsureRowHoverBackfill(existing);
                return false;
            }

            var font = GetDefaultFont();
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinTexturePath);

            var habitats = TraitPanelUI.HabitatOrder;
            float totalHeight = habitats.Length * RowHeight + (habitats.Length - 1) * RowSpacing;

            var panelGO = new GameObject("TraitPanel", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panelGO, "Create TraitPanel");
            panelGO.transform.SetParent(canvas.transform, false);

            var rootRect = panelGO.GetComponent<RectTransform>();
            // 화면 왼쪽 중앙, 세로로 서식지 개수만큼(6줄) 들어가는 좁고 긴 패널.
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.offsetMin = new Vector2(16f, -totalHeight / 2f);
            rootRect.offsetMax = new Vector2(176f, totalHeight / 2f);

            var layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var rows = new GameObject[habitats.Length];
            var icons = new Image[habitats.Length];
            var labels = new Text[habitats.Length];
            var hovers = new SynergyRowHover[habitats.Length];

            for (int i = 0; i < habitats.Length; i++)
            {
                var rowGO = new GameObject($"TraitRow_{habitats[i]}", typeof(RectTransform));
                rowGO.transform.SetParent(panelGO.transform, false);
                var rowRect = rowGO.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(0f, RowHeight);

                var bg = rowGO.AddComponent<Image>();
                bg.sprite = panelSprite;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0f, 0f, 0f, 0.55f);

                var iconGO = new GameObject("Icon", typeof(RectTransform));
                iconGO.transform.SetParent(rowGO.transform, false);
                var iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(15f, 0f);
                iconRect.sizeDelta = new Vector2(20f, 20f);
                var icon = iconGO.AddComponent<Image>();
                icon.sprite = coinSprite;
                icon.preserveAspect = true;
                icon.color = new Color(0.72f, 0.45f, 0.2f); // 기본값(동색). 실제 색은 TraitPanelUI가 갱신한다.
                icons[i] = icon;

                var labelGO = new GameObject("Label", typeof(RectTransform));
                labelGO.transform.SetParent(rowGO.transform, false);
                var labelRect = labelGO.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(32f, 0f);
                labelRect.offsetMax = new Vector2(-6f, 0f);
                var label = labelGO.AddComponent<Text>();
                label.font = font;
                label.fontSize = 14;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.text = $"{HabitatDisplay.GetKoreanName(habitats[i])} 0/6";
                labels[i] = label;

                // 마우스를 올리면 이 서식지의 동/은/금 3단계 효과를 전부 보여주는 툴팁 트리거.
                // 배경 Image(bg)가 raycastTarget=true(기본값)라서 이 줄 전체가 마우스 이벤트를 받는다.
                hovers[i] = rowGO.AddComponent<SynergyRowHover>();

                rowGO.SetActive(false); // 처음엔 다 꺼둔다 (실제로 2마리 이상 모인 시너지만 보이게).
                rows[i] = rowGO;
            }

            var panelUI = panelGO.AddComponent<TraitPanelUI>();
            panelUI.rows = rows;
            panelUI.rowIcons = icons;
            panelUI.rowLabels = labels;
            panelUI.rowHovers = hovers;

            EditorUtility.SetDirty(panelGO);
            EditorSceneManager.MarkSceneDirty(panelGO.scene);
            return true;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        /// <summary>
        /// 이 도구가 예전에 만들어둔(마우스 오버 툴팁 기능이 생기기 전) 패널이라면, 각 줄에
        /// SynergyRowHover가 빠져 있을 수 있으니 채워 넣는다. 재실행 안전.
        /// </summary>
        private static void EnsureRowHoverBackfill(TraitPanelUI panelUI)
        {
            if (panelUI.rows == null) return;

            bool needsBackfill = panelUI.rowHovers == null || panelUI.rowHovers.Length != panelUI.rows.Length;
            if (!needsBackfill)
            {
                foreach (var h in panelUI.rowHovers)
                {
                    if (h == null) { needsBackfill = true; break; }
                }
            }
            if (!needsBackfill) return;

            var hovers = new SynergyRowHover[panelUI.rows.Length];
            for (int i = 0; i < panelUI.rows.Length; i++)
            {
                if (panelUI.rows[i] == null) continue;
                var hover = panelUI.rows[i].GetComponent<SynergyRowHover>();
                if (hover == null) hover = panelUI.rows[i].AddComponent<SynergyRowHover>();
                hovers[i] = hover;
            }
            panelUI.rowHovers = hovers;
            EditorUtility.SetDirty(panelUI);
            EditorSceneManager.MarkSceneDirty(panelUI.gameObject.scene);
        }
    }
}
#endif
