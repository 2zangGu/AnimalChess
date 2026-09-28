using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 상점 5칸의 유닛 목록을 관리한다.
    /// Resources/Animals 폴더 아래의 모든 AnimalData를 코스트별로 모아두고,
    /// 플레이어 레벨에 따른 확률(LevelOddsTable)로 랜덤하게 뽑아 채운다.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public const int SlotCount = 5;
        public const int RefreshCost = 1;

        public AnimalData[] Slots { get; private set; } = new AnimalData[SlotCount];

        private readonly Dictionary<int, List<AnimalData>> _byCost = new Dictionary<int, List<AnimalData>>();

        private void Awake()
        {
            Instance = this;
            LoadRoster();
        }

        private void Start()
        {
            // PlayerEconomy.Instance가 이미 세팅되어 있다고 보장할 수 있는 시점(Start)에 첫 판매 목록을 뽑는다.
            RollAllSlots();
        }

        private void LoadRoster()
        {
            _byCost.Clear();
            for (int c = 1; c <= 5; c++) _byCost[c] = new List<AnimalData>();

            var all = Resources.LoadAll<AnimalData>("Animals");
            foreach (var animal in all)
            {
                if (animal == null) continue;
                // 상점에는 1성(진화 전 기본형) 유닛만 등장시킨다.
                if (animal.starLevel != 1) continue;
                int cost = Mathf.Clamp(animal.cost, 1, 5);
                _byCost[cost].Add(animal);
            }
        }

        public void RollAllSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                // 그 칸에 방금 있던 것과 똑같은 유닛이 다시 나오면 "새로고침이 안 된 것처럼" 보일 수
                // 있으니, 같은 코스트에 다른 후보가 있다면 몇 번 다시 뽑아서 최대한 피한다
                // (후보가 그 하나뿐이면 어쩔 수 없이 같은 게 다시 나올 수 있다).
                var previous = Slots[i];
                AnimalData picked = null;
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    picked = RollOne();
                    if (picked == null || previous == null || picked != previous) break;
                }
                Slots[i] = picked;
            }
        }

        public bool TryRefresh()
        {
            var economy = PlayerEconomy.Instance;
            if (economy == null) return false;
            if (!economy.TrySpendGold(RefreshCost)) return false;

            RollAllSlots();
            return true;
        }

        public bool TryBuy(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return false;
            var animal = Slots[slotIndex];
            if (animal == null) return false;

            var roster = PlayerRoster.Instance;
            if (roster == null || roster.IsBenchFull()) return false; // 벤치가 가득 차면 구매 불가

            var economy = PlayerEconomy.Instance;
            if (economy == null) return false;
            if (!economy.TrySpendGold(animal.cost)) return false;

            roster.TryAddToBench(animal);
            Slots[slotIndex] = null;
            return true;
        }

        private AnimalData RollOne()
        {
            int level = PlayerEconomy.Instance != null ? PlayerEconomy.Instance.Level : 1;
            var oddsRow = LevelOddsTable.GetOddsRow(level);

            // 실제로 뽑을 수 있는(풀에 유닛이 존재하는) 코스트만 후보로 삼는다.
            int totalWeight = 0;
            for (int cost = 1; cost <= 5; cost++)
            {
                if (_byCost[cost].Count == 0) continue;
                totalWeight += oddsRow[cost - 1];
            }

            if (totalWeight <= 0)
            {
                // 폴백: 가중치가 전부 0이면 유닛이 있는 아무 코스트에서나 뽑는다.
                for (int cost = 1; cost <= 5; cost++)
                {
                    if (_byCost[cost].Count > 0) return PickRandomFrom(_byCost[cost]);
                }
                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int acc = 0;
            for (int cost = 1; cost <= 5; cost++)
            {
                if (_byCost[cost].Count == 0) continue;
                acc += oddsRow[cost - 1];
                if (roll < acc)
                {
                    return PickRandomFrom(_byCost[cost]);
                }
            }

            return null;
        }

        private static AnimalData PickRandomFrom(List<AnimalData> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }
    }
}
