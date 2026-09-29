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
        //
        // 원래 참고했던 표는 Lv1/Lv2가 둘 다 "1코스트 100%"로 중복이었다. 그 표의 Lv2~Lv10을
        // 한 칸씩 당겨서 이 게임의 Lv1~Lv9로 쓰고(중복된 Lv1 행은 버림), 마지막 Lv10만 새로
        // 만들었다(참고 표엔 없던 구간이라 직접 지정: 1코3%/2코12%/3코20%/4코25%/5코40%).
        private static readonly int[][] Odds =
        {
            new[] { 100, 0, 0, 0, 0 },    // Lv1
            new[] { 75, 25, 0, 0, 0 },    // Lv2
            new[] { 55, 30, 15, 0, 0 },   // Lv3
            new[] { 45, 33, 20, 2, 0 },   // Lv4
            new[] { 30, 40, 25, 5, 0 },   // Lv5
            new[] { 16, 30, 43, 10, 1 },  // Lv6
            new[] { 15, 20, 32, 30, 3 },  // Lv7
            new[] { 10, 17, 25, 33, 15 }, // Lv8
            new[] { 5, 10, 20, 40, 25 },  // Lv9
            new[] { 3, 12, 20, 25, 40 },  // Lv10 (새로 지정)
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
