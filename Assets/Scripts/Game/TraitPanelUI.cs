using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 화면 왼쪽에, 지금 보드에 배치된 유닛들의 서식지 시너지를 보여준다.
    ///
    /// 보드에 "배치된" 유닛들만 서식지(Habitat)별로 세어서, 1마리 이상만 모여도 줄로 표시한다
    /// (Habitat.cs에 정의된 2/4/6 브레이크포인트 기준):
    /// - 1마리(다음 단계 미달): 회색 아이콘, "{마리수}/{다음 단계 마릿수}" (예: "1/2")
    /// - 2~3마리: 동색 아이콘, "{마리수}/4"
    /// - 4~5마리: 은색 아이콘, "{마리수}/6"
    /// - 6마리 이상: 금색 아이콘, "{마리수}/6" (최고 단계)
    ///
    /// 아직 활성화되지 않은(브레이크포인트 미달) 시너지도 회색으로 계속 보여줘서, 지금 몇 마리가
    /// 더 필요한지 한눈에 알 수 있게 한다. 벤치에 대기 중인 유닛은 실제 전투에 참여하지 않으므로
    /// 시너지 계산에 넣지 않는다.
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

        private static readonly Color InactiveColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color BronzeColor = new Color(0.72f, 0.45f, 0.2f);
        private static readonly Color SilverColor = new Color(0.75f, 0.78f, 0.8f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.15f);

        [Tooltip("HabitatOrder와 같은 순서(숲/바다/늪/사막/초원/극지)로 6개.")]
        public GameObject[] rows;
        public Image[] rowIcons;
        public Text[] rowLabels;
        [Tooltip("각 줄에 마우스를 올리면 동/은/금 3단계 효과를 전부 보여주는 툴팁 트리거. " +
                 "HabitatOrder와 같은 순서로 6개.")]
        public SynergyRowHover[] rowHovers;

        private readonly int[] _lastCounts = new int[6];
        private bool _initialized;

        private void Update()
        {
            var roster = PlayerRoster.Instance;
            if (roster == null) return;

            // 같은 성장 계통(예: 강아지 1성 + 웰시 코기 2성)은 몇 마리가 있든 하나로만 센다
            // (TraitSynergy.CountHabitats 참고 - CombatManager.BeginBattle의 실제 전투 적용과 기준이 같다).
            var habitatCounts = TraitSynergy.CountHabitats(roster.BoardUnits.Values);
            var counts = new int[HabitatOrder.Length];
            for (int h = 0; h < HabitatOrder.Length; h++)
            {
                habitatCounts.TryGetValue(HabitatOrder[h], out counts[h]);
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

                // 1마리라도 있으면 줄 자체는 보여준다(아직 브레이크포인트 미달이면 회색으로).
                bool hasAny = counts[h] >= 1;

                if (rows != null && h < rows.Length && rows[h] != null)
                {
                    rows[h].SetActive(hasAny);
                }
                if (!hasAny) continue;

                Color tierColor = counts[h] >= GoldThreshold ? GoldColor
                    : counts[h] >= SilverThreshold ? SilverColor
                    : counts[h] >= BronzeThreshold ? BronzeColor
                    : InactiveColor;

                // 분모는 "다음(또는 지금 달성한) 단계"의 필요 마릿수. 최고 단계에 도달하면 그대로 고정.
                int nextThreshold = counts[h] < BronzeThreshold ? BronzeThreshold
                    : counts[h] < SilverThreshold ? SilverThreshold
                    : GoldThreshold;

                if (rowIcons != null && h < rowIcons.Length && rowIcons[h] != null)
                {
                    rowIcons[h].color = tierColor;
                }
                if (rowLabels != null && h < rowLabels.Length && rowLabels[h] != null)
                {
                    rowLabels[h].text = $"{HabitatDisplay.GetKoreanName(HabitatOrder[h])} {counts[h]}/{nextThreshold}";
                    rowLabels[h].color = tierColor;
                }

                if (rowHovers != null && h < rowHovers.Length && rowHovers[h] != null)
                {
                    rowHovers[h].title = $"{HabitatDisplay.GetKoreanName(HabitatOrder[h])} 시너지";
                    rowHovers[h].tooltipBody = TraitSynergy.GetHabitatTooltip(HabitatOrder[h], counts[h]);
                }
            }
        }
    }
}
