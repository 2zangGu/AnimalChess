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
    /// 화면 왼쪽에, 지금 몇 마리가 모여서 어떤 종족 시너지가 몇 단계(동/은/금)까지
    /// 활성화됐는지 보여주는 패널(SpeciesTraitPanel)을 준비해주는 도구.
    ///
    /// TraitPanelSetupTool(서식지 시너지 패널)과 완전히 같은 만듦새이고, 그 패널 바로 아래쪽에
    /// 붙는다. 따로 실행할 필요 없이 'Tools > AnimalChess > 상점 UI 만들기'(ShopSetupTool)에서
    /// TraitPanelSetupTool 바로 다음에 자동으로 같이 호출된다.
    /// </summary>
    public static class SpeciesTraitPanelSetupTool
    {
        private const string PanelTexturePath = "Assets/Textures/UI/RoundHUDPanel.png";
        private const string CoinTexturePath = "Assets/Textures/UI/CoinIcon.png";
        private const float RowHeight = 26f;
        private const float RowSpacing = 4f;

        // 서식지 시너지 패널(TraitPanelSetupTool)과 같은 6줄짜리 레이아웃이라 높이가 같고,
        // 그 패널 바로 아래에 이 간격(PanelGap)만큼 띄워서 붙인다.
        private const float PanelGap = 16f;

        /// <summary>이미 만들어져 있으면 아무것도 하지 않는다 (재실행 안전).</summary>
        public static bool EnsureSpeciesTraitPanel(Canvas canvas)
        {
            var existing = Object.FindFirstObjectByType<SpeciesTraitPanelUI>();
            if (existing != null)
            {
                EnsureRowHoverBackfill(existing);
                return false;
            }

            var font = GetDefaultFont();
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinTexturePath);

            var species = SpeciesTraitPanelUI.SpeciesOrder;
            float totalHeight = species.Length * RowHeight + (species.Length - 1) * RowSpacing;

            // 서식지 시너지 패널과 같은 계산식(EnsureTraitPanel 참고)으로 그 패널의 아래쪽 경계를
            // 구한 다음, 그 밑에 이 패널을 붙인다.
            float habitatTotalHeight = totalHeight; // 서식지도 6줄이라 높이가 같다.
            float habitatBottomY = -habitatTotalHeight / 2f;
            float topY = habitatBottomY - PanelGap;
            float bottomY = topY - totalHeight;

            var panelGO = new GameObject("SpeciesTraitPanel", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panelGO, "Create SpeciesTraitPanel");
            panelGO.transform.SetParent(canvas.transform, false);

            var rootRect = panelGO.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.offsetMin = new Vector2(16f, bottomY);
            rootRect.offsetMax = new Vector2(176f, topY);

            var layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var rows = new GameObject[species.Length];
            var icons = new Image[species.Length];
            var labels = new Text[species.Length];
            var hovers = new SynergyRowHover[species.Length];

            for (int i = 0; i < species.Length; i++)
            {
                var rowGO = new GameObject($"SpeciesRow_{species[i]}", typeof(RectTransform));
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
                icon.color = new Color(0.55f, 0.55f, 0.55f); // 기본값(비활성 회색). 실제 색은 SpeciesTraitPanelUI가 갱신한다.
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
                label.text = $"{SpeciesDisplay.GetKoreanName(species[i])} 0/2";
                labels[i] = label;

                // 마우스를 올리면 이 종족의 동/은/금 3단계 효과를 전부 보여주는 툴팁 트리거.
                hovers[i] = rowGO.AddComponent<SynergyRowHover>();

                rowGO.SetActive(false); // 처음엔 다 꺼둔다 (실제로 1마리 이상 모인 종족만 보이게).
                rows[i] = rowGO;
            }

            var panelUI = panelGO.AddComponent<SpeciesTraitPanelUI>();
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
        private static void EnsureRowHoverBackfill(SpeciesTraitPanelUI panelUI)
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
