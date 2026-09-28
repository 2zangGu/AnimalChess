using UnityEngine;
using UnityEngine.UI;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 화면 왼쪽에, 지금 보드에 배치된 유닛들의 종족(Species) 시너지를 보여준다.
    /// 서식지 시너지를 보여주는 TraitPanelUI와 완전히 같은 구조/표시 방식이고, 세는 기준만
    /// 서식지 대신 종족으로 바꾼 것이다.
    ///
    /// 보드에 "배치된" 유닛들만 종족별로 세어서, 1마리 이상만 모여도 줄로 표시한다
    /// (TraitSynergy에 정의된 2/4/6 브레이크포인트 기준):
    /// - 1마리(다음 단계 미달): 회색 아이콘, "{마리수}/{다음 단계 마릿수}" (예: "1/2")
    /// - 2~3마리: 동색 아이콘, "{마리수}/4"
    /// - 4~5마리: 은색 아이콘, "{마리수}/6"
    /// - 6마리 이상: 금색 아이콘, "{마리수}/6" (최고 단계)
    ///
    /// 실제 전투 보너스는 TraitSynergy.ApplySpeciesBonus를 CombatManager.BeginBattle이 적용한다.
    /// 벤치에 대기 중인 유닛은 실제 전투에 참여하지 않으므로 시너지 계산에 넣지 않는다.
    /// </summary>
    public class SpeciesTraitPanelUI : MonoBehaviour
    {
        public static readonly Species[] SpeciesOrder =
        {
            Species.Mammal, Species.Fish, Species.Reptile, Species.Bird, Species.Amphibian, Species.Insect
        };

        private static readonly Color InactiveColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color BronzeColor = new Color(0.72f, 0.45f, 0.2f);
        private static readonly Color SilverColor = new Color(0.75f, 0.78f, 0.8f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.15f);

        [Tooltip("SpeciesOrder와 같은 순서(포유류/어류/파충류/조류/양서류/곤충)로 6개.")]
        public GameObject[] rows;
        public Image[] rowIcons;
        public Text[] rowLabels;
        [Tooltip("각 줄에 마우스를 올리면 동/은/금 3단계 효과를 전부 보여주는 툴팁 트리거. " +
                 "SpeciesOrder와 같은 순서로 6개.")]
        public SynergyRowHover[] rowHovers;

        private readonly int[] _lastCounts = new int[6];
        private bool _initialized;

        private void Update()
        {
            var roster = PlayerRoster.Instance;
            if (roster == null) return;

            // 같은 성장 계통(예: 강아지 1성 + 웰시 코기 2성)은 몇 마리가 있든 하나로만 센다
            // (TraitSynergy.CountSpecies 참고 - CombatManager.BeginBattle의 실제 전투 적용과 기준이 같다).
            var speciesCounts = TraitSynergy.CountSpecies(roster.BoardUnits.Values);
            var counts = new int[SpeciesOrder.Length];
            for (int s = 0; s < SpeciesOrder.Length; s++)
            {
                speciesCounts.TryGetValue(SpeciesOrder[s], out counts[s]);
            }

            bool changed = !_initialized;
            for (int s = 0; s < counts.Length && !changed; s++)
            {
                if (counts[s] != _lastCounts[s]) changed = true;
            }
            if (!changed) return;
            _initialized = true;

            for (int s = 0; s < SpeciesOrder.Length; s++)
            {
                _lastCounts[s] = counts[s];

                // 1마리라도 있으면 줄 자체는 보여준다(아직 브레이크포인트 미달이면 회색으로).
                bool hasAny = counts[s] >= 1;

                if (rows != null && s < rows.Length && rows[s] != null)
                {
                    rows[s].SetActive(hasAny);
                }
                if (!hasAny) continue;

                Color tierColor = counts[s] >= TraitSynergy.GoldThreshold ? GoldColor
                    : counts[s] >= TraitSynergy.SilverThreshold ? SilverColor
                    : counts[s] >= TraitSynergy.BronzeThreshold ? BronzeColor
                    : InactiveColor;

                // 분모는 "다음(또는 지금 달성한) 단계"의 필요 마릿수. 최고 단계에 도달하면 그대로 고정.
                int nextThreshold = counts[s] < TraitSynergy.BronzeThreshold ? TraitSynergy.BronzeThreshold
                    : counts[s] < TraitSynergy.SilverThreshold ? TraitSynergy.SilverThreshold
                    : TraitSynergy.GoldThreshold;

                if (rowIcons != null && s < rowIcons.Length && rowIcons[s] != null)
                {
                    rowIcons[s].color = tierColor;
                }
                if (rowLabels != null && s < rowLabels.Length && rowLabels[s] != null)
                {
                    rowLabels[s].text = $"{SpeciesDisplay.GetKoreanName(SpeciesOrder[s])} {counts[s]}/{nextThreshold}";
                    rowLabels[s].color = tierColor;
                }

                if (rowHovers != null && s < rowHovers.Length && rowHovers[s] != null)
                {
                    rowHovers[s].title = $"{SpeciesDisplay.GetKoreanName(SpeciesOrder[s])} 시너지";
                    rowHovers[s].tooltipBody = TraitSynergy.GetSpeciesTooltip(SpeciesOrder[s], counts[s]);
                }
            }
        }
    }
}
