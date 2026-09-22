using UnityEngine;

namespace AnimalChess.Game
{
    /// <summary>
    /// 플레이어 레벨별로 상점에 뜨는 유닛의 코스트(1~5) 확률표.
    /// 레벨 10 이후는 10레벨과 같은 확률을 그대로 쓴다.
    /// </summary>
    public static class LevelOddsTable
    {
        // {코스트1, 코스트2, 코스트3, 코스트4, 코스트5} 확률(%)
        private static readonly int[][] Odds =
        {
            new[] { 100, 0, 0, 0, 0 },    // Lv1
            new[] { 100, 0, 0, 0, 0 },    // Lv2
            new[] { 75, 25, 0, 0, 0 },    // Lv3
            new[] { 55, 30, 15, 0, 0 },   // Lv4
            new[] { 45, 33, 20, 2, 0 },   // Lv5
            new[] { 30, 40, 25, 5, 0 },   // Lv6
            new[] { 16, 30, 43, 10, 1 },  // Lv7
            new[] { 15, 20, 32, 30, 3 },  // Lv8
            new[] { 10, 17, 25, 33, 15 }, // Lv9
            new[] { 5, 10, 20, 40, 25 },  // Lv10
        };

        public static int MaxTableLevel => Odds.Length;

        /// <summary>주어진 레벨에서 코스트(1~5) 하나의 등장 확률(%)을 반환한다.</summary>
        public static int GetOdds(int level, int cost)
        {
            int levelIndex = Mathf.Clamp(level, 1, MaxTableLevel) - 1;
            int costIndex = Mathf.Clamp(cost, 1, 5) - 1;
            return Odds[levelIndex][costIndex];
        }

        /// <summary>주어진 레벨의 코스트 1~5 확률을 한 번에 가져온다.</summary>
        public static int[] GetOddsRow(int level)
        {
            int levelIndex = Mathf.Clamp(level, 1, MaxTableLevel) - 1;
            return Odds[levelIndex];
        }
    }
}
