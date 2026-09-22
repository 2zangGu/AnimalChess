#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 인간 웨이브 적 유닛 26종을 EnemyUnitData 애셋으로 만들어주는 도구.
    ///
    /// 별도의 "보스 전용" 유닛이나 티어 경계에서의 급격한 전환 없이, 1라운드부터 26종이
    /// 점진적으로 강한 쪽으로 섞여 들어오고 약한 쪽은 자연스럽게 빠지도록 설계했다:
    /// - 26종을 (이미) 약한 순서대로 나열해두고, 그 순번(index)에 따라 등장 시작 라운드
    ///   (minRound)를 1~MaxGameRound 구간에 고르게 분산시킨다.
    /// - 각 유닛은 등장 시작 후 WindowLength 라운드 동안만 활성 상태로 유지된다(maxRound).
    ///   그래서 초반 잡졸은 중반 이후 자연스럽게 빠지고, 후반으로 갈수록 강한 유닛 위주로
    ///   구성이 바뀐다. 단, 마지막 몇 종은 창을 다 채우지 못하므로 끝까지(MaxGameRound) 남는다.
    /// - hp/공격력/방어력도 같은 순번을 기반으로 한 곡선(ScaledStats)으로 계산해서, 사람이
    ///   일일이 손으로 맞추지 않아도 자동으로 완만하게 우상향하도록 했다. 공속/사거리는
    ///   무기 종류(근접/원거리, 가볍고 빠른지 무겁고 느린지)를 반영하는 값이라 순번과 별개로
    ///   유닛마다 직접 지정한다.
    ///
    /// Assets/Resources/Enemies 폴더에 만들어지며, 나중에 웨이브 스폰 시스템이
    /// Resources.LoadAll로 이 폴더를 읽고 현재 라운드가 minRound~maxRound 안에 드는
    /// 유닛들 중에서 뽑아 쓰면 된다 (ShopManager가 Resources/Animals를 읽는 것과 같은 방식).
    ///
    /// 동물과 달리 성장/합성 시스템이 없어서 한 계통당 애셋 1개뿐이다.
    /// 이미 같은 이름의 애셋이 있으면 새로 만들지 않고 값만 갱신한다 (재실행해도 안전).
    ///
    /// 메뉴: Tools > AnimalChess > 적 유닛 로스터(26종) 만들기
    /// </summary>
    public static class EnemyRosterGenerator
    {
        private const string OutputFolder = "Assets/Resources/Enemies";

        /// <summary>RoundManager.maxRound 기본값(30)과 맞춘다. 프로젝트에서 최종 라운드 수를
        /// 바꾸면 이 값도 같이 맞춰주면 된다.</summary>
        private const int MaxGameRound = 30;

        /// <summary>한 유닛이 등장을 시작한 뒤 활성 상태로 유지되는 라운드 수.</summary>
        private const int WindowLength = 10;

        private class Line
        {
            public string name;
            public string designIntent;
            public EnemyTier tier;
            public float attackSpeed;
            public float attackRange;
        }

        [MenuItem("Tools/AnimalChess/적 유닛 로스터(26종) 만들기")]
        public static void GenerateRoster()
        {
            EnsureFolder(OutputFolder);

            var lines = BuildLines();
            int createdCount = 0;
            int updatedCount = 0;

            var expectedNames = new HashSet<string>();
            foreach (var line in lines) expectedNames.Add(line.name);

            int deletedCount = DeleteStaleEnemies(expectedNames);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var (created, enemy) = CreateOrGetEnemy(line.name);
                ApplyStats(enemy, line, i, lines.Count);
                EditorUtility.SetDirty(enemy);

                if (created) createdCount++;
                else updatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "AnimalChess",
                $"적 유닛 로스터 {lines.Count}종 처리 완료.\n\n" +
                $"- 새로 생성: {createdCount}종\n" +
                $"- 이미 있어서 값만 갱신: {updatedCount}종\n" +
                (deletedCount > 0 ? $"- 예전 로스터에서 빠진 유닛 정리: {deletedCount}종\n" : "") +
                $"\n1라운드부터 {MaxGameRound}라운드까지 점진적으로 등장/교체되도록 라운드 구간과 " +
                $"스탯을 자동으로 계산했습니다 (보스 전용 유닛 없음).\n" +
                $"저장 위치: {OutputFolder}\n" +
                "이제 'Tools > AnimalChess > 적 유닛 아이콘 임포트 및 연결'을 실행하면 아이콘이 붙습니다.",
                "확인");
        }

        private static int DeleteStaleEnemies(HashSet<string> expectedNames)
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder)) return 0;

            int deleted = 0;
            var guids = AssetDatabase.FindAssets("t:EnemyUnitData", new[] { OutputFolder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
                if (expectedNames.Contains(nameWithoutExt)) continue;

                AssetDatabase.DeleteAsset(path);
                deleted++;
            }
            return deleted;
        }

        private static (bool created, EnemyUnitData enemy) CreateOrGetEnemy(string name)
        {
            string path = $"{OutputFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyUnitData>(path);
            if (existing != null) return (false, existing);

            var enemy = ScriptableObject.CreateInstance<EnemyUnitData>();
            AssetDatabase.CreateAsset(enemy, path);
            return (true, enemy);
        }

        private static void ApplyStats(EnemyUnitData enemy, Line line, int index, int count)
        {
            int introRound = IntroRound(index, count);
            int exitRound = Mathf.Min(MaxGameRound, introRound + WindowLength - 1);

            enemy.displayName = line.name;
            enemy.description = line.designIntent;
            enemy.tier = line.tier;
            enemy.minRound = introRound;
            enemy.maxRound = exitRound;
            enemy.baseStats = ScaledStats(index, count, line.attackSpeed, line.attackRange);
        }

        /// <summary>
        /// index번째(0부터 시작) 유닛이 처음 등장하는 라운드. 0번은 1라운드, 마지막(count-1)번은
        /// MaxGameRound에 오도록 균등하게 분산시킨다.
        /// </summary>
        private static int IntroRound(int index, int count)
        {
            if (count <= 1) return 1;
            return 1 + Mathf.RoundToInt(index * (float)(MaxGameRound - 1) / (count - 1));
        }

        /// <summary>
        /// index/count로 정해지는 진행도(0~1)를 완만한 지수 곡선에 태워서 hp/공격력/방어력을
        /// 계산한다. 사람이 일일이 정하지 않아도 항상 앞쪽보다 뒤쪽이 세지도록 보장된다.
        /// 공속/사거리는 무기 컨셉을 반영하는 값이라 곡선에 태우지 않고 그대로 쓴다.
        /// </summary>
        private static UnitStats ScaledStats(int index, int count, float attackSpeed, float attackRange)
        {
            float t = count <= 1 ? 0f : index / (float)(count - 1);

            float hp = Mathf.Lerp(16f, 650f, Mathf.Pow(t, 1.3f));
            float atk = Mathf.Lerp(6f, 75f, Mathf.Pow(t, 1.2f));
            float def = Mathf.Lerp(1f, 24f, Mathf.Pow(t, 1.3f));

            return new UnitStats
            {
                hp = Mathf.Round(hp),
                attackPower = Mathf.Round(atk),
                defense = Mathf.Round(def),
                attackSpeed = attackSpeed,
                attackRange = attackRange,
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Line L(string name, string designIntent, EnemyTier tier, float aspd, float range)
        {
            return new Line { name = name, designIntent = designIntent, tier = tier, attackSpeed = aspd, attackRange = range };
        }

        /// <summary>
        /// 약한 순서대로 나열한 26종. 이 목록의 순번(index)이 곧 IntroRound/ScaledStats의
        /// 입력값이 되므로, 순서 자체가 난이도 곡선을 결정한다.
        /// </summary>
        private static List<Line> BuildLines()
        {
            var list = new List<Line>
            {
                // ----- Hunter : 원시적인 무기를 쓰는 초반 잡졸 -----
                L("주먹패", "맨주먹으로 덤비는 잡졸. 약하지만 숫자가 많다.", EnemyTier.Hunter, 1.1f, 1),
                L("도끼전사", "커다란 도끼를 휘두르는 야만전사. 근접 딜러형.", EnemyTier.Hunter, 0.8f, 1),
                L("검객", "짧은 검을 다루는 떠돌이 검객. 밸런스형 근접.", EnemyTier.Hunter, 1.2f, 1),
                L("불량배", "야구방망이를 든 거리의 불량배. 빠르고 가볍다.", EnemyTier.Hunter, 1.3f, 1),
                L("암살자", "후드를 쓴 은신형 암살자. 체력은 낮지만 공속이 빠르다.", EnemyTier.Hunter, 1.4f, 1),
                L("궁수", "활을 쏘는 기본 원거리 유닛.", EnemyTier.Hunter, 1.0f, 4),
                L("투석꾼", "큰 돌을 던지는 원시적인 밀렵꾼. 느리지만 한 방이 세다.", EnemyTier.Hunter, 0.7f, 2),
                L("석궁병", "석궁으로 무장한 정찰병. 사거리가 길다.", EnemyTier.Hunter, 0.9f, 3),
                L("부메랑전사", "부메랑을 던지는 부족 전사.", EnemyTier.Hunter, 1.1f, 3),
                L("투창병", "창을 던지는 사냥꾼. 근거리/원거리 중간 역할.", EnemyTier.Hunter, 1.0f, 2),

                // ----- Soldier : 총기/에너지 무기로 무장한 현대적 병력 -----
                L("돌격병", "소총으로 무장한 표준 돌격 보병.", EnemyTier.Soldier, 1.1f, 4),
                L("화염방사병", "화염방사기를 든 근중거리 소각 부대.", EnemyTier.Soldier, 0.8f, 2),
                L("사무라이", "에너지 검을 다루는 정예 근접 검사.", EnemyTier.Soldier, 1.0f, 1),
                L("특전사", "숙련된 소총 특수부대원. 균형 잡힌 원거리 딜러.", EnemyTier.Soldier, 1.2f, 4),
                L("닌자", "쌍권총을 쓰는 은신 요원. 체력은 낮지만 공속이 매우 빠르다.", EnemyTier.Soldier, 1.5f, 3),
                L("강철기사", "에너지 도끼를 든 중장갑 기사. 탱커형 근접.", EnemyTier.Soldier, 0.7f, 1),
                L("잠입자", "에너지 단검으로 급습하는 잠입 요원. 공속이 빠른 근접 딜러.", EnemyTier.Soldier, 1.3f, 1),
                L("돌파병", "전기톱과 방패를 든 돌파 부대. 방어력이 높다.", EnemyTier.Soldier, 0.9f, 1),

                // ----- Mechanized : 메크/전함/전투기 등 최상위 병기급 유닛 -----
                L("강습메크", "전기톱검과 방패를 장착한 강습형 메크.", EnemyTier.Mechanized, 0.9f, 1),
                L("포박메크", "사슬 클로로 대상을 포박하는 메크. 중거리 견제형.", EnemyTier.Mechanized, 0.8f, 3),
                L("비행메크", "쌍검과 추진기를 단 비행형 메크. 빠르고 공격적이다.", EnemyTier.Mechanized, 1.1f, 1),
                L("포격메크", "대형 함포를 장착한 포격형 메크. 느리지만 사거리와 화력이 압도적이다.", EnemyTier.Mechanized, 0.5f, 6),
                L("미사일메크", "양 어깨에 미사일 포드를 장착한 지원형 메크.", EnemyTier.Mechanized, 0.7f, 5),
                L("사령관메크", "쌍검을 든 인간형 전투 메크. 후반부 최상위 근접 병기.", EnemyTier.Mechanized, 0.9f, 1),
                L("사령전함", "다수의 포탑을 장착한 초대형 전함. 후반부 최상위 화력 지원.", EnemyTier.Mechanized, 0.5f, 6),
                L("전투기", "미사일을 탑재한 초음속 전투기. 26종 중 가장 강력한 최종 유닛.", EnemyTier.Mechanized, 1.0f, 7),
            };

            return list;
        }
    }
}
#endif
