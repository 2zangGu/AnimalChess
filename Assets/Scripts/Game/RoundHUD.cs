using UnityEngine;
using UnityEngine.UI;

namespace AnimalChess.Game
{
    /// <summary>
    /// 화면 상단에 "Round N / 30" 텍스트와 진행도 바, 준비 시간 카운트다운을 표시한다.
    /// 준비 단계 동안은 남은 초를 같이 보여주고, 전투 단계로 넘어가면 "전투 중"으로 바뀐다.
    /// startButton이 연결돼 있으면(옆의 Start 버튼) 준비 단계에서만 누를 수 있게 하고,
    /// 전투 단계로 넘어가면 비활성화한다.
    /// RoundManager의 상태를 매 프레임 확인해서, 값이 바뀌었을 때만 텍스트/바/버튼을 갱신한다.
    /// </summary>
    public class RoundHUD : MonoBehaviour
    {
        public Text roundText;
        public Image progressFill;
        public Button startButton;

        private int _lastRound = -1;
        private int _lastMax = -1;
        private bool _lastIsPreparing;
        private int _lastPrepSeconds = -1;

        private void Update()
        {
            var manager = RoundManager.Instance;
            if (manager == null) return;

            int current = manager.CurrentRound;
            int max = manager.maxRound;
            bool isPreparing = manager.IsPreparing;
            int prepSeconds = Mathf.CeilToInt(Mathf.Max(0f, manager.PrepTimeRemaining));

            bool changed = current != _lastRound || max != _lastMax ||
                           isPreparing != _lastIsPreparing || prepSeconds != _lastPrepSeconds;
            if (!changed) return;

            _lastRound = current;
            _lastMax = max;
            _lastIsPreparing = isPreparing;
            _lastPrepSeconds = prepSeconds;

            if (roundText != null)
            {
                roundText.text = isPreparing
                    ? $"Round {current} / {max}  ({prepSeconds}s)"
                    : $"Round {current} / {max}  전투 중";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = max > 0 ? (float)current / max : 0f;
            }

            if (startButton != null)
            {
                // 준비 단계에서만 누를 수 있다. 전투 단계로 넘어가면(이미 시작됐으므로) 비활성화한다.
                startButton.interactable = isPreparing;
            }
        }

        /// <summary>Start 버튼의 OnClick에 연결한다. 준비 단계를 즉시 끝내고 전투 단계로 넘어간다.</summary>
        public void OnClickStart()
        {
            RoundManager.Instance?.StartBattlePhase();
        }
    }
}
