using UnityEngine;

namespace AnimalChess.Game
{
    /// <summary>
    /// 플레이어의 목숨(하트)을 관리한다. 기본 3개.
    /// 일반 라운드를 지면 1개가 깎이고, 보스 라운드(RoundManager.bossRounds)를 지면
    /// 남은 목숨과 상관없이 바로 게임 오버가 된다.
    /// </summary>
    public class PlayerLives : MonoBehaviour
    {
        public static PlayerLives Instance { get; private set; }

        [Tooltip("시작 목숨 개수")]
        [SerializeField] private int startingLives = 3;

        public int MaxLives => startingLives;
        public int CurrentLives { get; private set; }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            Instance = this;
            CurrentLives = Mathf.Max(0, startingLives);
            IsGameOver = false;
        }

        /// <summary>일반 라운드 패배: 목숨을 1개 깎는다. 0이 되면 게임 오버로 처리한다.</summary>
        public void LoseLife()
        {
            if (IsGameOver) return;

            CurrentLives = Mathf.Max(0, CurrentLives - 1);
            if (CurrentLives <= 0)
            {
                TriggerGameOver();
            }
        }

        /// <summary>보스 라운드 패배 등: 남은 목숨과 상관없이 즉시 게임 오버로 만든다.</summary>
        public void TriggerGameOver()
        {
            CurrentLives = 0;
            IsGameOver = true;
        }
    }
}
