using UnityEngine;

namespace AnimalChess.Game
{
    /// <summary>
    /// 플레이어의 레벨/경험치/골드를 관리한다.
    /// 라운드가 바뀔 때마다 자동으로 골드가 지급된다 (골드 수급 로직은 추후 튜닝 가능).
    ///
    /// 레벨은 이제 라운드 번호에 따라 자동으로 오른다(RoundManager.BeginPreparation이 매 라운드
    /// 시작마다 SetLevelForRound를 호출해준다). roundLevelThresholds[i]는 "그 라운드부터"
    /// (i+1)레벨이 된다는 뜻이다 (예: 기본값 기준 1~3라운드=1레벨, 4~6라운드=2레벨, ..., 27라운드
    /// 이후=10레벨). AddXP/xpToNextLevel은 예전 "골드로 경험치를 사는" 방식의 흔적으로, 지금은
    /// 어디서도 호출하지 않지만 나중에 다시 쓸 수 있도록 남겨뒀다.
    /// </summary>
    public class PlayerEconomy : MonoBehaviour
    {
        public static PlayerEconomy Instance { get; private set; }

        [Header("레벨 / 경험치 (임시값, 추후 조정 가능)")]
        [SerializeField] private int startingLevel = 1;

        [Tooltip("index 0 = 1레벨에서 2레벨로 가는데 필요한 경험치, index 1 = 2->3, ...\n" +
                 "(지금은 라운드 기반 자동 레벨업만 쓰여서 실제로는 사용되지 않는다.)")]
        public int[] xpToNextLevel = { 2, 6, 10, 20, 36, 56, 80, 84, 100 };

        [Tooltip("이 라운드 번호부터 (인덱스+1)레벨이 된다. 예: [1,4,7,10,13,16,19,22,25,27]이면\n" +
                 "1~3라운드=1레벨, 4~6라운드=2레벨, 7~9라운드=3레벨, ..., 27라운드 이후=10레벨.\n" +
                 "배열 길이는 MaxLevel(=xpToNextLevel.Length+1)과 같아야 한다.")]
        public int[] roundLevelThresholds = { 1, 4, 7, 10, 13, 16, 19, 22, 25, 27 };

        [Header("골드")]
        [SerializeField] private int startingGold = 50;
        [Tooltip("라운드가 하나 끝날 때마다 고정으로 지급되는 골드")]
        public int goldPerRound = 5;
        [Tooltip("라운드 전투가 끝났을 때, 살아남은 내 유닛 한 마리당 추가로 지급되는 골드")]
        public int goldPerSurvivingUnit = 1;

        [Header("이자")]
        [Tooltip("지금 가진 골드 이 값(기본 10)당 1골드를 이자로 받는다. 연승/연패 보너스는 의도적으로 " +
                 "만들지 않고, 그 대신 후반 골드 부족을 완화하기 위한 장치로 이자만 넣는다. 상한 없음 " +
                 "(예: 200골드 보유 중이면 이자 20골드).")]
        public int goldPerInterest = 10;

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

        private void Start()
        {
            // RoundManager.Awake()가 먼저 실행됐다는 보장이 없으므로(스크립트 실행 순서에 따라
            // PlayerEconomy.Awake()가 먼저 돌 수도 있음), 모든 Awake가 끝난 뒤인 Start 시점에
            // 1라운드 기준 레벨을 한 번 더 맞춰준다. 이후 라운드부터는 RoundManager.BeginPreparation이
            // 매 라운드 시작마다 SetLevelForRound를 호출해준다.
            int round = RoundManager.Instance != null ? RoundManager.Instance.CurrentRound : 1;
            SetLevelForRound(round);
        }

        /// <summary>
        /// 라운드 번호에 맞는 레벨로 맞춘다(roundLevelThresholds 기준). 레벨은 라운드가 진행되는
        /// 동안 자연히 올라가기만 하므로, 계산된 값이 지금 레벨보다 높을 때만 반영한다.
        /// </summary>
        public void SetLevelForRound(int round)
        {
            int level = 1;
            if (roundLevelThresholds != null)
            {
                for (int i = 0; i < roundLevelThresholds.Length; i++)
                {
                    if (round >= roundLevelThresholds[i]) level = i + 1;
                }
            }
            level = Mathf.Clamp(level, 1, MaxLevel);

            if (level > Level) Level = level;
        }

        /// <summary>
        /// 지금 가진 골드 기준으로 받을 수 있는 이자를 계산한다(goldPerInterest 골드당 1골드,
        /// 상한 없음). 라운드가 끝나기 전 상점 UI에서 "다음에 받을 이자가 얼마인지" 미리
        /// 보여줄 때도 이 값을 그대로 쓴다.
        /// </summary>
        public int GetInterest()
        {
            if (goldPerInterest <= 0) return 0;
            return Gold / goldPerInterest;
        }

        /// <summary>
        /// 라운드 하나가 끝났을 때 RoundManager가 호출해준다.
        /// 고정 골드(goldPerRound) + 생존 유닛 수 * goldPerSurvivingUnit + 이자(GetInterest)
        /// 만큼 지급한다. 이자는 이번 라운드 수입을 더하기 "전" 보유 골드 기준으로 계산한다.
        /// </summary>
        public void GrantRoundEndGold(int aliveUnitCount)
        {
            int interest = GetInterest();
            int amount = goldPerRound + Mathf.Max(0, aliveUnitCount) * goldPerSurvivingUnit + interest;
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
