using UnityEngine;
using UnityEngine.UI;

namespace AnimalChess.Game
{
    /// <summary>
    /// 상점 왼쪽의 플레이어 레벨 / 코스트별 확률 / 골드 표시를 담당한다.
    /// (경험치 표시는 없음.)
    /// </summary>
    public class ShopStatusUI : MonoBehaviour
    {
        public Text levelText;
        public Text xpText; // 더 이상 사용하지 않음 (예전 버전 호환용으로만 필드를 남겨둠).

        [Tooltip("코스트별 확률(%) 텍스트 5개. 인덱스 0이 1코스트, 인덱스 4가 5코스트다. " +
                 "바로 위 코스트 동전 아이콘 줄(CostIconsRow)과 같은 칸 너비/간격으로 배치되어 있어서, " +
                 "각 숫자가 자기 코스트 아이콘 바로 아래에 오도록 맞춰져 있다.")]
        public Text[] costOddsTexts;

        public Text goldText;
        [Tooltip("새로고침 버튼 라벨. ShopManager.RefreshCost 값이 바뀌어도 항상 최신 값으로 표시된다.")]
        public Text refreshCostText;

        private int _lastLevel = -1;
        private int _lastXp = -1;
        private int _lastGold = -1;
        private bool _refreshLabelSet;

        private void Update()
        {
            if (!_refreshLabelSet && refreshCostText != null)
            {
                refreshCostText.text = $"새로고침 ({ShopManager.RefreshCost})";
                _refreshLabelSet = true;
            }

            var economy = PlayerEconomy.Instance;
            if (economy == null) return;

            bool levelChanged = economy.Level != _lastLevel;
            bool xpChanged = economy.CurrentXP != _lastXp;
            bool goldChanged = economy.Gold != _lastGold;

            if (!levelChanged && !xpChanged && !goldChanged) return;

            _lastLevel = economy.Level;
            _lastXp = economy.CurrentXP;
            _lastGold = economy.Gold;

            if (levelText != null) levelText.text = $"{economy.Level}레벨";

            if (xpText != null)
            {
                xpText.text = economy.IsMaxLevel ? "MAX" : $"{economy.CurrentXP}/{economy.XPToNextLevel}";
            }

            if (costOddsTexts != null)
            {
                var row = LevelOddsTable.GetOddsRow(economy.Level);
                int count = Mathf.Min(row.Length, costOddsTexts.Length);
                for (int i = 0; i < count; i++)
                {
                    if (costOddsTexts[i] != null) costOddsTexts[i].text = $"{row[i]}%";
                }
            }

            // 골드 칸에는 "골드"라는 글자 대신 동전 아이콘을 옆에 두므로, 텍스트는 숫자만 표시한다.
            if (goldText != null) goldText.text = economy.Gold.ToString();
        }
    }
}
