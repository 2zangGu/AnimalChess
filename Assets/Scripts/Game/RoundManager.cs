using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimalChess.Game
{
    /// <summary>
    /// 게임의 라운드 진행 상태를 관리한다. 시간 제한은 없고,
    /// 정해진 마지막 라운드(기본 30)까지 라운드 번호만 올라간다.
    ///
    /// 한 라운드가 끝나면(EndRound) 항상:
    /// 1) 고정 골드 + 생존 유닛 1마리당 추가 골드를 지급하고 (PlayerEconomy.GrantRoundEndGold)
    /// 2) 전투 중 죽은 것으로 표시된 유닛을 강등/제거한다 (PlayerRoster.ProcessDeaths)
    ///
    /// 그 라운드를 졌다면(승리 못하면) 추가로:
    /// - 보스 라운드(bossRounds, 기본 10/20/30)면 남은 목숨과 상관없이 바로 게임 오버
    /// - 그게 아니면 목숨을 1개 깎는다 (PlayerLives.LoseLife) → 목숨이 0이 되면 그때 게임 오버
    ///
    /// 게임 오버가 아니면 라운드 번호가 하나 올라간다.
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
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // 이미 게임 오버 상태면 테스트 키 입력을 더 받지 않는다.
            if (PlayerLives.Instance != null && PlayerLives.Instance.IsGameOver) return;

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
        /// 한 라운드(전투)를 종료한다. 실제 웨이브 클리어/실패 판정이 생기면
        /// 그 결과에 따라 이걸 호출해주면 된다 (이기면 true, 지면 false).
        /// </summary>
        public void EndRound(bool won)
        {
            if (PlayerLives.Instance != null && PlayerLives.Instance.IsGameOver) return;

            int aliveUnits = PlayerRoster.Instance != null ? PlayerRoster.Instance.CountAliveUnits() : 0;
            PlayerEconomy.Instance?.GrantRoundEndGold(aliveUnits);
            PlayerRoster.Instance?.ProcessDeaths();

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
        }

        public void SetRound(int round)
        {
            CurrentRound = Mathf.Clamp(round, 1, Mathf.Max(1, maxRound));
        }
    }
}
