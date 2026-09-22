#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// Assets/Textures/Icons/Enemies 폴더에 있는 적 유닛 아이콘 PNG들을 스프라이트로 세팅하고,
    /// 같은 이름의 EnemyUnitData 애셋(Assets/Resources/Enemies/{이름}.asset)의 icon 필드에 연결한다.
    /// AnimalIconImportTool과 완전히 같은 방식(같은 이름 매칭, 큰 이미지는 Bilinear)으로 동작한다.
    ///
    /// 먼저 'Tools > AnimalChess > 적 유닛 로스터(26종) 만들기'로 EnemyUnitData 애셋들을
    /// 만들어둬야 아이콘이 실제로 연결된다. 그 전에 이 도구를 실행하면 스프라이트 세팅만 되고,
    /// 연결은 0건으로 나온다.
    ///
    /// 메뉴: Tools > AnimalChess > 적 유닛 아이콘 임포트 및 연결
    /// </summary>
    public static class EnemyIconImportTool
    {
        private const string IconFolder = "Assets/Textures/Icons/Enemies";
        private const string EnemyDataFolder = "Assets/Resources/Enemies";

        [MenuItem("Tools/AnimalChess/적 유닛 아이콘 임포트 및 연결")]
        public static void ImportAndLink()
        {
            if (!AssetDatabase.IsValidFolder(IconFolder))
            {
                EditorUtility.DisplayDialog("AnimalChess",
                    $"'{IconFolder}' 폴더가 없습니다.\n아이콘 PNG 파일들을 이 폴더에 먼저 넣어주세요.",
                    "확인");
                return;
            }

            AssetDatabase.Refresh();

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder });
            int reimportedCount = 0;
            int alreadyOkCount = 0;
            int linkedCount = 0;
            int alreadyLinkedCount = 0;
            var unmatched = new List<string>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);

                if (ConfigureAsSprite(path))
                {
                    reimportedCount++;
                }
                else
                {
                    alreadyOkCount++;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                string dataPath = $"{EnemyDataFolder}/{name}.asset";
                var enemyData = AssetDatabase.LoadAssetAtPath<EnemyUnitData>(dataPath);
                if (enemyData == null)
                {
                    unmatched.Add(name);
                    continue;
                }

                if (enemyData.icon == sprite)
                {
                    alreadyLinkedCount++;
                    continue;
                }

                enemyData.icon = sprite;
                EditorUtility.SetDirty(enemyData);
                linkedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"적 유닛 아이콘 임포트 결과 (총 {guids.Length}장)\n\n" +
                         $"- 새로 스프라이트 설정을 적용한 텍스처: {reimportedCount}장\n" +
                         $"- 이미 설정이 되어있던 텍스처: {alreadyOkCount}장\n" +
                         $"- 새로 EnemyUnitData.icon에 연결됨: {linkedCount}종\n" +
                         $"- 이미 연결되어 있던 것: {alreadyLinkedCount}종\n" +
                         $"- 이름이 같은 EnemyUnitData를 못 찾은 텍스처: {unmatched.Count}장\n";

            if (unmatched.Count > 0)
            {
                msg += "\n※ EnemyUnitData를 못 찾은 이름 일부 (먼저 'Tools > AnimalChess > 적 유닛 로스터(26종) 만들기'를 실행했는지 확인해주세요):\n";
                int shown = Mathf.Min(unmatched.Count, 10);
                for (int i = 0; i < shown; i++) msg += $"  - {unmatched[i]}\n";
                if (unmatched.Count > shown) msg += $"  ... 외 {unmatched.Count - shown}장\n";
            }

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");
        }

        /// <summary>
        /// 반환값 true = 설정을 새로 바꿔서 재임포트했음, false = 이미 올바른 설정이라 손대지 않았음.
        /// 이 아이콘들은 전부 400x400 실사풍 렌더 이미지라서 항상 Bilinear로 맞춘다
        /// (동물 아이콘처럼 32x32 픽셀아트로 넣는 경우가 없으므로 Point 분기는 두지 않는다).
        /// </summary>
        private static bool ConfigureAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }

            return changed;
        }
    }
}
#endif
