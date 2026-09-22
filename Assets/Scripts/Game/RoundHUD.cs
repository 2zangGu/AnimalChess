using UnityEngine;
using UnityEngine.UI;

namespace AnimalChess.Game
{
    /// <summary>
    /// 화면 상단에 "Round N / 30" 텍스트와 진행도 바를 표시한다.
    /// 시간 제한이 없는 게임이라 타이머는 따로 없다.
    /// RoundManager의 현재 라운드를 매 프레임 확인해서, 값이 바뀌었을 때만 텍스트/바를 갱신한다.
    /// </summary>
    public class RoundHUD : MonoBehaviour
    {
        public Text roundText;
        public Image progressFill;

        private int _lastRound = -1;
        private int _lastMax = -1;

        private void Update()
        {
            var manager = RoundManager.Instance;
            if (manager == null) return;

            int current = manager.CurrentRound;
            int max = manager.maxRound;
            if (current == _lastRound && max == _lastMax) return;

            _lastRound = current;
            _lastMax = max;

            if (roundText != null)
            {
                roundText.text = $"Round {current} / {max}";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = max > 0 ? (float)current / max : 0f;
            }
        }
    }
}
