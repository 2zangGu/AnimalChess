using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 화면 왼쪽에, 지금 활성화된 서식지 시너지를 보여준다.
    ///
    /// 보드에 "배치된" 유닛들만 서식지(Habitat)별로 세어서, 2마리 이상 모인 서식지만
    /// 줄로 표시한다 (Habitat.cs에 정의된 2/4/6 브레이크포인트를 그대로 따른다):
    /// - 2~3마리: 동색 아이콘, "{마리수}/6"
    /// - 4~5마리: 은색 아이콘, "{마리수}/6"
    /// - 6마리 이상: 금색 아이콘, "{마리수}/6"
    ///
    /// 벤치에 대기 중인 유닛은 실제 전투에 참여하지 않으므로 시너지 계산에 넣지 않는다.
    /// </summary>
    public class TraitPanelUI : MonoBehaviour
    {
        public static readonly Habitat[] HabitatOrder =
        {
            Habitat.Forest, Habitat.Sea, Habitat.Swamp, Habitat.Desert, Habitat.Grassland, Habitat.Tundra
        };

        private const int BronzeThreshold = 2;
        private const int SilverThreshold = 4;
        private const int GoldThreshold = 6;

        private static readonly Color BronzeColor = new Color(0.72f, 0.45f, 0.2f);
        private static readonly Color SilverColor = new Color(0.75f, 0.78f, 0.8f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.15f);

        [Tooltip("HabitatOrder와 같은 순서(숲/바다/늪/사막/초원/극지)로 6개.")]
        public GameObject[] rows;
        public Image[] rowIcons;
        public Text[] rowLabels;

        private readonly int[] _lastCounts = new int[6];
        private bool _initialized;

        private void Update()
        {
            var roster = PlayerRoster.Instance;
            if (roster == null) return;

            var counts = new int[HabitatOrder.Length];
            foreach (var kvp in roster.BoardUnits)
            {
                var unit = kvp.Value;
                if (unit == null || !unit.isAlive || unit.currentData == null) continue;

                for (int h = 0; h < HabitatOrder.Length; h++)
                {
                    if (unit.currentData.habitat == HabitatOrder[h])
                    {
                        counts[h]++;
                        break;
                    }
                }
            }

            bool changed = !_initialized;
            for (int h = 0; h < counts.Length && !changed; h++)
            {
                if (counts[h] != _lastCounts[h]) changed = true;
            }
            if (!changed) return;
            _initialized = true;

            for (int h = 0; h < HabitatOrder.Length; h++)
            {
                _lastCounts[h] = counts[h];
                bool active = counts[h] >= BronzeThreshold;

                if (rows != null && h < rows.Length && rows[h] != null)
                {
                    rows[h].SetActive(active);
                }
                if (!active) continue;

                Color tierColor = counts[h] >= GoldThreshold ? GoldColor
                    : counts[h] >= SilverThreshold ? SilverColor
                    : BronzeColor;

                if (rowIcons != null && h < rowIcons.Length && rowIcons[h] != null)
                {
                    rowIcons[h].color = tierColor;
                }
                if (rowLabels != null && h < rowLabels.Length && rowLabels[h] != null)
                {
                    rowLabels[h].text = $"{HabitatDisplay.GetKoreanName(HabitatOrder[h])} {counts[h]}/{GoldThreshold}";
                }
            }
        }
    }
}
