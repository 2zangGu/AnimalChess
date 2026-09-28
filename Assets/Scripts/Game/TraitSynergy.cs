using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.Game
{
    /// <summary>
    /// 종족(Species)/서식지(Habitat) 시너지의 브레이크포인트(2/4/6)와 "실제 전투 효과" 수치를
    /// 한곳에 모아둔다. TraitPanelUI(서식지)/SpeciesTraitPanelUI(종족)는 여기 브레이크포인트를
    /// 그대로 따라 화면에 표시하고, CombatManager.BeginBattle()이 여기 수치를 실제 전투 스탯에
    /// 반영한다. 전부 테스트용 임시 수치라 나중에 얼마든지 조정 가능하다.
    ///
    /// 설계 방향: 종족 시너지는 "그 종족 유닛 자신"에게만 붙는 스탯 보너스, 서식지 시너지는
    /// 아군 전체/적 전체에 영향을 주는 "전투 방식" 효과로 차별화했다(Species.cs/Habitat.cs 주석 참고).
    /// </summary>
    public static class TraitSynergy
    {
        public const int BronzeThreshold = 2;
        public const int SilverThreshold = 4;
        public const int GoldThreshold = 6;

        /// <summary>count마리가 모였을 때 몇 단계인지 (0=비활성, 1=동, 2=은, 3=금).</summary>
        public static int GetTier(int count)
        {
            if (count >= GoldThreshold) return 3;
            if (count >= SilverThreshold) return 2;
            if (count >= BronzeThreshold) return 1;
            return 0;
        }

        private enum StatKind
        {
            Hp,
            AttackPower,
            Defense,
            AttackSpeed,
        }

        private struct SpeciesEffect
        {
            public StatKind stat;
            public float[] percentByTier; // [동, 은, 금] 순서로 3개.
        }

        // ---- 종족 시너지: 그 종족 유닛 본인에게만 곱해지는 스탯 퍼센트 보너스 ----
        private static readonly Dictionary<Species, SpeciesEffect> SpeciesEffects = new Dictionary<Species, SpeciesEffect>
        {
            { Species.Mammal, new SpeciesEffect { stat = StatKind.Hp, percentByTier = new[] { 0.10f, 0.20f, 0.35f } } },
            { Species.Fish, new SpeciesEffect { stat = StatKind.AttackSpeed, percentByTier = new[] { 0.10f, 0.20f, 0.35f } } },
            { Species.Reptile, new SpeciesEffect { stat = StatKind.Defense, percentByTier = new[] { 0.10f, 0.20f, 0.35f } } },
            { Species.Bird, new SpeciesEffect { stat = StatKind.AttackPower, percentByTier = new[] { 0.10f, 0.20f, 0.35f } } },
            { Species.Amphibian, new SpeciesEffect { stat = StatKind.Hp, percentByTier = new[] { 0.10f, 0.20f, 0.35f } } },
            { Species.Insect, new SpeciesEffect { stat = StatKind.AttackSpeed, percentByTier = new[] { 0.05f, 0.15f, 0.25f } } },
        };

        /// <summary>
        /// 종족 시너지 스탯 보너스를 적용한 새 UnitStats를 돌려준다(원본을 직접 고치지 않음).
        /// speciesCount가 브레이크포인트 미만이면 원래 스탯을 그대로 돌려준다.
        /// </summary>
        public static UnitStats ApplySpeciesBonus(UnitStats stats, Species species, int speciesCount)
        {
            int tier = GetTier(speciesCount);
            if (tier <= 0) return stats;
            if (!SpeciesEffects.TryGetValue(species, out var effect)) return stats;

            float percent = effect.percentByTier[tier - 1];
            switch (effect.stat)
            {
                case StatKind.Hp:
                    stats.hp *= 1f + percent;
                    break;
                case StatKind.AttackPower:
                    stats.attackPower *= 1f + percent;
                    break;
                case StatKind.Defense:
                    stats.defense *= 1f + percent;
                    break;
                case StatKind.AttackSpeed:
                    stats.attackSpeed *= 1f + percent;
                    break;
            }
            return stats;
        }

        // ---- 서식지 시너지 수치 (아군 전체/적 전체에 영향, CombatManager.BeginBattle이 직접 사용) ----

        /// <summary>숲: 아군 전체 최대 HP 증가율(동/은/금).</summary>
        public static readonly float[] ForestHpPercent = { 0.10f, 0.20f, 0.35f };

        /// <summary>바다: 아군 전체 공격 속도 증가율(동/은/금).</summary>
        public static readonly float[] SeaAttackSpeedPercent = { 0.10f, 0.20f, 0.35f };

        /// <summary>늪: 적 전체 공격 속도 감소율(동/은/금).</summary>
        public static readonly float[] SwampEnemySlowPercent = { 0.10f, 0.20f, 0.35f };

        /// <summary>사막: 아군 전체가 받는 피해 감소율(동/은/금).</summary>
        public static readonly float[] DesertDamageReductionPercent = { 0.10f, 0.20f, 0.30f };

        /// <summary>초원: 전투 시작 시 적 진영 안쪽(빈 칸)으로 기습 이동시킬 아군 수(동/은/금).</summary>
        public static readonly int[] GrasslandFlankCount = { 1, 2, 3 };

        /// <summary>극지: 전투 시작 시 적 전체의 첫 공격을 늦추는 시간(초, 동/은/금).</summary>
        public static readonly float[] TundraEnemyDelaySeconds = { 1.5f, 3f, 5f };

        /// <summary>
        /// 보드에 배치된 유닛들을 서식지별로 센다. 같은 성장 계통(예: 강아지 1성 + 웰시 코기 2성)은
        /// 몇 마리가 있든 하나로만 센다("동일 계열 중복 방지") - AnimalData.GetFamilyRoot 참고.
        /// 시너지 패널 표시(TraitPanelUI), 전투 스탯 적용(CombatManager.BeginBattle), 배경 테마
        /// 선택(ComputeDominantHabitat)이 전부 이 기준을 따른다.
        /// </summary>
        public static Dictionary<Habitat, int> CountHabitats(IEnumerable<UnitInstance> boardUnits)
        {
            var familiesPerHabitat = new Dictionary<Habitat, HashSet<AnimalData>>();
            foreach (var unit in boardUnits)
            {
                if (unit == null || !unit.isAlive || unit.currentData == null) continue;

                if (!familiesPerHabitat.TryGetValue(unit.currentData.habitat, out var families))
                {
                    families = new HashSet<AnimalData>();
                    familiesPerHabitat[unit.currentData.habitat] = families;
                }
                families.Add(unit.currentData.GetFamilyRoot());
            }

            var counts = new Dictionary<Habitat, int>();
            foreach (var kvp in familiesPerHabitat) counts[kvp.Key] = kvp.Value.Count;
            return counts;
        }

        /// <summary>종족판 CountHabitats. 같은 계열 유닛은 하나로만 센다.</summary>
        public static Dictionary<Species, int> CountSpecies(IEnumerable<UnitInstance> boardUnits)
        {
            var familiesPerSpecies = new Dictionary<Species, HashSet<AnimalData>>();
            foreach (var unit in boardUnits)
            {
                if (unit == null || !unit.isAlive || unit.currentData == null) continue;

                if (!familiesPerSpecies.TryGetValue(unit.currentData.species, out var families))
                {
                    families = new HashSet<AnimalData>();
                    familiesPerSpecies[unit.currentData.species] = families;
                }
                families.Add(unit.currentData.GetFamilyRoot());
            }

            var counts = new Dictionary<Species, int>();
            foreach (var kvp in familiesPerSpecies) counts[kvp.Key] = kvp.Value.Count;
            return counts;
        }

        /// <summary>
        /// 보드에 배치된 유닛들 중, 시너지가 실제로 활성화된(2마리 이상, 동 단계 이상) 서식지
        /// 가운데 가장 많은 것을 계산한다("이번 전투에서 가장 강하게 활성화된 서식지 시너지").
        /// 여러 서식지가 마릿수로 동점이면 그 중 하나를 무작위로 고른다. 활성화된(2마리 이상)
        /// 서식지가 하나도 없으면(다들 1마리 이하) null을 돌려준다 - 이때는 기본 배경으로 남는다.
        /// RoundManager.StartBattlePhase가 전투 라운드로 넘어갈 때 이 값으로 배경 테마를 바꾼다
        /// (BackgroundThemeManager.SetDominantHabitat 참고).
        /// </summary>
        public static Habitat? ComputeDominantHabitat(IEnumerable<UnitInstance> boardUnits)
        {
            var counts = CountHabitats(boardUnits);

            int maxCount = 0;
            var topHabitats = new List<Habitat>();
            foreach (var kvp in counts)
            {
                // 아직 시너지가 활성화되지 않은(브론즈 단계 미달) 서식지는 배경 후보에서 제외한다.
                if (kvp.Value < BronzeThreshold) continue;

                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    topHabitats.Clear();
                    topHabitats.Add(kvp.Key);
                }
                else if (kvp.Value == maxCount)
                {
                    topHabitats.Add(kvp.Key);
                }
            }

            if (topHabitats.Count == 0) return null;
            return topHabitats[Random.Range(0, topHabitats.Count)];
        }

        // ---- 시너지 패널 줄에 마우스를 올렸을 때 보여줄 툴팁 텍스트 ----

        private static string StatDisplayName(StatKind stat)
        {
            switch (stat)
            {
                case StatKind.Hp: return "체력";
                case StatKind.AttackPower: return "공격력";
                case StatKind.Defense: return "방어력";
                case StatKind.AttackSpeed: return "공격속도";
                default: return "";
            }
        }

        private static string Pct(float fraction) => $"{fraction * 100f:0.#}%";

        /// <summary>
        /// 지금 활성화된 단계 앞에는 ▶, 아니면 · 를 붙인 한 줄을 만든다. 동/은/금 대신
        /// "2극지: ...", "4극지: ..." 처럼 "그 단계 마릿수 + 시너지 이름" 형태로 표기한다.
        /// </summary>
        private static string TierLine(bool active, int threshold, string synergyName, string effectText)
        {
            return $"{(active ? "▶ " : "· ")}{threshold}{synergyName}: {effectText}";
        }

        /// <summary>
        /// 종족 시너지 툴팁: 2/4/6마리 3단계 효과를 전부 "2포유류: ...", "4포유류: ..." 형태로
        /// 보여주고, currentCount 기준으로 지금 활성화된 단계 앞에 ▶ 표시를 붙인다.
        /// </summary>
        public static string GetSpeciesTooltip(Species species, int currentCount)
        {
            if (!SpeciesEffects.TryGetValue(species, out var effect)) return "";

            string statName = StatDisplayName(effect.stat);
            string name = SpeciesDisplay.GetKoreanName(species);
            int tier = GetTier(currentCount);

            return TierLine(tier == 1, BronzeThreshold, name, $"{statName} +{Pct(effect.percentByTier[0])}") + "\n" +
                   TierLine(tier == 2, SilverThreshold, name, $"{statName} +{Pct(effect.percentByTier[1])}") + "\n" +
                   TierLine(tier == 3, GoldThreshold, name, $"{statName} +{Pct(effect.percentByTier[2])}");
        }

        /// <summary>
        /// 서식지 시너지 툴팁: 2/4/6마리 3단계 효과를 전부 "2극지: ...", "4극지: ..." 형태로
        /// 보여주고, currentCount 기준으로 지금 활성화된 단계 앞에 ▶ 표시를 붙인다.
        /// </summary>
        public static string GetHabitatTooltip(Habitat habitat, int currentCount)
        {
            int tier = GetTier(currentCount);
            string name = HabitatDisplay.GetKoreanName(habitat);
            switch (habitat)
            {
                case Habitat.Forest:
                    return TierLine(tier == 1, BronzeThreshold, name, $"아군 전체 최대 HP +{Pct(ForestHpPercent[0])}") + "\n" +
                           TierLine(tier == 2, SilverThreshold, name, $"아군 전체 최대 HP +{Pct(ForestHpPercent[1])}") + "\n" +
                           TierLine(tier == 3, GoldThreshold, name, $"아군 전체 최대 HP +{Pct(ForestHpPercent[2])}");
                case Habitat.Sea:
                    return TierLine(tier == 1, BronzeThreshold, name, $"아군 전체 공격속도 +{Pct(SeaAttackSpeedPercent[0])}") + "\n" +
                           TierLine(tier == 2, SilverThreshold, name, $"아군 전체 공격속도 +{Pct(SeaAttackSpeedPercent[1])}") + "\n" +
                           TierLine(tier == 3, GoldThreshold, name, $"아군 전체 공격속도 +{Pct(SeaAttackSpeedPercent[2])}");
                case Habitat.Swamp:
                    return TierLine(tier == 1, BronzeThreshold, name, $"적 전체 공격속도 -{Pct(SwampEnemySlowPercent[0])}") + "\n" +
                           TierLine(tier == 2, SilverThreshold, name, $"적 전체 공격속도 -{Pct(SwampEnemySlowPercent[1])}") + "\n" +
                           TierLine(tier == 3, GoldThreshold, name, $"적 전체 공격속도 -{Pct(SwampEnemySlowPercent[2])}");
                case Habitat.Desert:
                    return TierLine(tier == 1, BronzeThreshold, name, $"아군이 받는 피해 -{Pct(DesertDamageReductionPercent[0])}") + "\n" +
                           TierLine(tier == 2, SilverThreshold, name, $"아군이 받는 피해 -{Pct(DesertDamageReductionPercent[1])}") + "\n" +
                           TierLine(tier == 3, GoldThreshold, name, $"아군이 받는 피해 -{Pct(DesertDamageReductionPercent[2])}");
                case Habitat.Grassland:
                    return TierLine(tier == 1, BronzeThreshold, name, $"전투 시작 시 아군 {GrasslandFlankCount[0]}마리 기습 이동") + "\n" +
                           TierLine(tier == 2, SilverThreshold, name, $"전투 시작 시 아군 {GrasslandFlankCount[1]}마리 기습 이동") + "\n" +
                           TierLine(tier == 3, GoldThreshold, name, $"전투 시작 시 아군 {GrasslandFlankCount[2]}마리 기습 이동");
                case Habitat.Tundra:
                    return TierLine(tier == 1, BronzeThreshold, name, $"적 전체 첫 공격 {TundraEnemyDelaySeconds[0]:0.#}초 지연") + "\n" +
                           TierLine(tier == 2, SilverThreshold, name, $"적 전체 첫 공격 {TundraEnemyDelaySeconds[1]:0.#}초 지연") + "\n" +
                           TierLine(tier == 3, GoldThreshold, name, $"적 전체 첫 공격 {TundraEnemyDelaySeconds[2]:0.#}초 지연");
                default:
                    return "";
            }
        }
    }
}
