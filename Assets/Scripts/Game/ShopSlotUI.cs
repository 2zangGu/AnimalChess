using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 상점 칸 하나(구매 카드)를 표시한다. slotIndex로 ShopManager.Slots[slotIndex]를 매 프레임 확인해서
    /// 값이 바뀌었을 때만 UI를 갱신한다.
    /// </summary>
    public class ShopSlotUI : MonoBehaviour
    {
        [Header("연결")]
        public int slotIndex;
        [Tooltip("동물의 실제 이름(예: '미어캣')을 보여준다.")]
        public Text nameText;
        [Tooltip("종족 · 서식지 한 줄 + 5개 전투 스탯(체력/공격력/방어력/공속/사거리)을 총 세 줄로 보여준다.")]
        public Text traitText;
        public Text costText;
        public Image costPip;
        public Image iconImage;
        public Button button;

        [Header("코스트별 색상 (1~5)")]
        public Color[] costColors = new Color[5]
        {
            new Color(0.75f, 0.75f, 0.75f), // 1: 회색
            new Color(0.3f, 0.8f, 0.3f),    // 2: 초록
            new Color(0.3f, 0.55f, 1f),     // 3: 파랑
            new Color(0.7f, 0.35f, 0.9f),   // 4: 보라
            new Color(1f, 0.8f, 0.15f),     // 5: 골드
        };

        private AnimalData _lastAnimal;
        private bool _initialized;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(OnClicked);
        }

        private void Update()
        {
            var manager = ShopManager.Instance;
            if (manager == null) return;
            if (slotIndex < 0 || slotIndex >= manager.Slots.Length) return;

            var current = manager.Slots[slotIndex];
            if (_initialized && current == _lastAnimal) return;

            _initialized = true;
            _lastAnimal = current;
            Refresh(current);
        }

        private void Refresh(AnimalData animal)
        {
            bool empty = animal == null;

            if (nameText != null) nameText.text = empty ? "" : animal.displayName;
            if (traitText != null) traitText.text = empty ? "" : FormatStats(animal);
            if (costText != null) costText.text = empty ? "" : animal.cost.ToString();

            if (costPip != null)
            {
                costPip.enabled = !empty;
                if (!empty)
                {
                    int idx = Mathf.Clamp(animal.cost - 1, 0, costColors.Length - 1);
                    costPip.color = costColors[idx];
                }
            }

            if (iconImage != null)
            {
                var icon = empty ? null : animal.icon;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (button != null) button.interactable = !empty;
        }

        private void OnClicked()
        {
            ShopManager.Instance?.TryBuy(slotIndex);
        }

        /// <summary>
        /// 종족·서식지 한 줄과 5개 전투 스탯을 한글 이름으로 세 줄에 나눠서 보여준다. 숫자와 다음
        /// 글자 사이에 공백을 둬서 값이 두 자리든 세 자리든 읽기 편하게 한다.
        /// 예: "포유류 · 초원" / "체력 100  공격 20  방어 10" / "공속 1.2  사거리 2"
        /// </summary>
        private static string FormatStats(AnimalData animal)
        {
            var s = animal.baseStats;
            return $"{SpeciesDisplay.GetKoreanName(animal.species)} · {HabitatDisplay.GetKoreanName(animal.habitat)}\n" +
                   $"체력 {s.hp:0.#}  공격 {s.attackPower:0.#}  방어 {s.defense:0.#}\n" +
                   $"공속 {s.attackSpeed:0.#}  사거리 {s.attackRange:0.#}";
        }
    }
}
