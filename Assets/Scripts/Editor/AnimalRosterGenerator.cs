#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 기획 문서(동물 로스터 xlsx)의 50계통 x 3성 = 150마리를 AnimalData 애셋으로 만들어주는 도구.
    /// Assets/Resources/Animals 폴더에 만들어지며, 상점(ShopManager)이 Resources.LoadAll로
    /// 이 폴더를 읽어서 판매 목록을 구성한다.
    ///
    /// 이미 같은 이름의 애셋이 있으면 새로 만들지 않고 그 애셋의 값만 갱신한다 (재실행해도 안전).
    /// 성장 계통(1성/2성/3성)은 previousEvolution/nextEvolution으로 서로 연결해준다.
    ///
    /// 메뉴: Tools > AnimalChess > 동물 로스터(50계통) 만들기
    /// </summary>
    public static class AnimalRosterGenerator
    {
        private const string OutputFolder = "Assets/Resources/Animals";

        private class StarRow
        {
            public string name;
            public int hpStar, atkStar, defStar, aspdStar;
            public float range;
        }

        private class Line
        {
            public Species species;
            public Habitat habitat;
            public int cost;
            public string designIntent;
            public StarRow star1, star2, star3;
        }

        [MenuItem("Tools/AnimalChess/동물 로스터(50계통) 만들기")]
        public static void GenerateRoster()
        {
            EnsureFolder(OutputFolder);

            var lines = BuildLines();
            int createdCount = 0;
            int updatedCount = 0;

            var expectedNames = new HashSet<string>();
            foreach (var line in lines)
            {
                expectedNames.Add(line.star1.name);
                expectedNames.Add(line.star2.name);
                expectedNames.Add(line.star3.name);
            }

            // 예전 기획서(더 적은 계통 수)로 만들어둔 동물 중, 이번 50계통 목록에 없는 것들은
            // 상점 풀을 오염시키지 않도록 지워준다 (예: 옛날 버전의 쥐/토끼 계통 등).
            int deletedCount = DeleteStaleAnimals(expectedNames);

            foreach (var line in lines)
            {
                var (a1created, a1) = CreateOrGetAnimal(line.star1.name);
                var (a2created, a2) = CreateOrGetAnimal(line.star2.name);
                var (a3created, a3) = CreateOrGetAnimal(line.star3.name);

                ApplyStats(a1, line, line.star1, 1, null, a2);
                ApplyStats(a2, line, line.star2, 2, a1, a3);
                ApplyStats(a3, line, line.star3, 3, a2, null);

                createdCount += (a1created ? 1 : 0) + (a2created ? 1 : 0) + (a3created ? 1 : 0);
                updatedCount += (!a1created ? 1 : 0) + (!a2created ? 1 : 0) + (!a3created ? 1 : 0);

                EditorUtility.SetDirty(a1);
                EditorUtility.SetDirty(a2);
                EditorUtility.SetDirty(a3);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "AnimalChess",
                $"동물 로스터 {lines.Count}계통 (총 {lines.Count * 3}마리) 처리 완료.\n\n" +
                $"- 새로 생성: {createdCount}마리\n" +
                $"- 이미 있어서 값만 갱신: {updatedCount}마리\n" +
                (deletedCount > 0 ? $"- 예전 로스터에서 빠진 동물 정리: {deletedCount}마리\n" : "") +
                $"\n저장 위치: {OutputFolder}\n" +
                "이제 'Tools > AnimalChess > 상점 UI 만들기'를 실행하면 이 동물들이 상점에 등장합니다.",
                "확인");
        }

        /// <summary>
        /// OutputFolder 안에 있는 AnimalData 중, 이번 BuildLines() 목록에 없는(=예전 로스터에서만 있던)
        /// 애셋을 삭제한다. 파일 이름(=동물 이름) 기준으로 비교한다.
        /// </summary>
        private static int DeleteStaleAnimals(HashSet<string> expectedNames)
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder)) return 0;

            int deleted = 0;
            var guids = AssetDatabase.FindAssets("t:AnimalData", new[] { OutputFolder });
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

        private static (bool created, AnimalData animal) CreateOrGetAnimal(string name)
        {
            string path = $"{OutputFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AnimalData>(path);
            if (existing != null) return (false, existing);

            var animal = ScriptableObject.CreateInstance<AnimalData>();
            AssetDatabase.CreateAsset(animal, path);
            return (true, animal);
        }

        private static void ApplyStats(AnimalData animal, Line line, StarRow row, int starLevel,
            AnimalData previous, AnimalData next)
        {
            animal.displayName = row.name;
            animal.species = line.species;
            animal.habitat = line.habitat;
            animal.cost = line.cost;
            animal.starLevel = starLevel;
            animal.previousEvolution = previous;
            animal.nextEvolution = next;

            var sb = new StringBuilder();
            sb.Append(Stars(row.hpStar)).Append(" HP / ");
            sb.Append(Stars(row.atkStar)).Append(" 공격 / ");
            sb.Append(Stars(row.defStar)).Append(" 방어 / ");
            sb.Append(Stars(row.aspdStar)).Append(" 공속 / 사거리 ").Append(row.range);
            sb.Append('\n').Append(line.designIntent);
            animal.description = sb.ToString();

            animal.baseStats = new UnitStats
            {
                hp = row.hpStar * 8f,
                attackPower = row.atkStar * 4f,
                defense = row.defStar * 2f,
                attackSpeed = row.aspdStar * 0.3f,
                attackRange = row.range,
            };
        }

        private static string Stars(int n)
        {
            return new string('★', Mathf.Max(0, n));
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

        private static List<Line> BuildLines()
        {
            var list = new List<Line>();

            // ===== 1코스트 =====
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Forest, cost = 1,
                designIntent = "밸런스형 근접",
                star1 = new StarRow { name = "강아지", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "웰시 코기", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "도사견", hpStar = 4, atkStar = 4, defStar = 3, aspdStar = 2, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Grassland, cost = 1,
                designIntent = "빠른 공속 딜러",
                star1 = new StarRow { name = "고양이", hpStar = 1, atkStar = 2, defStar = 1, aspdStar = 4, range = 1.0f },
                star2 = new StarRow { name = "아비시니안", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 5, range = 1.0f },
                star3 = new StarRow { name = "벵골고양이", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 5, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Grassland, cost = 1,
                designIntent = "저비용 원거리",
                star1 = new StarRow { name = "참새", hpStar = 1, atkStar = 1, defStar = 1, aspdStar = 4, range = 2.0f },
                star2 = new StarRow { name = "어치", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 4, range = 3.0f },
                star3 = new StarRow { name = "매", hpStar = 2, atkStar = 4, defStar = 1, aspdStar = 4, range = 4.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Sea, cost = 1,
                designIntent = "HP형 원거리",
                star1 = new StarRow { name = "오리", hpStar = 2, atkStar = 1, defStar = 1, aspdStar = 2, range = 2.0f },
                star2 = new StarRow { name = "기러기", hpStar = 3, atkStar = 2, defStar = 2, aspdStar = 2, range = 3.0f },
                star3 = new StarRow { name = "백조", hpStar = 4, atkStar = 3, defStar = 2, aspdStar = 2, range = 3.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Desert, cost = 1,
                designIntent = "히든 강캐 라인",
                star1 = new StarRow { name = "도마뱀", hpStar = 1, atkStar = 1, defStar = 2, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "왕도마뱀", hpStar = 2, atkStar = 2, defStar = 3, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "코모도왕도마뱀", hpStar = 4, atkStar = 4, defStar = 4, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Forest, cost = 1,
                designIntent = "저비용 밸런스",
                star1 = new StarRow { name = "카멜레온", hpStar = 1, atkStar = 1, defStar = 1, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "목도리도마뱀", hpStar = 2, atkStar = 2, defStar = 2, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "바실리스크도마뱀", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 3, range = 2.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Sea, cost = 1,
                designIntent = "물량형",
                star1 = new StarRow { name = "정어리", hpStar = 1, atkStar = 1, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "꽁치", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "삼치", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Tundra, cost = 1,
                designIntent = "극지 저비용",
                star1 = new StarRow { name = "빙어", hpStar = 1, atkStar = 1, defStar = 1, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "열빙어", hpStar = 2, atkStar = 1, defStar = 1, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "은어", hpStar = 2, atkStar = 2, defStar = 2, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Amphibian, habitat = Habitat.Swamp, cost = 1,
                designIntent = "성장폭 큰 반전 라인",
                star1 = new StarRow { name = "개구리", hpStar = 1, atkStar = 1, defStar = 1, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "황소개구리", hpStar = 3, atkStar = 2, defStar = 2, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "골리앗개구리", hpStar = 5, atkStar = 3, defStar = 3, aspdStar = 2, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Insect, habitat = Habitat.Forest, cost = 1,
                designIntent = "저비용 몸빵",
                star1 = new StarRow { name = "개미", hpStar = 2, atkStar = 1, defStar = 2, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "사슴벌레", hpStar = 3, atkStar = 2, defStar = 3, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "장수풍뎅이", hpStar = 4, atkStar = 3, defStar = 4, aspdStar = 2, range = 1.0f },
            });

            // ===== 2코스트 =====
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Forest, cost = 2,
                designIntent = "밸런스 공격형",
                star1 = new StarRow { name = "여우", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "붉은여우", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "은여우", hpStar = 4, atkStar = 4, defStar = 2, aspdStar = 4, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Desert, cost = 2,
                designIntent = "물량 몸빵형",
                star1 = new StarRow { name = "미어캣", hpStar = 2, atkStar = 1, defStar = 2, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "몽구스", hpStar = 3, atkStar = 2, defStar = 3, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "오소리", hpStar = 4, atkStar = 3, defStar = 4, aspdStar = 2, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Tundra, cost = 2,
                designIntent = "사거리+극지시너지",
                star1 = new StarRow { name = "부엉이", hpStar = 1, atkStar = 2, defStar = 1, aspdStar = 3, range = 3.0f },
                star2 = new StarRow { name = "수리부엉이", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 3, range = 3.0f },
                star3 = new StarRow { name = "흰올빼미", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 3, range = 4.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Tundra, cost = 2,
                designIntent = "종족+서식지 이중시너지 예시",
                star1 = new StarRow { name = "펭귄", hpStar = 2, atkStar = 1, defStar = 2, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "젠투펭귄", hpStar = 3, atkStar = 2, defStar = 3, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "황제펭귄", hpStar = 4, atkStar = 3, defStar = 4, aspdStar = 2, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Forest, cost = 2,
                designIntent = "밸런스 파충류",
                star1 = new StarRow { name = "아놀도마뱀", hpStar = 1, atkStar = 1, defStar = 2, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "이구아나", hpStar = 2, atkStar = 2, defStar = 3, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "가시이구아나", hpStar = 3, atkStar = 3, defStar = 4, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Swamp, cost = 2,
                designIntent = "저비용 독딜러",
                star1 = new StarRow { name = "살무사", hpStar = 1, atkStar = 2, defStar = 1, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "살모사", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 2, range = 2.0f },
                star3 = new StarRow { name = "방울뱀", hpStar = 2, atkStar = 4, defStar = 2, aspdStar = 2, range = 2.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Sea, cost = 2,
                designIntent = "공속형",
                star1 = new StarRow { name = "고등어", hpStar = 1, atkStar = 1, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "전갱이", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 4, range = 1.0f },
                star3 = new StarRow { name = "가다랑어", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 4, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Swamp, cost = 2,
                designIntent = "늪 기습형 딜러",
                star1 = new StarRow { name = "미꾸라지", hpStar = 1, atkStar = 2, defStar = 1, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "드렁허리", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "가물치", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Amphibian, habitat = Habitat.Swamp, cost = 2,
                designIntent = "재생형 밸런스",
                star1 = new StarRow { name = "도롱뇽", hpStar = 1, atkStar = 1, defStar = 2, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "불도롱뇽", hpStar = 2, atkStar = 2, defStar = 3, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "왕도롱뇽", hpStar = 4, atkStar = 2, defStar = 4, aspdStar = 2, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Insect, habitat = Habitat.Grassland, cost = 2,
                designIntent = "기습형 딜러",
                star1 = new StarRow { name = "사마귀", hpStar = 1, atkStar = 2, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "왕사마귀", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "유령사마귀", hpStar = 2, atkStar = 4, defStar = 2, aspdStar = 4, range = 1.0f },
            });

            // ===== 3코스트 =====
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Swamp, cost = 3,
                designIntent = "중간 사거리 딜러",
                star1 = new StarRow { name = "뱀", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 3, range = 2.0f },
                star2 = new StarRow { name = "비단뱀", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 3, range = 2.0f },
                star3 = new StarRow { name = "아나콘다", hpStar = 5, atkStar = 4, defStar = 2, aspdStar = 3, range = 2.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Swamp, cost = 3,
                designIntent = "탱커형",
                star1 = new StarRow { name = "메기", hpStar = 3, atkStar = 1, defStar = 2, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "큰메기", hpStar = 4, atkStar = 2, defStar = 3, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "자이언트메기", hpStar = 5, atkStar = 3, defStar = 4, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Forest, cost = 3,
                designIntent = "회피/스피드형",
                star1 = new StarRow { name = "다람쥐", hpStar = 1, atkStar = 1, defStar = 1, aspdStar = 4, range = 1.0f },
                star2 = new StarRow { name = "청설모", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 5, range = 1.0f },
                star3 = new StarRow { name = "하늘다람쥐", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 5, range = 2.0f },
            });
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Tundra, cost = 3,
                designIntent = "기습형 딜러",
                star1 = new StarRow { name = "북극여우", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 4, range = 1.0f },
                star2 = new StarRow { name = "흰족제비", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 4, range = 1.0f },
                star3 = new StarRow { name = "울버린", hpStar = 4, atkStar = 5, defStar = 2, aspdStar = 4, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Sea, cost = 3,
                designIntent = "최장거리형",
                star1 = new StarRow { name = "가마우지", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 3, range = 3.0f },
                star2 = new StarRow { name = "펠리컨", hpStar = 3, atkStar = 3, defStar = 1, aspdStar = 3, range = 4.0f },
                star3 = new StarRow { name = "알바트로스", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 3, range = 5.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Grassland, cost = 3,
                designIntent = "화려한 중간티어",
                star1 = new StarRow { name = "공작", hpStar = 1, atkStar = 2, defStar = 1, aspdStar = 3, range = 2.0f },
                star2 = new StarRow { name = "금계", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 3, range = 3.0f },
                star3 = new StarRow { name = "극락조", hpStar = 2, atkStar = 4, defStar = 1, aspdStar = 4, range = 3.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Swamp, cost = 3,
                designIntent = "중간티어 탱커",
                star1 = new StarRow { name = "자라", hpStar = 3, atkStar = 1, defStar = 3, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "왕자라", hpStar = 4, atkStar = 2, defStar = 4, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "악어거북", hpStar = 5, atkStar = 3, defStar = 5, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Sea, cost = 3,
                designIntent = "바다 밸런스",
                star1 = new StarRow { name = "숭어", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "농어", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "방어", hpStar = 4, atkStar = 4, defStar = 2, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Amphibian, habitat = Habitat.Swamp, cost = 3,
                designIntent = "독성 몸빵",
                star1 = new StarRow { name = "두꺼비", hpStar = 2, atkStar = 1, defStar = 2, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "물두꺼비", hpStar = 3, atkStar = 2, defStar = 3, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "왕두꺼비", hpStar = 4, atkStar = 2, defStar = 4, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Insect, habitat = Habitat.Forest, cost = 3,
                designIntent = "중갑형",
                star1 = new StarRow { name = "하늘소", hpStar = 2, atkStar = 2, defStar = 2, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "넓적사슴벌레", hpStar = 3, atkStar = 3, defStar = 3, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "아틀라스장수풍뎅이", hpStar = 4, atkStar = 3, defStar = 4, aspdStar = 2, range = 1.0f },
            });

            // ===== 4코스트 =====
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Grassland, cost = 4,
                designIntent = "대형 포식자 올라운더",
                star1 = new StarRow { name = "사자", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "아프리카사자", hpStar = 4, atkStar = 5, defStar = 3, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "화이트라이온", hpStar = 5, atkStar = 5, defStar = 3, aspdStar = 4, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Grassland, cost = 4,
                designIntent = "최강 탱커",
                star1 = new StarRow { name = "코끼리", hpStar = 4, atkStar = 3, defStar = 4, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "아프리카코끼리", hpStar = 5, atkStar = 4, defStar = 5, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "맘모스", hpStar = 5, atkStar = 5, defStar = 5, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Swamp, cost = 4,
                designIntent = "고HP고공격",
                star1 = new StarRow { name = "악어", hpStar = 3, atkStar = 3, defStar = 3, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "나일악어", hpStar = 4, atkStar = 4, defStar = 3, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "바다악어", hpStar = 5, atkStar = 5, defStar = 4, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Swamp, cost = 4,
                designIntent = "우아한 원거리 지원",
                star1 = new StarRow { name = "홍학", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 3, range = 3.0f },
                star2 = new StarRow { name = "왜가리", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 3, range = 3.0f },
                star3 = new StarRow { name = "두루미", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 3, range = 4.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Forest, cost = 4,
                designIntent = "숲 견제 원거리",
                star1 = new StarRow { name = "앵무새", hpStar = 2, atkStar = 2, defStar = 1, aspdStar = 4, range = 2.0f },
                star2 = new StarRow { name = "금강앵무", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 4, range = 3.0f },
                star3 = new StarRow { name = "코뿔새", hpStar = 3, atkStar = 4, defStar = 3, aspdStar = 4, range = 3.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Desert, cost = 4,
                designIntent = "사막 중독형 딜러",
                star1 = new StarRow { name = "사막뿔살모사", hpStar = 2, atkStar = 3, defStar = 2, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "사이드와인더", hpStar = 3, atkStar = 4, defStar = 3, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "길라몬스터", hpStar = 4, atkStar = 5, defStar = 3, aspdStar = 2, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Sea, cost = 4,
                designIntent = "바다 대형 탱커",
                star1 = new StarRow { name = "흰동가리", hpStar = 2, atkStar = 1, defStar = 2, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "쥐가오리", hpStar = 4, atkStar = 2, defStar = 3, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "만타가오리", hpStar = 5, atkStar = 2, defStar = 4, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Swamp, cost = 4,
                designIntent = "늪/바다 고체력",
                star1 = new StarRow { name = "잉어", hpStar = 3, atkStar = 2, defStar = 2, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "비단잉어", hpStar = 4, atkStar = 3, defStar = 3, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "철갑상어", hpStar = 5, atkStar = 3, defStar = 4, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Amphibian, habitat = Habitat.Tundra, cost = 4,
                designIntent = "극지 생존형",
                star1 = new StarRow { name = "청개구리", hpStar = 2, atkStar = 1, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "산개구리", hpStar = 3, atkStar = 2, defStar = 2, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "북방산개구리", hpStar = 4, atkStar = 2, defStar = 3, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Insect, habitat = Habitat.Forest, cost = 4,
                designIntent = "고공격 독딜러",
                star1 = new StarRow { name = "땅벌", hpStar = 1, atkStar = 3, defStar = 1, aspdStar = 4, range = 1.0f },
                star2 = new StarRow { name = "말벌", hpStar = 2, atkStar = 4, defStar = 1, aspdStar = 4, range = 1.0f },
                star3 = new StarRow { name = "장수말벌", hpStar = 2, atkStar = 5, defStar = 2, aspdStar = 4, range = 1.0f },
            });

            // ===== 5코스트 =====
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Tundra, cost = 5,
                designIntent = "최고 공속+사거리",
                star1 = new StarRow { name = "독수리", hpStar = 2, atkStar = 3, defStar = 1, aspdStar = 4, range = 3.0f },
                star2 = new StarRow { name = "흰머리독수리", hpStar = 2, atkStar = 4, defStar = 1, aspdStar = 5, range = 3.5f },
                star3 = new StarRow { name = "하르피아", hpStar = 3, atkStar = 5, defStar = 2, aspdStar = 5, range = 4.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Sea, cost = 5,
                designIntent = "최고 방어력",
                star1 = new StarRow { name = "거북이", hpStar = 3, atkStar = 1, defStar = 4, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "갈라파고스거북이", hpStar = 4, atkStar = 1, defStar = 5, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "아르켈론", hpStar = 5, atkStar = 2, defStar = 5, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Forest, cost = 5,
                designIntent = "강력한 근접 딜러",
                star1 = new StarRow { name = "호랑이", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "벵골호랑이", hpStar = 4, atkStar = 5, defStar = 3, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "백호", hpStar = 5, atkStar = 5, defStar = 4, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Mammal, habitat = Habitat.Tundra, cost = 5,
                designIntent = "최종 탱커급 딜러",
                star1 = new StarRow { name = "불곰", hpStar = 4, atkStar = 4, defStar = 3, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "그리즐리", hpStar = 5, atkStar = 4, defStar = 4, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "북극곰", hpStar = 5, atkStar = 5, defStar = 5, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Bird, habitat = Habitat.Forest, cost = 5,
                designIntent = "발차기 근접형 대형 조류",
                star1 = new StarRow { name = "에뮤", hpStar = 3, atkStar = 3, defStar = 2, aspdStar = 4, range = 1.0f },
                star2 = new StarRow { name = "타조", hpStar = 4, atkStar = 4, defStar = 3, aspdStar = 4, range = 1.0f },
                star3 = new StarRow { name = "화식조", hpStar = 4, atkStar = 5, defStar = 3, aspdStar = 5, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Reptile, habitat = Habitat.Desert, cost = 5,
                designIntent = "최상위 독딜러",
                star1 = new StarRow { name = "그린맘바", hpStar = 2, atkStar = 4, defStar = 1, aspdStar = 4, range = 1.0f },
                star2 = new StarRow { name = "블랙맘바", hpStar = 3, atkStar = 5, defStar = 2, aspdStar = 4, range = 1.0f },
                star3 = new StarRow { name = "킹코브라", hpStar = 3, atkStar = 5, defStar = 2, aspdStar = 5, range = 2.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Sea, cost = 5,
                designIntent = "최상위 딜러 상어",
                star1 = new StarRow { name = "흑기흉상어", hpStar = 3, atkStar = 4, defStar = 2, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "뱀상어", hpStar = 4, atkStar = 5, defStar = 2, aspdStar = 3, range = 1.0f },
                star3 = new StarRow { name = "백상아리", hpStar = 5, atkStar = 5, defStar = 3, aspdStar = 3, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Fish, habitat = Habitat.Sea, cost = 5,
                designIntent = "심해 이색 탱커",
                star1 = new StarRow { name = "아귀", hpStar = 3, atkStar = 2, defStar = 3, aspdStar = 1, range = 1.0f },
                star2 = new StarRow { name = "개복치", hpStar = 4, atkStar = 2, defStar = 4, aspdStar = 1, range = 1.0f },
                star3 = new StarRow { name = "실러캔스", hpStar = 5, atkStar = 3, defStar = 5, aspdStar = 1, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Amphibian, habitat = Habitat.Forest, cost = 5,
                designIntent = "작지만 치명적인 독딜러",
                star1 = new StarRow { name = "딸기독개구리", hpStar = 1, atkStar = 3, defStar = 1, aspdStar = 3, range = 1.0f },
                star2 = new StarRow { name = "코발트독개구리", hpStar = 1, atkStar = 4, defStar = 1, aspdStar = 4, range = 1.0f },
                star3 = new StarRow { name = "황금독개구리", hpStar = 2, atkStar = 5, defStar = 1, aspdStar = 4, range = 1.0f },
            });
            list.Add(new Line
            {
                species = Species.Insect, habitat = Habitat.Swamp, cost = 5,
                designIntent = "최상위 몸빵형 곤충",
                star1 = new StarRow { name = "비단벌레", hpStar = 2, atkStar = 2, defStar = 2, aspdStar = 2, range = 1.0f },
                star2 = new StarRow { name = "헤라클레스장수풍뎅이", hpStar = 4, atkStar = 3, defStar = 4, aspdStar = 2, range = 1.0f },
                star3 = new StarRow { name = "타이탄하늘소", hpStar = 5, atkStar = 4, defStar = 5, aspdStar = 2, range = 1.0f },
            });
            return list;
        }
    }
}
#endif
