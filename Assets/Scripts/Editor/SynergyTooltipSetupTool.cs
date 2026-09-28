#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Game;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 화면 왼쪽 시너지 패널(서식지/종족) 줄에 마우스를 올리면 뜨는 설명 말풍선(툴팁) 하나를
    /// 미리 만들어둔다. 어느 줄 위에 있든 이 자리에 뜨고 내용만 바뀐다(SynergyRowHover가
    /// SynergyTooltipUI.Instance.Show/Hide를 호출한다). 시너지 패널들 바로 오른쪽에 자리잡으므로,
    /// TraitPanelSetupTool/SpeciesTraitPanelSetupTool보다 뒤에 호출돼야 한다(ShopSetupTool이
    /// 이미 그 순서로 호출해준다). 재실행 안전.
    /// </summary>
    public static class SynergyTooltipSetupTool
    {
        private const string PanelTexturePath = "Assets/Textures/UI/RoundHUDPanel.png";

        public static bool EnsureSynergyTooltip(Canvas canvas)
        {
            var existing = Object.FindFirstObjectByType<SynergyTooltipUI>();
            if (existing != null) return false;

            var font = GetDefaultFont();
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);

            // 루트 오브젝트는 항상 켜둔다 - SynergyTooltipUI.Awake()가 Play 시작하자마자 실행돼서
            // Instance가 제때 잡혀야 하기 때문이다(비활성 오브젝트는 Awake가 늦게, 또는 영영 안 불린다).
            // 실제 보이고 안 보이고는 자식인 Content만 켰다 껐다 한다.
            var rootGO = new GameObject("SynergyTooltip", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootGO, "Create SynergyTooltip");
            rootGO.transform.SetParent(canvas.transform, false);

            var rootRect = rootGO.GetComponent<RectTransform>();
            // 서식지/종족 시너지 패널(왼쪽, x=16~176) 바로 오른쪽에 자리잡는다.
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.offsetMin = new Vector2(184f, -140f);
            rootRect.offsetMax = new Vector2(444f, 140f);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(rootGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var bg = contentGO.AddComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.88f);
            bg.raycastTarget = false; // 이 패널 자체는 마우스 이벤트를 가로채지 않는다.

            var titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(contentGO.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(12f, -36f);
            titleRect.offsetMax = new Vector2(-12f, -8f);
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.UpperLeft;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            var bodyGO = new GameObject("Body", typeof(RectTransform));
            bodyGO.transform.SetParent(contentGO.transform, false);
            var bodyRect = bodyGO.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(12f, 10f);
            bodyRect.offsetMax = new Vector2(-12f, -40f);
            var bodyText = bodyGO.AddComponent<Text>();
            bodyText.font = font;
            bodyText.fontSize = 14;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = new Color(0.92f, 0.92f, 0.92f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.lineSpacing = 1.2f;
            bodyText.raycastTarget = false;

            var tooltipUI = rootGO.AddComponent<SynergyTooltipUI>();
            tooltipUI.panel = contentGO;
            tooltipUI.titleText = titleText;
            tooltipUI.bodyText = bodyText;

            contentGO.SetActive(false); // 평소엔 숨겨둔다(마우스를 올렸을 때만 SynergyTooltipUI.Show가 켠다).

            EditorUtility.SetDirty(rootGO);
            EditorSceneManager.MarkSceneDirty(rootGO.scene);
            return true;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
#endif
