using UnityEngine;
using UnityEngine.UI;

namespace AnimalChess.Game
{
    /// <summary>
    /// 하트 모양 아이콘 3개로 목숨을 표시하고, 게임 오버가 되면 게임 오버 패널을 띄운다.
    /// (하트 글자(♥/♡) 대신, 실제 하트 모양으로 그려진 스프라이트 이미지를 색만 바꿔서 사용한다 —
    /// 폰트에 하트 글리프가 없어서 글자가 안 보이는 문제를 피하기 위함.)
    /// </summary>
    public class LivesHUD : MonoBehaviour
    {
        public Image[] heartImages;
        public GameObject gameOverPanel;

        private static readonly Color FilledColor = new Color(0.95f, 0.15f, 0.2f);
        private static readonly Color EmptyColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);

        private int _lastLives = -1;
        private bool _lastGameOver;
        private bool _initialized;

        private void Update()
        {
            var lives = PlayerLives.Instance;
            if (lives == null) return;

            bool livesChanged = !_initialized || lives.CurrentLives != _lastLives;
            bool gameOverChanged = !_initialized || lives.IsGameOver != _lastGameOver;
            if (!livesChanged && !gameOverChanged) return;

            _initialized = true;
            _lastLives = lives.CurrentLives;
            _lastGameOver = lives.IsGameOver;

            if (livesChanged) RefreshHearts(lives.CurrentLives);
            if (gameOverChanged && gameOverPanel != null) gameOverPanel.SetActive(lives.IsGameOver);
        }

        private void RefreshHearts(int currentLives)
        {
            if (heartImages == null) return;
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null) continue;
                bool filled = i < currentLives;
                heartImages[i].color = filled ? FilledColor : EmptyColor;
            }
        }
    }
}
