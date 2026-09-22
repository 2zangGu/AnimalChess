using UnityEngine;

namespace AnimalChess.Game
{
    /// <summary>
    /// 플레이어의 레벨/경험치/골드를 관리한다.
    /// 라운드가 바뀔 때마다 자동으로 골드가 지급된다 (골드 수급 로직은 추후 튜닝 가능).
    /// </summary>
    public class PlayerEconomy : MonoBehaviour
    {
        public static PlayerEconomy Instance { get; private set; }

        [Header("레벨 / 경험치 (임시값, 추후 조정 가능)")]
        [SerializeField] private int startingLevel = 1;

        [Tooltip("index 0 = 1레벨에서 2레벨로 가는데 필요한 경험치, index 1 = 2->3, ...")]
        public int[] xpToNextLevel = { 2, 6, 10, 20, 36, 56, 80, 84, 100 };

        [Header("골드")]
        [SerializeField] private int startingGold = 50;
        [Tooltip("라운드가 하나 끝날 때마다 고정으로 지급되는 골드")]
        public int goldPerRound = 5;
        [Tooltip("라운드 전투가 끝났을 때, 살아남은 내 유닛 한 마리당 추가로 지급되는 골드")]
        public int goldPerSurvivingUnit = 1;

        public int Level { get; private set; }
        public int CurrentXP { get; private set; }
        public int Gold { get; private set; }

        public int MaxLevel => xpToNextLevel.Length + 1;
        public bool IsMaxLevel => Level >= MaxLevel;

        /// <summary>다음 레벨까지 필요한 경험치. 최대 레벨이면 0.</summary>
        public int XPToNextLevel
        {
            get
            {
                int idx = Level - 1;
                if (idx < 0 || idx >= xpToNextLevel.Length) return 0;
                return xpToNextLevel[idx];
            }
        }

        private void Awake()
        {
            Instance = this;
            Level = Mathf.Clamp(startingLevel, 1, MaxLevel);
            CurrentXP = 0;
            Gold = startingGold;
        }

        /// <summary>
        /// 라운드 하나가 끝났을 때 RoundManager가 호출해준다.
        /// 고정 골드(goldPerRound) + 생존 유닛 수 * goldPerSurvivingUnit 만큼 지급한다.
        /// </summary>
        public void GrantRoundEndGold(int aliveUnitCount)
        {
            int amount = goldPerRound + Mathf.Max(0, aliveUnitCount) * goldPerSurvivingUnit;
            AddGold(amount);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount == 0) return;
            Gold = Mathf.Max(0, Gold + amount);
        }

        public void AddXP(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return;
            CurrentXP += amount;
            while (!IsMaxLevel && CurrentXP >= XPToNextLevel && XPToNextLevel > 0)
            {
                CurrentXP -= XPToNextLevel;
                Level++;
            }
            if (IsMaxLevel) CurrentXP = 0;
        }
    }
}
