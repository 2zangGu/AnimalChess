#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// Assets/Textures/Icons/Animals 폴더에 있는 동물 아이콘 PNG들을 스프라이트로 세팅하고,
    /// 같은 이름의 AnimalData 애셋(Assets/Resources/Animals/{이름}.asset)의 icon 필드에 연결한다.
    ///
    /// 대부분은 픽셀아트라서 필터를 Point(계단현상 없이 또렷하게)로 맞추지만, 원본 이미지가 큰
    /// 실사/일러스트 이미지(가로세로 160px 초과)로 직접 넣은 경우는 Point로 하면 오히려 계단현상이
    /// 심해 보이므로 자동으로 Bilinear를 대신 적용한다. 압축은 Uncompressed로 맞춘다.
    /// 이미 같은 설정이면 다시 임포트하지 않고, AnimalData 쪽 icon도 이미 같은 스프라이트면 건드리지 않는다
    /// (재실행 안전).
    ///
    /// 먼저 'Tools > AnimalChess > 동물 로스터(50계통) 만들기'로 AnimalData 애셋들을 만들어둬야
    /// 아이콘이 실제로 연결된다. 그 전에 이 도구를 실행하면 스프라이트 세팅만 되고, 연결은 0건으로 나온다.
    ///
    /// 메뉴: Tools > AnimalChess > 동물 아이콘 임포트 및 연결
    /// </summary>
    public static class AnimalIconImportTool
    {
        private const string IconFolder = "Assets/Textures/Icons/Animals";
        private const string AnimalDataFolder = "Assets/Resources/Animals";

        [MenuItem("Tools/AnimalChess/동물 아이콘 임포트 및 연결")]
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

                if (ConfigureAsPixelSprite(path))
                {
                    reimportedCount++;
                }
                else
                {
                    alreadyOkCount++;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                string dataPath = $"{AnimalDataFolder}/{name}.asset";
                var animalData = AssetDatabase.LoadAssetAtPath<AnimalData>(dataPath);
                if (animalData == null)
                {
                    unmatched.Add(name);
                    continue;
                }

                if (animalData.icon == sprite)
                {
                    alreadyLinkedCount++;
                    continue;
                }

                animalData.icon = sprite;
                EditorUtility.SetDirty(animalData);
                linkedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"동물 아이콘 임포트 결과 (총 {guids.Length}장)\n\n" +
                         $"- 새로 픽셀아트 설정(Point 필터/무압축)을 적용한 텍스처: {reimportedCount}장\n" +
                         $"- 이미 설정이 되어있던 텍스처: {alreadyOkCount}장\n" +
                         $"- 새로 AnimalData.icon에 연결됨: {linkedCount}마리\n" +
                         $"- 이미 연결되어 있던 것: {alreadyLinkedCount}마리\n" +
                         $"- 이름이 같은 AnimalData를 못 찾은 텍스처: {unmatched.Count}장\n";

            if (unmatched.Count > 0)
            {
                msg += "\n※ AnimalData를 못 찾은 이름 일부 (먼저 'Tools > AnimalChess > 동물 로스터(50계통) 만들기'를 실행했는지 확인해주세요):\n";
                int shown = Mathf.Min(unmatched.Count, 10);
                for (int i = 0; i < shown; i++) msg += $"  - {unmatched[i]}\n";
                if (unmatched.Count > shown) msg += $"  ... 외 {unmatched.Count - shown}장\n";
            }

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");
        }

        /// <summary>
        /// 반환값 true = 설정을 새로 바꿔서 재임포트했음, false = 이미 올바른 설정이라 손대지 않았음.
        /// </summary>
        private static bool ConfigureAsPixelSprite(string path)
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
            // 원본이 160px보다 크면 손으로 넣은 실사/일러스트 이미지로 보고 Bilinear를 적용해서
            // 부드럽게 보이도록 하고, 그 외(생성된 32x32→128x128 픽셀아트 등)는 Point를 유지한다.
            bool isLargeCustomImage = TryGetPngSize(path, out int srcW, out int srcH)
                                       && Mathf.Max(srcW, srcH) > 160;
            var desiredFilterMode = isLargeCustomImage ? FilterMode.Bilinear : FilterMode.Point;
            if (importer.filterMode != desiredFilterMode)
            {
                importer.filterMode = desiredFilterMode;
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

        /// <summary>
        /// PNG 파일의 IHDR 청크에서 원본 가로/세로 픽셀 크기를 직접 읽는다. 텍스처 임포터 API 버전
        /// 차이에 의존하지 않도록, 프로젝트 루트 기준 assetPath("Assets/...")의 실제 파일을 열어
        /// PNG 시그니처와 IHDR을 파싱한다. PNG가 아니거나 읽기 실패하면 false를 반환한다.
        /// </summary>
        private static bool TryGetPngSize(string assetPath, out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                string fullPath = Path.GetFullPath(assetPath);
                byte[] header = new byte[24];
                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                {
                    int readTotal = 0;
                    while (readTotal < header.Length)
                    {
                        int n = fs.Read(header, readTotal, header.Length - readTotal);
                        if (n <= 0) break;
                        readTotal += n;
                    }
                    if (readTotal < header.Length) return false;
                }

                // PNG 시그니처: 0x89 'P' 'N' 'G' \r \n 0x1A \n
                if (header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47)
                {
                    return false;
                }

                // IHDR은 시그니처(8바이트) + 길이(4) + "IHDR"(4) 다음에 width(4)/height(4)가 빅엔디안으로 온다.
                width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
