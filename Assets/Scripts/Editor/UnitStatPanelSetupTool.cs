#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Game;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 보드 위(내 유닛/적 유닛)나 벤치의 유닛을 마우스 오른쪽 버튼으로 클릭했을 때 뜨는
    /// 정보창(아이콘 + 이름 + 스탯)을 씬에 준비해주는 도구.
    /// 이미 있으면 그대로 두고, 없는 것만 새로 만든다(재실행 안전).
    /// 패널 배경 스프라이트는 'Tools &gt; AnimalChess &gt; 라운드 HUD 만들기'가 만들어둔
    /// RoundHUDPanel.png를 재사용하므로, 그 도구가 먼저 한 번은 실행돼 있어야 한다.
    /// 메뉴: Tools &gt; AnimalChess &gt; 유닛 정보창 만들기
    /// </summary>
    public static class UnitStatPanelSetupTool
    {
        private const string PanelTexturePath = "Assets/Textures/UI/RoundHUDPanel.png";
        private const float PanelWidth = 260f;
        private const float PanelHeight = 340f;

        [MenuItem("Tools/AnimalChess/유닛 정보창 만들기")]
        public static void SetupUnitStatPanelMenu()
        {
            EnsureUnitStatPanel(out string message);
            EditorUtility.DisplayDialog("AnimalChess", message, "확인");
        }

        /// <summary>
        /// 다이얼로그 없이 정보창을 준비한다. 자동화 스크립트에서 메뉴를 거치지 않고
        /// 바로 호출하기 위한 진입점. 이미 있으면 아무 것도 하지 않고 false를 반환한다.
        /// </summary>
        public static bool EnsureUnitStatPanel(out string message)
        {
            // 패널은 처음엔 꺼진 채로(SetActive(false)) 만들어지므로, 기본 FindFirstObjectByType(비활성 제외)로는
            // 이미 있는 패널을 못 찾아서 재실행할 때마다 중복 생성되는 문제가 있었다.
            // 비활성 오브젝트도 포함해서 찾아야 재실행 시 안전하다(중복 생성 방지).
            var existing = Object.FindFirstObjectByType<UnitStatPanelUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                message = "이미 유닛 정보창이 있어서 그대로 사용합니다.";
                return false;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                message = "씬에 Canvas가 없습니다. 먼저 다른 UI 도구(상점 UI 만들기 등)를 한 번 실행해주세요.";
                return false;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);
            var font = GetDefaultFont();

            var panelGO = new GameObject("UnitStatPanel", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panelGO, "Create UnitStatPanel");
            panelGO.transform.SetParent(canvas.transform, false);

            var rootRect = panelGO.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0.5f);
            rootRect.anchorMax = new Vector2(1f, 0.5f);
            rootRect.pivot = new Vector2(1f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-16f, 0f);
            rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            // 실제로 보이는 내용(배경+아이콘+텍스트 등)은 전부 별도의 자식 오브젝트(PanelContent)에 넣고,
            // 이 자식만 켰다/껐다 한다. UnitStatPanelUI 스크립트는 항상 켜져 있는 루트(panelGO)에 붙여야
            // Awake()가 확실히 호출돼서 Instance가 제대로 설정된다 — 비활성(SetActive(false))된
            // 오브젝트에 붙은 컴포넌트는 Awake 자체가 호출되지 않기 때문에(유니티의 기본 동작),
            // 루트를 직접 꺼버리면 UnitStatPanelUI.Instance가 계속 null로 남아 정보창이 아예 안 뜨는
            // 문제가 있었다.
            var contentGO = new GameObject("PanelContent", typeof(RectTransform));
            contentGO.transform.SetParent(panelGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var bg = contentGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            // 닫기(X) 버튼 - 오른쪽 위 모서리
            var closeGO = new GameObject("CloseButton", typeof(RectTransform));
            closeGO.transform.SetParent(contentGO.transform, false);
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-6f, -6f);
            closeRect.sizeDelta = new Vector2(24f, 24f);
            var closeImage = closeGO.AddComponent<Image>();
            closeImage.sprite = sprite;
            closeImage.type = Image.Type.Sliced;
            closeImage.color = new Color(0.6f, 0.2f, 0.2f, 0.9f);
            var closeButton = closeGO.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;

            var closeLabelGO = new GameObject("Label", typeof(RectTransform));
            closeLabelGO.transform.SetParent(closeGO.transform, false);
            var closeLabelRect = closeLabelGO.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;
            var closeLabel = closeLabelGO.AddComponent<Text>();
            closeLabel.font = font;
            closeLabel.fontSize = 16;
            closeLabel.fontStyle = FontStyle.Bold;
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.color = Color.white;
            closeLabel.text = "X";

            // 아이콘
            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(contentGO.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -44f);
            iconRect.sizeDelta = new Vector2(96f, 96f);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.preserveAspect = true;

            // 이름 (+ 별 등급)
            var nameGO = new GameObject("NameText", typeof(RectTransform));
            nameGO.transform.SetParent(contentGO.transform, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -150f);
            nameRect.sizeDelta = new Vector2(240f, 28f);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;

            // 부가 정보 (종족/서식지/코스트, 또는 티어/등장 라운드)
            var infoGO = new GameObject("InfoText", typeof(RectTransform));
            infoGO.transform.SetParent(contentGO.transform, false);
            var infoRect = infoGO.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 1f);
            infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.anchoredPosition = new Vector2(0f, -180f);
            infoRect.sizeDelta = new Vector2(240f, 40f);
            var infoText = infoGO.AddComponent<Text>();
            infoText.font = font;
            infoText.fontSize = 13;
            infoText.alignment = TextAnchor.UpperCenter;
            infoText.color = new Color(0.85f, 0.85f, 0.85f);

            // 전투 스탯 (체력/공격력/방어력/공속/사거리)
            var statsGO = new GameObject("StatsText", typeof(RectTransform));
            statsGO.transform.SetParent(contentGO.transform, false);
            var statsRect = statsGO.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 1f);
            statsRect.anchorMax = new Vector2(0.5f, 1f);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.anchoredPosition = new Vector2(0f, -226f);
            statsRect.sizeDelta = new Vector2(220f, 110f);
            var statsText = statsGO.AddComponent<Text>();
            statsText.font = font;
            statsText.fontSize = 15;
            statsText.alignment = TextAnchor.UpperLeft;
            statsText.color = Color.white;
            statsText.lineSpacing = 1.15f;

            var panelUI = panelGO.AddComponent<UnitStatPanelUI>();
            panelUI.panelRoot = contentGO;
            panelUI.iconImage = iconImage;
            panelUI.nameText = nameText;
            panelUI.infoText = infoText;
            panelUI.statsText = statsText;

            closeButton.onClick.AddListener(() => UnitStatPanelUI.Instance?.Hide());

            // panelGO(스크립트가 붙은 루트)는 항상 켜둔 채로, 실제 내용인 contentGO만 꺼서 시작한다.
            contentGO.SetActive(false);

            MarkDirty(panelGO);
            message = "보드 위(내 유닛/적 유닛)나 벤치의 유닛을 마우스 오른쪽 버튼으로 클릭하면 " +
                      "화면 오른쪽에 정보창(아이콘+스탯)이 뜨도록 준비했습니다.";
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
