using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 유닛 하나(내 동물이든 적이든)의 아이콘과 스탯을 보여주는 정보창.
    /// UnitStatPanelController가 보드 위/벤치의 유닛을 마우스 오른쪽 버튼으로 클릭하면
    /// ShowAnimal/ShowEnemy를 호출해서 내용을 채우고 패널을 켠다.
    /// 닫기(X) 버튼을 누르거나, 유닛이 없는 곳을 오른쪽 클릭하면 Hide()가 호출돼 꺼진다.
    ///
    /// 'Tools &gt; AnimalChess &gt; 유닛 정보창 만들기'(UnitStatPanelSetupTool)가 씬에 준비해준다.
    /// </summary>
    public class UnitStatPanelUI : MonoBehaviour
    {
        public static UnitStatPanelUI Instance { get; private set; }

        public GameObject panelRoot;
        public Image iconImage;
        public Text nameText;
        public Text infoText;
        public Text statsText;

        private void Awake()
        {
            Instance = this;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>내(플레이어) 동물 유닛의 정보를 띄운다.</summary>
        public void ShowAnimal(UnitInstance unit)
        {
            if (unit?.currentData == null) return;
            var data = unit.currentData;

            string stars = new string('★', Mathf.Max(0, unit.StarLevel));
            string info = $"{SpeciesDisplay.GetKoreanName(data.species)} · {HabitatDisplay.GetKoreanName(data.habitat)}\n코스트 {data.cost}";

            Fill(data.icon, string.IsNullOrEmpty(stars) ? data.displayName : $"{data.displayName} {stars}", info, data.baseStats);
        }

        /// <summary>적(인간 웨이브) 유닛의 정보를 띄운다.</summary>
        public void ShowEnemy(EnemyUnitInstance unit)
        {
            if (unit?.currentData == null) return;
            var data = unit.currentData;

            string info = $"{GetTierKoreanName(data.tier)}\n등장 라운드 {data.minRound}~{data.maxRound}";
            Fill(data.icon, data.displayName, info, data.baseStats);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Fill(Sprite icon, string title, string info, UnitStats stats)
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            if (nameText != null) nameText.text = title;
            if (infoText != null) infoText.text = info;
            if (statsText != null)
            {
                statsText.text =
                    $"체력 {stats.hp:0}\n" +
                    $"공격력 {stats.attackPower:0.#}\n" +
                    $"방어력 {stats.defense:0.#}\n" +
                    $"공속 {stats.attackSpeed:0.##}\n" +
                    $"사거리 {stats.attackRange:0.#}";
            }
        }

        private static string GetTierKoreanName(EnemyTier tier)
        {
            switch (tier)
            {
                case EnemyTier.Hunter: return "사냥꾼 (원시 무기)";
                case EnemyTier.Soldier: return "병사 (총기)";
                case EnemyTier.Mechanized: return "기계화 병기";
                default: return tier.ToString();
            }
        }
    }
}
