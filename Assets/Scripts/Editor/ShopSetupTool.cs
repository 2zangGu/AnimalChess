#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Game;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 롤토체스 스타일 상점 UI(플레이어 레벨/확률/골드 + 새로고침 + 유닛 구매 칸 5개)를
    /// 화면 하단에 준비해주는 도구. 레벨업 버튼은 의도적으로 만들지 않는다.
    ///
    /// 'Tools > AnimalChess > 라운드 HUD 만들기'를 먼저 실행해서 Canvas/EventSystem이
    /// 씬에 있어야 한다 (그 Canvas를 그대로 재사용한다).
    /// 상점에 나올 동물 목록은 'Tools > AnimalChess > 동물 로스터(50계통) 만들기'로 미리 만들어둬야 한다.
    ///
    /// 메뉴: Tools > AnimalChess > 상점 UI 만들기
    /// </summary>
    public static class ShopSetupTool
    {
        private const string PanelTexturePath = "Assets/Textures/UI/RoundHUDPanel.png";
        private const string HeartTexturePath = "Assets/Textures/UI/HeartIcon.png";
        private const string CoinTexturePath = "Assets/Textures/UI/CoinIcon.png";
        private const int SlotCount = 5;
        private const int BenchSize = 9;
        private const float GoldPanelWidth = 104f;
        private const float GoldPanelHeight = 56f;

        // 코스트 동전 아이콘 줄(CostIconsRow)과 확률(%) 숫자 줄(OddsRow)이 같은 칸 너비/간격을
        // 써야 숫자와 아이콘이 세로로 정확히 겹쳐 보인다("확률 바로 위에 코스트 아이콘").
        private const float OddsColumnWidth = 34f;
        private const float OddsColumnSpacing = 4f;

        private static readonly Color[] CostColors =
        {
            new Color(0.75f, 0.75f, 0.75f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.3f, 0.55f, 1f),
            new Color(0.7f, 0.35f, 0.9f),
            new Color(1f, 0.8f, 0.15f),
        };

        [MenuItem("Tools/AnimalChess/상점 UI 만들기")]
        public static void SetupShop()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "AnimalChess",
                    "씬에 UI Canvas가 없습니다.\n\n먼저 'Tools > AnimalChess > 라운드 HUD 만들기'를 실행해서 " +
                    "Canvas/EventSystem을 만들어주세요. 그 다음에 이 도구를 다시 실행하면 됩니다.",
                    "확인");
                return;
            }

            bool economyCreated = EnsurePlayerEconomy(out _);
            bool rosterCreated = EnsurePlayerRoster(out _);
            bool livesCreated = EnsurePlayerLives(out _);
            bool shopManagerCreated = EnsureShopManager(out _);
            bool gameOverCreated = EnsureGameOverPanel(canvas, out GameObject gameOverPanelGO);
            bool hudCreated = EnsureShopHUD(canvas, gameOverPanelGO, out GameObject hudGO);
            bool benchCreated = EnsureBenchHUD(canvas, out GameObject benchGO);
            // 시너지 패널은 코스트 동전 스프라이트(CoinIcon.png)를 재사용하므로,
            // 그 스프라이트를 만드는 EnsureShopHUD보다 반드시 뒤에서 호출해야 한다.
            bool traitPanelCreated = TraitPanelSetupTool.EnsureTraitPanel(canvas);
            // 종족 시너지 패널은 서식지 시너지 패널 바로 아래에 붙으므로, 그 패널이 먼저 만들어진
            // 뒤에 호출해야 한다.
            bool speciesTraitPanelCreated = SpeciesTraitPanelSetupTool.EnsureSpeciesTraitPanel(canvas);
            // 시너지 툴팁도 두 패널이 먼저 만들어진(또는 이미 있는) 뒤에 호출해야
            // 각 줄의 SynergyRowHover 백필이 제대로 동작한다.
            bool synergyTooltipCreated = SynergyTooltipSetupTool.EnsureSynergyTooltip(canvas);

            string msg = "상점 UI를 씬에 준비했습니다.\n\n";
            msg += economyCreated
                ? "- 'PlayerEconomy'(레벨/경험치/골드)를 새로 만들었습니다.\n"
                : "- 기존 'PlayerEconomy'를 그대로 사용합니다.\n";
            msg += rosterCreated
                ? "- 'PlayerRoster'(보유 유닛/벤치)를 새로 만들었습니다.\n"
                : "- 기존 'PlayerRoster'를 그대로 사용합니다.\n";
            msg += livesCreated
                ? "- 'PlayerLives'(목숨 3개)를 새로 만들었습니다.\n"
                : "- 기존 'PlayerLives'를 그대로 사용합니다.\n";
            msg += shopManagerCreated
                ? "- 'ShopManager'를 새로 만들었습니다.\n"
                : "- 기존 'ShopManager'를 그대로 사용합니다.\n";
            msg += hudCreated
                ? "- 화면 하단에 상점 UI(목숨 하트/레벨/확률/골드/새로고침/유닛 5칸)를 새로 만들었습니다.\n"
                : "- 기존 상점 UI를 그대로 사용합니다 (목숨 하트가 없었다면 추가했습니다).\n";
            msg += benchCreated
                ? $"- 상점 위에 유닛 보관 칸(벤치) {BenchSize}칸을 새로 만들었습니다.\n"
                : "- 기존 벤치 UI를 그대로 사용합니다.\n";
            msg += gameOverCreated
                ? "- 게임 오버 화면(목숨이 다 떨어지거나 보스 라운드를 지면 표시)을 새로 만들었습니다.\n"
                : "- 기존 게임 오버 화면을 그대로 사용합니다.\n";
            msg += traitPanelCreated
                ? "- 화면 왼쪽에 서식지 시너지 패널(2마리 동/4마리 은/6마리 금)을 새로 만들었습니다.\n"
                : "- 기존 시너지 패널을 그대로 사용합니다.\n";
            msg += speciesTraitPanelCreated
                ? "- 서식지 시너지 패널 바로 아래에 종족 시너지 패널을 새로 만들었습니다.\n"
                : "- 기존 종족 시너지 패널을 그대로 사용합니다.\n";
            msg += synergyTooltipCreated
                ? "- [신규] 시너지 패널 줄에 마우스를 올리면 동/은/금 3단계 효과를 전부 보여주는 " +
                  "말풍선(툴팁)을 시너지 패널 오른쪽에 새로 만들었습니다.\n"
                : "- 기존 시너지 툴팁을 그대로 사용합니다(줄에 연결이 빠져 있었다면 이번에 다시 연결했습니다).\n";
            msg += "\n※ 상점/벤치 칸에 동물 아이콘을 표시하도록 칸 배치를 다시 잡았습니다. " +
                   "'Tools > AnimalChess > 동물 아이콘 임포트 및 연결'을 실행해야 실제로 그림이 보입니다.\n" +
                   "※ 경험치 표시는 없앴고, 골드는 상점 오른쪽 끝에 작게 붙는 칸으로 따로 뺐습니다.\n" +
                   "※ 골드 칸엔 '골드' 글자 대신 금색 동전 아이콘이 붙고, 리롤 확률 바로 위엔 1~5코스트를 " +
                   "나타내는 동전 아이콘 5개(코스트별 색상)가 추가됐습니다.\n" +
                   "※ 화면 왼쪽 시너지 패널(위: 서식지, 아래: 종족)은 보드에 배치된 유닛만 세어서, " +
                   "1마리만 있어도 회색으로 보여주고 2마리 이상부터 동/은/금 순으로 활성화됩니다(2/4/6마리 기준). " +
                   "이제 서식지·종족 시너지 모두 실제 전투 스탯/효과에 반영됩니다(자세한 수치는 TraitSynergy.cs 참고). " +
                   "종족: 포유류/양서류 HP↑, 어류/곤충 공속↑, 파충류 방어력↑, 조류 공격력↑ (그 종족 유닛에게만 적용). " +
                   "서식지: 숲 아군 HP↑, 바다 아군 공속↑, 늪 적 공속↓, 사막 아군 받는 피해↓, 초원 전투 시작 시 " +
                   "근접 아군 일부를 적진으로 기습 이동, 극지 적의 첫 공격을 지연 (모두 아군/적 전체에 적용).\n" +
                   "※ 라운드가 끝나면 고정 골드 5원 + 살아남은 내 유닛 1마리당 1원이 지급됩니다.\n" +
                   "※ 목숨은 3개로 시작하고, 일반 라운드를 지면 하트가 1개 줄어듭니다.\n" +
                   "※ 10/20/30라운드(보스 라운드)를 지면 남은 목숨과 상관없이 그 자리에서 바로 게임 오버입니다.\n" +
                   "※ 새로고침 비용은 1골드, 레벨별 경험치 곡선은 테스트용 임시 값이라 나중에 얼마든지 조정할 수 있습니다.\n" +
                   "※ [수정] 새로고침 버튼을 눌러도 반응이 없던 문제를 고쳤습니다 (버튼 클릭 연결 방식이 " +
                   "씬 저장/재컴파일 후 사라지는 방식이었던 게 원인이었습니다). 레벨 옆 배치 마릿수(0/2 형태) 표시도 " +
                   "혹시 연결이 끊겨있었다면 이번에 다시 연결되도록 했습니다.\n" +
                   "※ 전투 중 죽은 유닛은 다음 라운드 준비 시간에 3성→2성→1성 순으로 강등되고, 1성이면 완전히 사라집니다.\n" +
                   "※ 유닛 재고(같은 동물을 몇 마리까지 살 수 있는지) 제한은 아직 없습니다.\n" +
                   "※ 연승/연패 보너스 없이 진행하기로 해서, 승/패 연승 아이콘은 넣지 않았습니다.\n" +
                   "※ 아직 실제 전투 시스템이 없어서, Play 중 N 키로 라운드 승리 처리를, L 키로 라운드 패배 처리를(목숨 감소/보스면 즉시 게임 오버), " +
                   "M 키로 벤치 유닛 하나를 무작위로 '전투 중 사망'시키는 테스트를 해볼 수 있습니다.\n\n" +
                   "상점에 동물이 안 뜬다면 'Tools > AnimalChess > 동물 로스터(50계통) 만들기'를 먼저 실행했는지 확인해주세요.";

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");
            Selection.activeGameObject = hudGO;
        }

        private static bool EnsurePlayerEconomy(out GameObject go)
        {
            var existing = Object.FindFirstObjectByType<PlayerEconomy>();
            if (existing != null) { go = existing.gameObject; return false; }

            go = new GameObject("PlayerEconomy");
            Undo.RegisterCreatedObjectUndo(go, "Create PlayerEconomy");
            Undo.AddComponent<PlayerEconomy>(go);
            MarkDirty(go);
            return true;
        }

        private static bool EnsurePlayerRoster(out GameObject go)
        {
            var existing = Object.FindFirstObjectByType<PlayerRoster>();
            if (existing != null) { go = existing.gameObject; return false; }

            go = new GameObject("PlayerRoster");
            Undo.RegisterCreatedObjectUndo(go, "Create PlayerRoster");
            Undo.AddComponent<PlayerRoster>(go);
            MarkDirty(go);
            return true;
        }

        private static bool EnsurePlayerLives(out GameObject go)
        {
            var existing = Object.FindFirstObjectByType<PlayerLives>();
            if (existing != null) { go = existing.gameObject; return false; }

            go = new GameObject("PlayerLives");
            Undo.RegisterCreatedObjectUndo(go, "Create PlayerLives");
            Undo.AddComponent<PlayerLives>(go);
            MarkDirty(go);
            return true;
        }

        private static bool EnsureShopManager(out GameObject go)
        {
            var existing = Object.FindFirstObjectByType<ShopManager>();
            if (existing != null) { go = existing.gameObject; return false; }

            go = new GameObject("ShopManager");
            Undo.RegisterCreatedObjectUndo(go, "Create ShopManager");
            Undo.AddComponent<ShopManager>(go);
            MarkDirty(go);
            return true;
        }

        private static bool EnsureShopHUD(Canvas canvas, GameObject gameOverPanelGO, out GameObject hudGO)
        {
            // 예전 버그로 Canvas 바로 밑에 잘못 생성됐던 화면 전체 높이짜리 골드 바가 남아있으면 정리한다.
            CleanupStrayGoldPanel(canvas);

            var existingStatus = Object.FindFirstObjectByType<ShopStatusUI>();
            if (existingStatus != null)
            {
                // ShopStatusUI는 ShopHUD 오브젝트 자신에 붙어있는 컴포넌트다 (자식이 아니라).
                // 예전에는 여기서 .transform.parent를 잘못 타고 올라가 Canvas를 가리키는 바람에
                // 아래의 하트/골드칸/새로고침 라벨 보정이 전부 엉뚱한 곳(Canvas)에 적용되고 있었다.
                hudGO = existingStatus.gameObject;
                ReflowStatusPanel(hudGO);
                EnsureCostIconsRow(hudGO);
                existingStatus.costOddsTexts = EnsureOddsRow(hudGO, GetDefaultFont());
                EnsureLivesUI(hudGO, gameOverPanelGO, GetDefaultFont());
                EnsureGoldPanel(hudGO, GetDefaultFont(), LoadPanelSprite());
                EnsureRefreshLabelBinding(hudGO);
                EnsureRefreshButtonBinding(hudGO);
                EnsureLevelTextBinding(hudGO);
                EnsureShopSlotIcons(hudGO);
                return false;
            }

            var font = GetDefaultFont();
            var sprite = LoadPanelSprite();

            // 루트 패널: 화면 하단 중앙, 1000 x 150
            hudGO = new GameObject("ShopHUD", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(hudGO, "Create ShopHUD");
            hudGO.transform.SetParent(canvas.transform, false);

            var rootRect = hudGO.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.offsetMin = new Vector2(-500f, 16f);
            rootRect.offsetMax = new Vector2(500f, 166f);

            var bg = hudGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.65f);

            // ---- 왼쪽: 레벨 / 확률 / 새로고침 (경험치 표시는 없음, 연승연패 보너스도 없음) ----
            var statusPanel = CreateRect(hudGO.transform, "StatusPanel",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(16f, 10f), new Vector2(216f, -10f));

            var levelText = CreateText(statusPanel, "LevelText",
                new Vector2(0f, 0.66f), new Vector2(1f, 1f),
                new Vector2(4f, 2f), new Vector2(-4f, -2f),
                font, 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, "1레벨");
            // 목숨 하트 자리 때문에 이 칸의 anchorMin.x가 나중에(EnsureLivesUI) 오른쪽으로 줄어들어
            // 폭이 좁아질 수 있는데, 그 상태에서 "1레벨 0/2"처럼 줄바꿈되면 세로로 잘려서 아예 안
            // 보이는 문제가 있었다. 줄바꿈/세로 잘림 없이 항상 전체가 보이도록 강제한다.
            levelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            levelText.verticalOverflow = VerticalWrapMode.Overflow;

            // 확률(%) 숫자 5개는 EnsureOddsRow가 만든다 (아래에서 호출). 코스트 아이콘 줄과
            // 정확히 같은 칸 너비/간격을 써야 숫자가 자기 코스트 아이콘 바로 밑에 오기 때문에,
            // 여기서 텍스트 하나로 만들지 않고 별도 헬퍼로 뺐다.

            var refreshButtonRect = CreateRect(statusPanel, "RefreshButton",
                new Vector2(0f, 0f), new Vector2(1f, 0.33f),
                new Vector2(4f, 2f), new Vector2(-4f, -2f));
            var refreshImage = refreshButtonRect.gameObject.AddComponent<Image>();
            refreshImage.sprite = sprite;
            refreshImage.type = Image.Type.Sliced;
            refreshImage.color = new Color(0.2f, 0.45f, 0.85f, 0.9f);
            var refreshButton = refreshButtonRect.gameObject.AddComponent<Button>();
            refreshButton.targetGraphic = refreshImage;
            var refreshLabel = CreateText(refreshButtonRect.transform, "Label",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                font, 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, $"새로고침 ({ShopManager.RefreshCost})");
            // 주의: 여기서 onClick.AddListener로 직접 붙이지 않는다. 에디터 스크립트에서 붙인 리스너는
            // '영구 저장(persistent)'되지 않아서 씬 저장/스크립트 재컴파일(도메인 리로드) 후에 사라진다
            // (그래서 "새로고침 버튼이 안 눌린다" 버그가 있었다). 대신 ShopStatusUI.refreshButton에
            // 참조만 연결해두면, ShopStatusUI.Awake()가 Play 모드가 시작될 때마다 새로 연결해준다.

            // ---- 오른쪽: 구매 가능한 유닛 5칸 (골드 칸 너비만큼 오른쪽에 여백을 둔다) ----
            var slotsRow = CreateRect(hudGO.transform, "SlotsRow",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(232f, 10f), new Vector2(-(16f + GoldPanelWidth + 8f), -10f));
            var layout = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < SlotCount; i++)
            {
                CreateShopSlot(slotsRow.transform, i, font, sprite);
            }

            var statusUI = hudGO.AddComponent<ShopStatusUI>();
            statusUI.levelText = levelText;
            statusUI.refreshCostText = refreshLabel;
            statusUI.refreshButton = refreshButton;

            EnsureCostIconsRow(hudGO);
            statusUI.costOddsTexts = EnsureOddsRow(hudGO, font);
            EnsureLivesUI(hudGO, gameOverPanelGO, font);
            EnsureGoldPanel(hudGO, font, sprite);
            EnsureShopSlotIcons(hudGO);

            MarkDirty(hudGO);
            return true;
        }

        /// <summary>
        /// 리롤 확률(OddsRow) 바로 위에, 1~5코스트를 나타내는 작은 금색 동전 아이콘 5개를
        /// 코스트 색상(CostColors)으로 물들여서 한 줄로 붙인다. 확률 밴드의 위쪽 절반을 이 줄에
        /// 내주고, 아래쪽 절반에 확률(%) 글자가 남도록 두 밴드를 다시 나눈다.
        /// 이미 만들어져 있으면 칸 크기만 다시 맞춘다 (재실행 안전).
        /// </summary>
        private static void EnsureCostIconsRow(GameObject hudGO)
        {
            var statusPanelTransform = hudGO.transform.Find("StatusPanel");
            if (statusPanelTransform == null) return;

            var existingRow = statusPanelTransform.Find("CostIconsRow");
            bool isNew = existingRow == null;

            RectTransform iconsRow = isNew
                ? CreateRect(statusPanelTransform, "CostIconsRow",
                    new Vector2(0f, 0.5f), new Vector2(1f, 0.66f),
                    new Vector2(4f, 0f), new Vector2(-4f, 0f))
                : existingRow.GetComponent<RectTransform>();

            // 하트 줄(LivesRow)과 똑같은 방식: 레이아웃이 자식 크기를 건드리지 않고(스프라이트의
            // 실제 픽셀 크기에 좌우되지 않게) 고정 크기(sizeDelta)로만 배치해서, 5칸이 항상
            // 똑같은 크기로 대칭이 맞게 나오도록 한다. 예전에 만들어둔 줄이 있으면(스프라이트
            // 원본 크기에 좌우되던 방식이라 칸 크기가 들쭉날쭉했다) 설정을 다시 맞춘다.
            var layout = iconsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = iconsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = OddsColumnSpacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // 칸 너비는 아래 확률(%) 숫자 칸(OddsColumnWidth)과 똑같이 맞춘다. 그래야 이 줄과
            // OddsRow가 둘 다 가운데 정렬된 채로 겹쳐서, i번째 동전이 i번째 숫자 바로 위에 온다.
            if (isNew)
            {
                var coinSprite = LoadOrCreateCoinSprite();
                for (int i = 0; i < CostColors.Length; i++)
                {
                    var iconGO = new GameObject($"CostIcon{i + 1}", typeof(RectTransform));
                    iconGO.transform.SetParent(iconsRow.transform, false);
                    var iconRect = iconGO.GetComponent<RectTransform>();
                    iconRect.sizeDelta = new Vector2(OddsColumnWidth, 18f);

                    var img = iconGO.AddComponent<Image>();
                    img.sprite = coinSprite;
                    img.preserveAspect = true;
                    img.color = CostColors[i];
                }
            }
            else
            {
                // 이미 만들어진 아이콘들의 크기를 고정값으로 다시 맞춰서 대칭/정렬을 바로잡는다.
                for (int i = 0; i < iconsRow.childCount; i++)
                {
                    if (iconsRow.GetChild(i).GetComponent<RectTransform>() is RectTransform childRect)
                    {
                        childRect.sizeDelta = new Vector2(OddsColumnWidth, 18f);
                    }
                }
            }

            MarkDirty(hudGO);
        }

        /// <summary>
        /// 확률(%) 숫자 5개를, 바로 위 코스트 동전 아이콘 줄(CostIconsRow)과 정확히 같은 칸
        /// 너비/간격으로 배치한다. 예전 버전의 한 줄짜리 문자열 텍스트("OddsText")가 남아있으면
        /// 지우고 새로 만든다. 이미 만들어져 있으면 칸 크기만 다시 맞추고 재사용한다 (재실행 안전).
        /// </summary>
        private static Text[] EnsureOddsRow(GameObject hudGO, Font font)
        {
            var statusPanelTransform = hudGO.transform.Find("StatusPanel");
            if (statusPanelTransform == null) return null;

            // 예전 버전의 단일 문자열 확률 텍스트가 남아있으면 제거한다 (칸별 정렬이 불가능한 방식이었다).
            var oldOddsText = statusPanelTransform.Find("OddsText");
            if (oldOddsText != null) Object.DestroyImmediate(oldOddsText.gameObject);

            var existingRow = statusPanelTransform.Find("OddsRow");
            bool isNew = existingRow == null;

            RectTransform oddsRow = isNew
                ? CreateRect(statusPanelTransform, "OddsRow",
                    new Vector2(0f, 0.33f), new Vector2(1f, 0.5f),
                    new Vector2(4f, 0f), new Vector2(-4f, 0f))
                : existingRow.GetComponent<RectTransform>();

            var layout = oddsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = oddsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = OddsColumnSpacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var texts = new Text[CostColors.Length];
            if (isNew)
            {
                for (int i = 0; i < CostColors.Length; i++)
                {
                    var textGO = new GameObject($"OddsValue{i + 1}", typeof(RectTransform));
                    textGO.transform.SetParent(oddsRow.transform, false);
                    var textRect = textGO.GetComponent<RectTransform>();
                    textRect.sizeDelta = new Vector2(OddsColumnWidth, 20f);

                    var text = textGO.AddComponent<Text>();
                    text.font = font;
                    text.fontSize = 14;
                    text.fontStyle = FontStyle.Normal;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = new Color(1f, 0.85f, 0.4f);
                    text.text = "0%";
                    texts[i] = text;
                }
            }
            else
            {
                // 이미 만들어져 있으면 칸 크기만 최신값으로 맞추고 그대로 재사용한다.
                for (int i = 0; i < oddsRow.childCount && i < texts.Length; i++)
                {
                    var child = oddsRow.GetChild(i);
                    if (child.GetComponent<RectTransform>() is RectTransform textRect)
                    {
                        textRect.sizeDelta = new Vector2(OddsColumnWidth, 20f);
                    }
                    texts[i] = child.GetComponent<Text>();
                }
            }

            MarkDirty(hudGO);
            return texts;
        }

        /// <summary>
        /// 골드 표시를 유닛 구매 칸과 겹치지 않게, 상점 오른쪽 끝에 바로 붙는 작은 전용 칸으로
        /// 분리해서 붙인다. ShopHUD 전체 높이만큼 늘어나지 않도록 point anchor로 고정폭/고정높이
        /// 박스를 만들고 세로 중앙에 맞춘다. 이미 만들어져 있으면 아무것도 하지 않는다 (재실행 안전).
        /// 예전 버전에서 만들었던, 유닛 칸과 겹쳐 보이던 우상단 'GoldText'가 남아있으면 지운다.
        /// </summary>
        private static void EnsureGoldPanel(GameObject hudGO, Font font, Sprite sprite)
        {
            var statusUI = hudGO.GetComponent<ShopStatusUI>();

            var existingPanel = hudGO.transform.Find("GoldPanel");
            if (existingPanel != null)
            {
                EnsureGoldCoinIcon(existingPanel, font, statusUI);
                return;
            }

            // 예전 버전에서 상점 칸과 겹쳐 보이던 우상단 골드 텍스트가 있으면 제거한다.
            var oldGoldText = hudGO.transform.Find("GoldText");
            if (oldGoldText != null) Object.DestroyImmediate(oldGoldText.gameObject);

            // 유닛 구매 칸(SlotsRow)이 골드 칸 자리를 침범하지 않도록 오른쪽 여백을 확보한다.
            var slotsRowTransform = hudGO.transform.Find("SlotsRow");
            if (slotsRowTransform != null && slotsRowTransform.GetComponent<RectTransform>() is RectTransform slotsRect)
            {
                slotsRect.offsetMax = new Vector2(-(16f + GoldPanelWidth + 8f), slotsRect.offsetMax.y);
            }

            // point anchor(1, 0.5): ShopHUD 오른쪽 끝, 세로 중앙에 고정폭 x 고정높이 작은 박스로 붙인다.
            // (anchorMin==anchorMax라서 offsetMin/offsetMax가 화면 크기와 무관하게 그대로 위치/크기가 된다.)
            var goldPanel = CreateRect(hudGO.transform, "GoldPanel",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-(16f + GoldPanelWidth), -(GoldPanelHeight / 2f)),
                new Vector2(-16f, GoldPanelHeight / 2f));
            var goldBg = goldPanel.gameObject.AddComponent<Image>();
            goldBg.sprite = sprite;
            goldBg.type = Image.Type.Sliced;
            goldBg.color = new Color(0.3f, 0.24f, 0.05f, 0.85f);

            EnsureGoldCoinIcon(goldPanel, font, statusUI);

            MarkDirty(hudGO);
        }

        /// <summary>
        /// 골드 칸 안에 "골드"라는 글자 대신 금색 동전 아이콘을 놓고, 그 옆에 숫자만 표시되게
        /// 구성한다. 이미 동전 아이콘이 있으면 아무것도 하지 않는다 (재실행 안전). 예전 버전처럼
        /// "골드 50" 글자만 칸 전체를 채우고 있었다면, 아이콘을 추가하고 숫자 텍스트 자리를
        /// 오른쪽으로 옮긴다.
        /// </summary>
        private static void EnsureGoldCoinIcon(Transform goldPanel, Font font, ShopStatusUI statusUI)
        {
            const float iconSize = 30f;
            const float pad = 6f;

            if (goldPanel.Find("GoldIcon") == null)
            {
                var iconRect = CreateRect(goldPanel, "GoldIcon",
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(pad, -iconSize / 2f), new Vector2(pad + iconSize, iconSize / 2f));
                var iconImg = iconRect.gameObject.AddComponent<Image>();
                iconImg.sprite = LoadOrCreateCoinSprite();
                iconImg.preserveAspect = true;
                iconImg.color = new Color(1f, 0.82f, 0.25f);
            }

            var goldTextTransform = goldPanel.Find("GoldText");
            Text goldText;
            if (goldTextTransform == null)
            {
                goldText = CreateText(goldPanel, "GoldText",
                    Vector2.zero, Vector2.one,
                    new Vector2(pad + iconSize + 4f, 2f), new Vector2(-6f, -2f),
                    font, 16, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f), "50");
            }
            else
            {
                goldText = goldTextTransform.GetComponent<Text>();
                if (goldTextTransform.GetComponent<RectTransform>() is RectTransform textRect)
                {
                    // 예전엔 아이콘이 없어서 텍스트가 칸 전체를 차지했을 수 있으니, 아이콘 자리만큼
                    // 왼쪽 여백을 내준다.
                    textRect.offsetMin = new Vector2(pad + iconSize + 4f, textRect.offsetMin.y);
                }
                if (goldText != null && goldText.text.Contains("골드"))
                {
                    // "골드 50" 같은 옛 글자 형식이면 숫자만 남긴다 (다음 프레임에 실제 값으로 갱신됨).
                    goldText.text = "50";
                }
            }

            if (statusUI != null) statusUI.goldText = goldText;
        }

        /// <summary>
        /// 금색 동전 모양(원형, 테두리가 살짝 어두운 입체 느낌) 스프라이트를 그려서 저장한다.
        /// 흰색 계열 그레이스케일로 그려두고, Image.color로 색을 입혀서 쓴다(하트 아이콘과 같은 방식) —
        /// 골드 칸에서는 금색으로, 코스트 아이콘 줄에서는 코스트별 색(CostColors)으로 재사용할 수 있다.
        /// 이미 만들어져 있으면 그대로 재사용한다.
        /// </summary>
        private static Sprite LoadOrCreateCoinSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(CoinTexturePath);
            if (existing != null) return existing;

            EnsureFolder("Assets/Textures/UI");

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = (size - 1) / 2f;
            float outerRadius = size * 0.46f;
            float innerRadius = size * 0.34f;
            const float rimSoftness = 2f;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = px - center;
                    float dy = py - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01((outerRadius - dist) / rimSoftness + 0.5f);
                    // 안쪽은 밝게, 테두리 쪽은 살짝 어둡게 명암을 넣어서 동전처럼 입체감을 준다.
                    float shade = dist < innerRadius ? 1f : 0.7f;
                    pixels[py * size + px] = new Color(shade, shade, shade, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(CoinTexturePath, png);
            AssetDatabase.ImportAsset(CoinTexturePath);

            var importer = AssetImporter.GetAtPath(CoinTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(CoinTexturePath);
        }

        /// <summary>
        /// Canvas 바로 밑에 직접 붙어있는 'GoldPanel'을 지운다. 정상적인 골드 칸은 항상
        /// ShopHUD의 자식이어야 하므로, Canvas 바로 밑에 있는 동명의 오브젝트는 예전
        /// 버그(hudGO가 Canvas로 잘못 계산되던 문제)의 잔재로 보고 제거한다. 화면 전체
        /// 높이로 늘어나 보이던 골드 바가 바로 이 잔재였다. 이미 없으면 아무것도 하지 않는다.
        /// </summary>
        private static void CleanupStrayGoldPanel(Canvas canvas)
        {
            var stray = canvas.transform.Find("GoldPanel");
            if (stray != null) Object.DestroyImmediate(stray.gameObject);
        }

        /// <summary>
        /// 예전 버전(경험치 표시가 있던 4단 레이아웃: 레벨/경험치/확률/새로고침)의 상점 UI를
        /// 최신 3단 레이아웃(레벨/확률/새로고침, 경험치 없음)으로 정리한다. 경험치 텍스트가
        /// 없으면(이미 최신이면) 밴드 값만 다시 맞추고 별다른 변화는 없다 (재실행 안전).
        /// </summary>
        private static void ReflowStatusPanel(GameObject hudGO)
        {
            var statusPanelTransform = hudGO.transform.Find("StatusPanel");
            if (statusPanelTransform == null) return;

            var xpTransform = statusPanelTransform.Find("XPText");
            if (xpTransform != null)
            {
                Object.DestroyImmediate(xpTransform.gameObject);
                var statusUI = hudGO.GetComponent<ShopStatusUI>();
                if (statusUI != null) statusUI.xpText = null;
            }

            SetVerticalBand(statusPanelTransform.Find("LevelText"), 0.66f, 1f);
            // 확률 밴드(0.33~0.5)와 그 위 코스트 아이콘 밴드(0.5~0.66)는 각각
            // EnsureOddsRow/EnsureCostIconsRow가 직접 만들어서 알아서 맞는 위치에 놓는다.
            SetVerticalBand(statusPanelTransform.Find("RefreshButton"), 0f, 0.33f);

            MarkDirty(hudGO);
        }

        /// <summary>x축 anchor(가로 위치/폭)는 그대로 두고, y축 밴드(세로 위치/높이)만 바꾼다.
        /// LevelText는 목숨 하트 자리를 위해 anchorMin.x가 0.4로 줄어들어 있을 수 있는데,
        /// 그 값을 그대로 보존하기 위해 x는 건드리지 않는다.</summary>
        private static void SetVerticalBand(Transform t, float yMin, float yMax)
        {
            if (t == null) return;
            if (t.GetComponent<RectTransform>() is RectTransform rect)
            {
                rect.anchorMin = new Vector2(rect.anchorMin.x, yMin);
                rect.anchorMax = new Vector2(rect.anchorMax.x, yMax);
            }
        }

        /// <summary>
        /// SetVerticalBand와 달리 anchorMin/Max뿐 아니라 offsetMin/Max(픽셀 여백)까지 좌우 6px만
        /// 남기고 완전히 새로 리셋한다. 원래 다른 용도(더 큰 밴드)로 만들어졌던 텍스트를 아주 얇은
        /// 밴드로 재배치할 때 옛날 픽셀 오프셋이 남아있으면 높이가 음수/0이 돼서 안 보이게 되는
        /// 문제를 막기 위한 용도다.
        /// </summary>
        private static void ResetTextBand(Transform t, float yMin, float yMax, float sidePadding = 6f)
        {
            if (t == null) return;
            if (t.GetComponent<RectTransform>() is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0f, yMin);
                rect.anchorMax = new Vector2(1f, yMax);
                rect.offsetMin = new Vector2(sidePadding, 0f);
                rect.offsetMax = new Vector2(-sidePadding, 0f);
            }
        }

        /// <summary>
        /// 새로고침 버튼 라벨을 ShopStatusUI에 연결해서, ShopManager.RefreshCost 값이 바뀌어도
        /// 버튼 글자가 옛날 값으로 굳어있지 않고 항상 최신 값을 보여주게 한다. 재실행 안전.
        /// </summary>
        private static void EnsureRefreshLabelBinding(GameObject hudGO)
        {
            var statusUI = hudGO.GetComponent<ShopStatusUI>();
            if (statusUI == null || statusUI.refreshCostText != null) return;

            var labelTransform = hudGO.transform.Find("StatusPanel/RefreshButton/Label");
            if (labelTransform != null)
            {
                statusUI.refreshCostText = labelTransform.GetComponent<Text>();
                MarkDirty(hudGO);
            }
        }

        /// <summary>
        /// 새로고침 버튼을 ShopStatusUI.refreshButton에 연결해서, Play 모드가 시작될 때마다
        /// ShopStatusUI.Awake()가 onClick 리스너를 다시 붙여주게 한다. (예전에는 이 도구가
        /// 버튼을 만들 때 직접 onClick.AddListener를 호출했는데, 그 리스너는 영구 저장되지 않아서
        /// 씬 저장/스크립트 재컴파일 후에는 사라져 "눌러도 반응 없음" 버그가 있었다.) 재실행 안전.
        /// </summary>
        private static void EnsureRefreshButtonBinding(GameObject hudGO)
        {
            var statusUI = hudGO.GetComponent<ShopStatusUI>();
            if (statusUI == null || statusUI.refreshButton != null) return;

            var buttonTransform = hudGO.transform.Find("StatusPanel/RefreshButton");
            if (buttonTransform != null)
            {
                statusUI.refreshButton = buttonTransform.GetComponent<Button>();
                MarkDirty(hudGO);
            }
        }

        /// <summary>
        /// 레벨 텍스트를 ShopStatusUI.levelText에 연결한다. 정상적인 경우 새로 만들 때 이미
        /// 연결되어 있지만, 혹시라도 연결이 끊긴(레벨/배치 가능 마릿수 표시가 하나도 안 보이는)
        /// 예전 씬을 다시 여는 경우를 대비한 안전장치다. 재실행 안전.
        /// </summary>
        private static void EnsureLevelTextBinding(GameObject hudGO)
        {
            var statusUI = hudGO.GetComponent<ShopStatusUI>();
            if (statusUI == null) return;

            var labelTransform = hudGO.transform.Find("StatusPanel/LevelText");
            if (labelTransform == null) return;

            if (statusUI.levelText == null)
            {
                statusUI.levelText = labelTransform.GetComponent<Text>();
                MarkDirty(hudGO);
            }

            // 레벨 옆 "0/2" 배치 마릿수 표시가 목숨 하트 자리 때문에 좁아진 칸 폭 안에서 줄바꿈되면
            // 두 번째 줄이 칸 높이에 가려 안 보이는 문제가 있었다. 이미 만들어져 있던 예전 씬에도
            // 적용되도록, 참조가 이미 있어도 오버플로우 설정만은 매번 강제로 다시 맞춘다.
            var levelTextComponent = labelTransform.GetComponent<Text>();
            if (levelTextComponent != null)
            {
                levelTextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
                levelTextComponent.verticalOverflow = VerticalWrapMode.Overflow;
                MarkDirty(hudGO);
            }
        }

        /// <summary>
        /// StatusPanel의 레벨 텍스트 왼쪽에 하트 모양 아이콘 3개짜리 목숨 표시를 붙이고,
        /// LivesHUD 컴포넌트를 연결한다. 이미 붙어 있으면 아무것도 하지 않는다 (재실행 안전).
        /// </summary>
        private static void EnsureLivesUI(GameObject hudGO, GameObject gameOverPanelGO, Font font)
        {
            var existingLivesHUD = hudGO.GetComponent<LivesHUD>();
            if (existingLivesHUD != null)
            {
                if (existingLivesHUD.gameOverPanel == null) existingLivesHUD.gameOverPanel = gameOverPanelGO;
                return;
            }

            var statusPanelTransform = hudGO.transform.Find("StatusPanel");
            if (statusPanelTransform == null) return;

            // LivesRow는 LevelText와 같은 세로 밴드를 써야 한다. StatusPanel 레이아웃이
            // (경험치 제거 등으로) 바뀌어도 하트가 항상 레벨 텍스트와 같은 줄에 오도록,
            // 하드코딩된 밴드 대신 LevelText의 현재 anchor 값을 그대로 읽어서 쓴다.
            var levelTextTransform = statusPanelTransform.Find("LevelText");
            Vector2 livesYBand = new Vector2(0.75f, 1f); // LevelText를 못 찾았을 때의 기본값
            if (levelTextTransform != null && levelTextTransform.GetComponent<RectTransform>() is RectTransform levelRect)
            {
                livesYBand = new Vector2(levelRect.anchorMin.y, levelRect.anchorMax.y);
                // 하트가 들어갈 왼쪽 40%만큼 레벨 텍스트 영역을 줄인다.
                levelRect.anchorMin = new Vector2(0.4f, levelRect.anchorMin.y);
            }

            var livesRow = CreateRect(statusPanelTransform, "LivesRow",
                new Vector2(0f, livesYBand.x), new Vector2(0.4f, livesYBand.y),
                new Vector2(4f, 2f), new Vector2(-2f, -2f));
            var livesLayout = livesRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            livesLayout.spacing = 2f;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;
            livesLayout.childControlWidth = false;
            livesLayout.childControlHeight = false;
            livesLayout.childAlignment = TextAnchor.MiddleLeft;

            var heartSprite = LoadOrCreateHeartSprite();

            var heartImages = new Image[3];
            for (int i = 0; i < heartImages.Length; i++)
            {
                var heartGO = new GameObject($"Heart{i}", typeof(RectTransform));
                heartGO.transform.SetParent(livesRow, false);
                var heartRect = heartGO.GetComponent<RectTransform>();
                heartRect.sizeDelta = new Vector2(20f, 20f);

                var heartImg = heartGO.AddComponent<Image>();
                heartImg.sprite = heartSprite;
                heartImg.preserveAspect = true;
                heartImg.color = new Color(0.95f, 0.15f, 0.2f);
                heartImages[i] = heartImg;
            }

            var livesHUD = hudGO.AddComponent<LivesHUD>();
            livesHUD.heartImages = heartImages;
            livesHUD.gameOverPanel = gameOverPanelGO;

            MarkDirty(hudGO);
        }

        /// <summary>
        /// 실제 하트 모양(수식 기반 벡터 하트)을 픽셀로 그려서 스프라이트로 저장한다.
        /// 색은 흰색으로 만들어두고, LivesHUD가 Image.color로 채워진/빈 상태를 표현한다.
        /// 이미 만들어져 있으면 그대로 재사용한다.
        /// </summary>
        private static Sprite LoadOrCreateHeartSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(HeartTexturePath);
            if (existing != null) return existing;

            EnsureFolder("Assets/Textures/UI");

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    // 하트 음함수 곡선: (x^2+y^2-1)^3 - x^2*y^3 <= 0 이면 하트 내부.
                    // py=0(텍스처 아래쪽)을 y=-1.2(뾰족한 아래쪽 끝)에 대응시켜 하트가 바로 서게 그린다.
                    float x = ((px + 0.5f) / size) * 2.4f - 1.2f;
                    float y = ((py + 0.5f) / size) * 2.4f - 1.2f;
                    float f = Mathf.Pow(x * x + y * y - 1f, 3f) - x * x * y * y * y;
                    pixels[py * size + px] = new Color(1f, 1f, 1f, f <= 0f ? 1f : 0f);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(HeartTexturePath, png);
            AssetDatabase.ImportAsset(HeartTexturePath);

            var importer = AssetImporter.GetAtPath(HeartTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(HeartTexturePath);
        }

        private static bool EnsureGameOverPanel(Canvas canvas, out GameObject panelGO)
        {
            var existingTransform = canvas.transform.Find("GameOverPanel");
            bool created = existingTransform == null;

            if (created)
            {
                var font = GetDefaultFont();

                panelGO = new GameObject("GameOverPanel", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(panelGO, "Create GameOverPanel");
                panelGO.transform.SetParent(canvas.transform, false);

                var rect = panelGO.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var bg = panelGO.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.75f);

                CreateText(panelGO.transform, "GameOverText",
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    font, 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.2f, 0.2f), "게임 오버");

                panelGO.SetActive(false);
                MarkDirty(panelGO);
            }
            else
            {
                panelGO = existingTransform.gameObject;
            }

            // 항상 다른 UI들보다 위에 그려지도록 맨 마지막 자식으로 둔다.
            panelGO.transform.SetAsLastSibling();
            return created;
        }

        private static void CreateShopSlot(Transform parent, int index, Font font, Sprite sprite)
        {
            var slotGO = new GameObject($"ShopSlot{index}", typeof(RectTransform));
            slotGO.transform.SetParent(parent, false);

            var bg = slotGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1f, 1f, 1f, 0.12f);

            var button = slotGO.AddComponent<Button>();
            button.targetGraphic = bg;

            var costPipRect = CreateRect(slotGO.transform, "CostPip",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -18f), new Vector2(18f, -4f));
            var costPip = costPipRect.gameObject.AddComponent<Image>();
            costPip.color = CostColors[Mathf.Clamp(0, 0, CostColors.Length - 1)];

            var nameText = CreateText(slotGO.transform, "NameText",
                new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                new Vector2(6f, 0f), new Vector2(-6f, -18f),
                font, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, "");

            var traitText = CreateText(slotGO.transform, "TraitText",
                new Vector2(0f, 0.25f), new Vector2(1f, 0.55f),
                new Vector2(6f, 0f), new Vector2(-6f, 0f),
                font, 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f), "");

            var costText = CreateText(slotGO.transform, "CostText",
                new Vector2(0f, 0f), new Vector2(1f, 0.25f),
                new Vector2(6f, 2f), new Vector2(-6f, 0f),
                font, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), "");

            var slotUI = slotGO.AddComponent<ShopSlotUI>();
            slotUI.slotIndex = index;
            slotUI.nameText = nameText;
            slotUI.traitText = traitText;
            slotUI.costText = costText;
            slotUI.costPip = costPip;
            slotUI.button = button;
            slotUI.costColors = CostColors;
        }

        private static bool EnsureBenchHUD(Canvas canvas, out GameObject benchGO)
        {
            var existing = Object.FindFirstObjectByType<BenchSlotUI>();
            if (existing != null)
            {
                // existing은 BenchSlot(칸) 자신이고, 그 부모는 BenchSlotsRow다.
                // BenchHUD 루트를 얻으려면 한 번 더 위로 올라가야 한다 (예전엔 여기서 한 단계만
                // 올라가서 BenchSlotsRow를 BenchHUD로 착각하는 버그가 있었다 - ShopHUD 쪽과 같은 종류의 버그).
                // (아래쪽에서 새로 만들 때 쓰는 지역변수 slotsRow와 이름이 겹치면 컴파일 에러가 나서
                // existingSlotsRow로 이름을 다르게 뒀다.)
                var existingSlotsRow = existing.transform.parent;
                benchGO = (existingSlotsRow != null && existingSlotsRow.parent != null)
                    ? existingSlotsRow.parent.gameObject
                    : existing.gameObject;
                EnsureBenchSlotIcons(benchGO);
                return false;
            }

            var font = GetDefaultFont();
            var sprite = LoadPanelSprite();

            // 상점(ShopHUD) 바로 위에 놓이는 벤치 패널: 1000 x 74, ShopHUD 위 10px 여백.
            benchGO = new GameObject("BenchHUD", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(benchGO, "Create BenchHUD");
            benchGO.transform.SetParent(canvas.transform, false);

            var rootRect = benchGO.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.offsetMin = new Vector2(-500f, 176f);
            rootRect.offsetMax = new Vector2(500f, 250f);

            var bg = benchGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            var slotsRow = CreateRect(benchGO.transform, "BenchSlotsRow",
                Vector2.zero, Vector2.one,
                new Vector2(10f, 6f), new Vector2(-10f, -6f));
            var layout = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < BenchSize; i++)
            {
                CreateBenchSlot(slotsRow.transform, i, font, sprite);
            }

            EnsureBenchSlotIcons(benchGO);

            MarkDirty(benchGO);
            return true;
        }

        /// <summary>
        /// SlotsRow 밑의 각 상점 칸(ShopSlot)에 Icon 이미지가 없으면 추가한다. 이름 자리(NameText)는
        /// 동물의 실제 이름(예: "미어캣")을 보여주고, TraitText 자리는 종족·서식지 한 줄과 5개 전투
        /// 스탯(체력/공격력/방어력/공속/사거리) 두 줄을 합쳐 세 줄로 보여주는 용도로 재사용한다.
        /// 코스트(가격)는 그대로 맨 아래에 둔다. 글자가 너무 작아서 안 보인다는 피드백이 있어서 각
        /// 줄 폰트 크기를 키웠다. 이미 아이콘이 있으면 배치만 다시 맞추고 넘어간다 (재실행 안전).
        /// AnimalRosterGenerator로 만든 AnimalData.icon을 ShopSlotUI가 표시할 수 있게 연결하는
        /// 역할도 겸한다.
        /// </summary>
        private static void EnsureShopSlotIcons(GameObject hudGO)
        {
            var slotsRow = hudGO.transform.Find("SlotsRow");
            if (slotsRow == null) return;

            foreach (Transform slot in slotsRow)
            {
                var slotUI = slot.GetComponent<ShopSlotUI>();
                if (slotUI == null) continue;

                var iconTransform = slot.Find("Icon");
                Image icon;
                if (iconTransform == null)
                {
                    var iconRect = CreateRect(slot, "Icon",
                        new Vector2(0f, 0.44f), new Vector2(1f, 1f),
                        new Vector2(6f, 2f), new Vector2(-6f, -2f));
                    iconRect.SetSiblingIndex(0); // 배경 다음, CostPip/글자보다 아래에 그려지도록 맨 앞으로.
                    icon = iconRect.gameObject.AddComponent<Image>();
                    icon.preserveAspect = true;
                    icon.enabled = false;
                }
                else
                {
                    icon = iconTransform.GetComponent<Image>();
                    if (icon == null) icon = iconTransform.gameObject.AddComponent<Image>();
                    SetVerticalBand(iconTransform, 0.44f, 1f);
                }

                // 동물 이름 한 줄 (예: "미어캣"). 아이콘 바로 아래, 종족/서식지·스탯 위에 둔다.
                // NameText는 예전에 카드 맨 위(코스트 배지를 피하려고 위쪽 18px를 비워둠)에 있던
                // 자리라서 offsetMax.y가 -18로 남아있다. 지금은 훨씬 얇은 밴드로 옮기는데 그 -18이
                // 그대로 남아있으면 높이가 음수가 돼서 아예 렌더링이 안 되므로, 위치를 옮길 때마다
                // 픽셀 오프셋도 새로 깨끗하게 리셋해준다.
                var nameTextTransform = slot.Find("NameText");
                if (nameTextTransform != null)
                {
                    nameTextTransform.gameObject.SetActive(true);
                    ResetTextBand(nameTextTransform, 0.34f, 0.44f);
                    var nameTextComp = nameTextTransform.GetComponent<Text>();
                    if (nameTextComp != null)
                    {
                        nameTextComp.fontSize = 13;
                        nameTextComp.fontStyle = FontStyle.Bold;
                        nameTextComp.alignment = TextAnchor.MiddleCenter;
                        nameTextComp.color = Color.white;
                        nameTextComp.resizeTextForBestFit = true;
                        nameTextComp.resizeTextMinSize = 8;
                        nameTextComp.resizeTextMaxSize = 14;
                    }
                }

                // 종족·서식지 한 줄 + 스탯 두 줄, 총 세 줄이라 최소 폰트를 좀 더 낮춰서 세 줄이 다
                // 잘리지 않고 Best Fit으로 줄어들 수 있게 한다.
                var statsTextTransform = slot.Find("TraitText");
                if (statsTextTransform != null)
                {
                    SetVerticalBand(statsTextTransform, 0.14f, 0.34f);
                    var statsTextComp = statsTextTransform.GetComponent<Text>();
                    if (statsTextComp != null)
                    {
                        statsTextComp.fontSize = 12;
                        statsTextComp.alignment = TextAnchor.MiddleCenter;
                        statsTextComp.color = new Color(0.9f, 0.9f, 0.9f);
                        statsTextComp.resizeTextForBestFit = true;
                        statsTextComp.resizeTextMinSize = 6;
                        statsTextComp.resizeTextMaxSize = 12;
                    }
                }

                // 코스트(가격) 숫자. SetVerticalBand만으로는 예전에 이 자리가 더 컸을 때 남은 픽셀
                // 오프셋이 그대로 남아 안 보일 수 있으므로(NameText와 같은 종류의 버그), 여기도
                // ResetTextBand로 앵커와 오프셋을 통째로 새로 잡아서 항상 보이도록 한다.
                var costTextTransform = slot.Find("CostText");
                if (costTextTransform != null)
                {
                    ResetTextBand(costTextTransform, 0f, 0.14f);
                    var costTextComp = costTextTransform.GetComponent<Text>();
                    if (costTextComp != null)
                    {
                        costTextComp.fontStyle = FontStyle.Bold;
                        costTextComp.resizeTextForBestFit = true;
                        costTextComp.resizeTextMinSize = 10;
                        costTextComp.resizeTextMaxSize = 18;
                    }
                }

                slotUI.iconImage = icon;
            }
        }

        /// <summary>
        /// BenchSlotsRow 밑의 각 벤치 칸(BenchSlot)에 Icon 이미지가 없으면 추가해서 칸 전체를 채우고,
        /// 이름/별/코스트 표시는 전부 숨긴다 (벤치는 아이콘만 보이면 된다). 이미 아이콘이 있으면
        /// 배치만 다시 맞추고 넘어간다 (재실행 안전).
        /// </summary>
        private static void EnsureBenchSlotIcons(GameObject benchGO)
        {
            var slotsRow = benchGO.transform.Find("BenchSlotsRow");
            if (slotsRow == null) return;

            foreach (Transform slot in slotsRow)
            {
                var slotUI = slot.GetComponent<BenchSlotUI>();
                if (slotUI == null) continue;

                var iconTransform = slot.Find("Icon");
                Image icon;
                if (iconTransform == null)
                {
                    var iconRect = CreateRect(slot, "Icon",
                        Vector2.zero, Vector2.one,
                        new Vector2(3f, 3f), new Vector2(-3f, -3f));
                    iconRect.SetSiblingIndex(0);
                    icon = iconRect.gameObject.AddComponent<Image>();
                    icon.preserveAspect = true;
                    icon.enabled = false;
                }
                else
                {
                    icon = iconTransform.GetComponent<Image>();
                    if (icon == null) icon = iconTransform.gameObject.AddComponent<Image>();
                    SetVerticalBand(iconTransform, 0f, 1f);
                }

                // 벤치는 아이콘만 보이면 되므로 이름/별/코스트 표시는 전부 숨긴다.
                var nameTextTransform = slot.Find("NameText");
                if (nameTextTransform != null) nameTextTransform.gameObject.SetActive(false);
                var starTextTransform = slot.Find("StarText");
                if (starTextTransform != null) starTextTransform.gameObject.SetActive(false);
                if (slotUI.costPip != null) slotUI.costPip.gameObject.SetActive(false);

                slotUI.iconImage = icon;
            }
        }

        private static void CreateBenchSlot(Transform parent, int index, Font font, Sprite sprite)
        {
            var slotGO = new GameObject($"BenchSlot{index}", typeof(RectTransform));
            slotGO.transform.SetParent(parent, false);

            var bg = slotGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1f, 1f, 1f, 0.1f);

            var costPipRect = CreateRect(slotGO.transform, "CostPip",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(3f, -12f), new Vector2(12f, -3f));
            var costPip = costPipRect.gameObject.AddComponent<Image>();
            costPip.enabled = false;
            costPip.color = CostColors[0];

            var nameText = CreateText(slotGO.transform, "NameText",
                new Vector2(0f, 0.4f), new Vector2(1f, 1f),
                new Vector2(4f, 0f), new Vector2(-4f, -12f),
                font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, "");

            var starText = CreateText(slotGO.transform, "StarText",
                new Vector2(0f, 0f), new Vector2(1f, 0.4f),
                new Vector2(4f, 2f), new Vector2(-4f, 0f),
                font, 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f), "");

            var slotUI = slotGO.AddComponent<BenchSlotUI>();
            slotUI.slotIndex = index;
            slotUI.nameText = nameText;
            slotUI.starText = starText;
            slotUI.costPip = costPip;
            slotUI.costColors = CostColors;
        }

        private static RectTransform CreateRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static Text CreateText(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Font font, int fontSize, FontStyle style, TextAnchor alignment, Color color, string content)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            return text;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static Sprite LoadPanelSprite()
        {
            // RoundHUDSetupTool이 이미 만들어둔 둥근 사각형 패널 텍스처를 재사용한다.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelTexturePath);
            return sprite; // 없으면 null이어도 Image는 단색 사각형으로 표시된다.
        }

        private static void MarkDirty(GameObject go)
        {
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
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
    }
}
#endif
