using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        private bool _lastBattleActive;
        private int _lastAlivePlayer = -1;
        private int _lastAliveEnemy = -1;

        /// <summary>
        /// Start 버튼이 왜 안 눌리는지 원인을 찾기 위한 자가 진단. 씬 설정(EventSystem 유무,
        /// 버튼 OnClick() 연결 여부, interactable 초기값)을 게임 시작 시 한 번 콘솔에 찍는다.
        /// 이 진단은 스크립트 코드가 아니라 씬/에디터 쪽 설정 문제를 잡기 위한 것이다.
        /// </summary>
        private void Start()
        {
            if (EventSystem.current == null)
            {
                Debug.LogWarning("[RoundHUD] 씬에 EventSystem이 없습니다! 이러면 버튼을 포함한 모든 UI 클릭이 " +
                                  "전혀 작동하지 않습니다. Hierarchy에서 GameObject > UI > Event System으로 추가해주세요.");
            }
            else
            {
                Debug.LogWarning($"[RoundHUD] EventSystem 확인됨: {EventSystem.current.gameObject.name}");
            }

            if (startButton == null)
            {
                Debug.LogWarning("[RoundHUD] startButton 필드가 Inspector에 연결되어 있지 않습니다! " +
                                  "RoundHUD 컴포넌트의 Start Button 슬롯에 Start 버튼 오브젝트를 드래그해서 넣어주세요.");
                return;
            }

            int listenerCount = startButton.onClick.GetPersistentEventCount();
            if (listenerCount == 0)
            {
                Debug.LogWarning("[RoundHUD] Start 버튼의 OnClick() 리스트가 비어 있습니다! Inspector에서 버튼의 " +
                                  "OnClick() 섹션에 RoundHUD 오브젝트를 넣고 OnClickStart 함수를 연결해주세요.");
            }
            else
            {
                for (int i = 0; i < listenerCount; i++)
                {
                    var target = startButton.onClick.GetPersistentTarget(i);
                    string methodName = startButton.onClick.GetPersistentMethodName(i);
                    Debug.LogWarning($"[RoundHUD] Start 버튼 OnClick()[{i}]: " +
                                      $"{(target != null ? target.GetType().Name : "null")}.{methodName}");
                }
            }

            Debug.LogWarning($"[RoundHUD] Start 버튼 초기 interactable = {startButton.interactable}, " +
                              $"activeInHierarchy = {startButton.gameObject.activeInHierarchy}");
        }

        private void Update()
        {
            var manager = RoundManager.Instance;
            if (manager == null) return;

            int current = manager.CurrentRound;
            int max = manager.maxRound;
            bool isPreparing = manager.IsPreparing;
            int prepSeconds = Mathf.CeilToInt(Mathf.Max(0f, manager.PrepTimeRemaining));

            var combat = CombatManager.Instance;
            bool battleActive = combat != null && combat.IsBattleActive;
            int alivePlayer = combat != null ? combat.AlivePlayerCount : 0;
            int aliveEnemy = combat != null ? combat.AliveEnemyCount : 0;

            bool changed = current != _lastRound || max != _lastMax ||
                           isPreparing != _lastIsPreparing || prepSeconds != _lastPrepSeconds ||
                           battleActive != _lastBattleActive || alivePlayer != _lastAlivePlayer ||
                           aliveEnemy != _lastAliveEnemy;
            if (!changed) return;

            _lastRound = current;
            _lastMax = max;
            _lastIsPreparing = isPreparing;
            _lastPrepSeconds = prepSeconds;
            _lastBattleActive = battleActive;
            _lastAlivePlayer = alivePlayer;
            _lastAliveEnemy = aliveEnemy;

            if (roundText != null)
            {
                if (isPreparing)
                {
                    roundText.text = $"Round {current} / {max}  ({prepSeconds}s)";
                }
                else
                {
                    // 전투 중에는 지금 남아있는 아군/적 수를 같이 보여줘서, 보드에 유닛을 배치하지
                    // 않아 전투 없이 바로 라운드가 끝나는 경우에도 원인을 눈으로 바로 알 수 있게 한다.
                    string combatInfo = battleActive ? $" (아군 {alivePlayer} · 적 {aliveEnemy})" : "";
                    roundText.text = $"Round {current} / {max}  전투 중{combatInfo}";
                }
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
            Debug.LogWarning("[RoundHUD] OnClickStart 호출됨 (버튼 클릭 이벤트가 스크립트까지 도달함).");
            RoundManager.Instance?.StartBattlePhase();
        }
    }
}
