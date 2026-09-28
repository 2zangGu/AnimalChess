using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimalChess.Game
{
    /// <summary>
    /// 게임의 라운드 진행 상태를 관리한다. 시간 제한은 없고,
    /// 정해진 마지막 라운드(기본 30)까지 라운드 번호만 올라간다.
    ///
    /// 라운드마다 "준비 단계"(유닛 배치/상점)가 있고, 이 단계는 prepTimeLimit(기본 30초) 동안
    /// 계속되다가 시간이 다 되면 자동으로, 또는 플레이어가 배치를 미리 끝내고 Start 버튼을
    /// 누르면(StartBattlePhase) 그 즉시 끝난다. 라운드 HUD 옆의 Start 버튼이 이걸 호출한다.
    /// 실제 전투 시뮬레이션은 아직 없어서, 지금은 준비 단계 종료가 "곧 전투 시작"이라는 상태
    /// 표시(IsPreparing=false)만 담당한다.
    ///
    /// 한 라운드가 끝나면(EndRound) 항상:
    /// 1) 고정 골드 + 생존 유닛 1마리당 추가 골드를 지급하고 (PlayerEconomy.GrantRoundEndGold)
    /// 2) 전투 중 죽은 것으로 표시된 유닛을 강등/제거한다 (PlayerRoster.ProcessDeaths)
    ///
    /// 그 라운드를 졌다면(승리 못하면) 추가로:
    /// - 보스 라운드(bossRounds, 기본 10/20/30)면 남은 목숨과 상관없이 바로 게임 오버
    /// - 그게 아니면 목숨을 1개 깎는다 (PlayerLives.LoseLife) → 목숨이 0이 되면 그때 게임 오버
    ///
    /// 게임 오버가 아니면 라운드 번호가 하나 올라가고, 새 라운드의 준비 단계가 다시 시작된다.
    ///
    /// 아직 "웨이브를 다 잡으면 이기고, 못 잡으면 진다" 같은 실제 전투 로직은 없어서,
    /// 그 시스템이 생기면 결과에 따라 EndRound(true)/EndRound(false)를 거기서 호출해주면 된다.
    /// 그 전까지 테스트용으로 Play 중:
    /// - N 키 = 이번 라운드를 이긴 것으로 처리하고 종료 (enableDebugAdvanceKey)
    /// - L 키 = 이번 라운드를 진 것으로 처리하고 종료 (enableDebugLoseKey)
    /// - M 키 = 벤치의 살아있는 유닛 중 하나를 무작위로 "전투 중 사망"으로 표시 (enableDebugKillKey)
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        public static RoundManager Instance { get; private set; }

        [Header("라운드 설정")]
        [Tooltip("마지막 라운드 번호")]
        public int maxRound = 30;

        [Tooltip("시작 라운드 번호 (보통 1)")]
        [SerializeField] private int startingRound = 1;

        [Tooltip("이 라운드에서 지면 남은 목숨과 상관없이 바로 게임 오버가 된다.")]
        public int[] bossRounds = { 10, 20, 30 };

        [Header("준비 시간")]
        [Tooltip("한 라운드마다 유닛을 배치할 수 있는 준비 시간(초). 이 시간이 다 되거나 " +
                 "Start 버튼을 누르면(StartBattlePhase) 준비 단계가 끝난다.")]
        public float prepTimeLimit = 30f;

        [Header("테스트용 (임시)")]
        [Tooltip("웨이브 클리어 판정 로직이 아직 없어서, 테스트로 '승리'를 시뮬레이션할 수 있게 " +
                 "N 키에 임시로 연결해둔 것. 실제 웨이브 시스템이 생기면 꺼도 된다.")]
        public bool enableDebugAdvanceKey = true;

        [Tooltip("L 키를 누르면 이번 라운드를 '패배'로 처리한다 (목숨 감소 / 보스 라운드면 즉시 게임 오버 테스트용).")]
        public bool enableDebugLoseKey = true;

        [Tooltip("M 키를 누르면 벤치의 살아있는 유닛 중 하나를 무작위로 사망 처리한다 " +
                 "(실제 전투 시스템이 생기기 전까지 강등/제거 로직 테스트용).")]
        public bool enableDebugKillKey = true;

        public int CurrentRound { get; private set; }

        /// <summary>지금 준비 단계(배치/상점) 중인지. false면 전투 단계.</summary>
        public bool IsPreparing { get; private set; } = true;

        /// <summary>준비 단계에서 남은 시간(초). 준비 단계가 아니면 0.</summary>
        public float PrepTimeRemaining { get; private set; }

        public bool IsLastRound => CurrentRound >= maxRound;

        public bool IsBossRound(int round)
        {
            if (bossRounds == null) return false;
            for (int i = 0; i < bossRounds.Length; i++)
            {
                if (bossRounds[i] == round) return true;
            }
            return false;
        }

        public bool IsCurrentRoundBoss => IsBossRound(CurrentRound);

        private void Awake()
        {
            Instance = this;
            CurrentRound = Mathf.Clamp(startingRound, 1, Mathf.Max(1, maxRound));
            EnsureCombatManager();
            BeginPreparation();
        }

        /// <summary>
        /// 준비 단계가 끝나면 자동으로 전투(타겟팅/이동/공격/사망 판정)를 시뮬레이션하는 CombatManager를
        /// 자동으로 붙여준다. BoardManager의 EnsureXxx 헬퍼들과 같은 자기 자신 프로비저닝 패턴.
        /// </summary>
        private void EnsureCombatManager()
        {
            if (GetComponent<CombatManager>() == null)
            {
                gameObject.AddComponent<CombatManager>();
            }
        }

        private void Update()
        {
            bool isGameOver = PlayerLives.Instance != null && PlayerLives.Instance.IsGameOver;

            if (IsPreparing && !isGameOver)
            {
                PrepTimeRemaining -= Time.deltaTime;
                if (PrepTimeRemaining <= 0f)
                {
                    StartBattlePhase();
                }
            }

            if (Keyboard.current == null) return;

            // 이미 게임 오버 상태면 테스트 키 입력을 더 받지 않는다.
            if (isGameOver) return;

            if (enableDebugAdvanceKey && Keyboard.current.nKey.wasPressedThisFrame)
            {
                EndRound(won: true);
            }

            if (enableDebugLoseKey && Keyboard.current.lKey.wasPressedThisFrame)
            {
                EndRound(won: false);
            }

            if (enableDebugKillKey && Keyboard.current.mKey.wasPressedThisFrame)
            {
                PlayerRoster.Instance?.DebugKillRandomUnit();
            }
        }

        /// <summary>
        /// 새 라운드의 준비 단계(배치/상점)를 시작한다. 남은 시간을 prepTimeLimit로 초기화한다.
        /// </summary>
        private void BeginPreparation()
        {
            IsPreparing = true;
            PrepTimeRemaining = prepTimeLimit;

            // 라운드 번호에 따라 자동으로 레벨을 맞춘다(PlayerEconomy.SetLevelForRound 참고).
            PlayerEconomy.Instance?.SetLevelForRound(CurrentRound);
        }

        /// <summary>
        /// 준비 단계를 끝내고 전투 단계로 넘어간다. 준비 시간이 다 됐을 때 자동으로 호출되거나,
        /// 라운드 HUD 옆의 Start 버튼을 눌러서(미리 배치를 끝냈을 때) 수동으로 호출할 수 있다.
        /// 이미 준비 단계가 아니면(이미 시작됐으면) 아무 일도 하지 않는다.
        /// </summary>
        public void StartBattlePhase()
        {
            if (!IsPreparing)
            {
                Debug.LogWarning("[RoundManager] StartBattlePhase 호출됨 - 이미 준비 단계가 아니라서 무시합니다.");
                return;
            }
            Debug.LogWarning("[RoundManager] StartBattlePhase 호출됨 - 준비 단계를 종료합니다.");

            // 전투가 시작되기 직전, 지금 배치를 기억해둔다. 전투가 끝나면 이 자리로 되돌아간다
            // (PlayerRoster.RestorePrepPhasePositions 참고).
            PlayerRoster.Instance?.SnapshotBoardPositions();

            IsPreparing = false;
            PrepTimeRemaining = 0f;
        }

        /// <summary>
        /// 한 라운드(전투)를 종료한다. 실제 웨이브 클리어/실패 판정이 생기면
        /// 그 결과에 따라 이걸 호출해주면 된다 (이기면 true, 지면 false).
        /// </summary>
        public void EndRound(bool won)
        {
            if (PlayerLives.Instance != null && PlayerLives.Instance.IsGameOver) return;

            int aliveUnits = PlayerRoster.Instance != null ? PlayerRoster.Instance.CountAliveUnits() : 0;
            PlayerEconomy.Instance?.GrantRoundEndGold(aliveUnits);
            PlayerRoster.Instance?.ProcessDeaths();

            // 전투 중 자동 이동으로 흐트러진 배치를, 이번 전투를 시작하기 직전(그 전 준비 단계 때)
            // 놓여 있던 자리로 되돌린다.
            PlayerRoster.Instance?.RestorePrepPhasePositions();

            if (!won && PlayerLives.Instance != null)
            {
                if (IsCurrentRoundBoss)
                {
                    PlayerLives.Instance.TriggerGameOver();
                }
                else
                {
                    PlayerLives.Instance.LoseLife();
                }
            }

            if (PlayerLives.Instance != null && PlayerLives.Instance.IsGameOver)
            {
                return; // 게임 오버면 다음 라운드로 넘어가지 않는다.
            }

            SetRound(CurrentRound + 1);
            BeginPreparation();

            // 새 라운드 준비 단계가 시작될 때마다 상점 5칸을 전부 새로 뽑는다(무료 새로고침).
            // 골드가 드는 수동 새로고침(ShopManager.TryRefresh)과 달리, 라운드가 넘어갈 때는
            // 비용 없이 자동으로 갱신된다.
            ShopManager.Instance?.RollAllSlots();
        }

        public void SetRound(int round)
        {
            CurrentRound = Mathf.Clamp(round, 1, Mathf.Max(1, maxRound));
        }
    }
}
