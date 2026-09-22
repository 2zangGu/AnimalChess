#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 유저가 넣어준 갈라진 흙바닥 이미지를, 보드 아래에 깔리는
    /// "게임 기본 배경(바닥)"으로 적용해주는 도구.
    ///
    /// 흰 배경은 미리 투명 처리(알파 마스크)해서 잘라둔 텍스처를 사용하고,
    /// 알파 클리핑(컷아웃) 머티리얼로 만들어서 원형 흙바닥만 보이게 한다.
    /// 서식지별 하늘/안개색은 BackgroundThemeManager가 계속 담당하고,
    /// 이 바닥은 그 아래 깔리는 "항상 보이는 기본 지형" 역할이다.
    ///
    /// 메뉴: Tools > AnimalChess > 기본 배경(바닥) 적용
    /// </summary>
    public static class GroundSetupTool
    {
        private const string TexturePath = "Assets/Textures/DefaultGround.png";
        private const string MaterialPath = "Assets/Materials/GroundBase.mat";
        private const string GroundObjectName = "GroundBase";

        [MenuItem("Tools/AnimalChess/기본 배경(바닥) 적용")]
        public static void ApplyGroundBackground()
        {
            var texture = LoadAndConfigureTexture();
            if (texture == null)
            {
                EditorUtility.DisplayDialog(
                    "AnimalChess",
                    $"텍스처를 찾지 못했습니다: {TexturePath}\n프로젝트에 이미지 파일이 먼저 들어와 있어야 합니다.",
                    "확인");
                return;
            }

            var material = CreateOrUpdateMaterial(texture);
            GameObject ground = CreateOrUpdateGroundObject(material, texture);

            EditorUtility.DisplayDialog(
                "AnimalChess",
                "기본 바닥(GroundBase)을 씬에 적용했습니다.\n\n" +
                "- 흰 배경은 투명 처리된 텍스처를 사용했습니다.\n" +
                "- 보드(BoardManager) 위치를 기준으로 중앙에 배치했습니다.\n" +
                "- 크기/위치는 Hierarchy에서 'GroundBase' 오브젝트를 선택해 Transform에서 자유롭게 조절할 수 있어요.\n" +
                "- 타일은 Play를 눌러야 보이지만, 이 바닥은 지금 Edit 모드에서 바로 보입니다.",
                "확인");

            Selection.activeGameObject = ground;
        }

        private static Texture2D LoadAndConfigureTexture()
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
                if (importer.mipmapEnabled != true) { importer.mipmapEnabled = true; changed = true; }
                if (changed)
                {
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static Material CreateOrUpdateMaterial(Texture2D texture)
        {
            EnsureFolder("Assets/Materials");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            mat.SetTexture("_BaseMap", texture);
            // 알파 클리핑(컷아웃): 원형 흙바닥 바깥(투명 영역)은 아예 렌더링하지 않는다.
            // 반투명 정렬 문제 없이 깔끔하게 원 모양만 남는다.
            mat.SetFloat("_Surface", 0f); // Opaque
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Smoothness", 0.1f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static GameObject CreateOrUpdateGroundObject(Material material, Texture2D texture)
        {
            GameObject ground = GameObject.Find(GroundObjectName);
            bool created = ground == null;

            if (created)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
                ground.name = GroundObjectName;
                Undo.RegisterCreatedObjectUndo(ground, "Create GroundBase");

                var col = ground.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            // Theme_Default(기본 배경)에서 Play 모드로 직접 눈으로 맞춰본 위치/각도/크기.
            // Edit 모드(Play 안 눌렀을 때)에도 똑같이 보이도록 이 값을 그대로 고정해서 쓴다.
            // (world-space 절대 좌표: BoardManager의 자식이 아니라 씬 루트에 독립적으로 둔다.
            //  타일판(BoardManager Transform/boardOffset)을 아무리 움직여도 배경은 절대 같이 움직이지 않도록 하기 위함.)
            Vector3 position = new Vector3(5.34f, -0.05f, -0.25f);
            float width = 128f;
            float depth = 168.4f;

            // 배경(GroundBase)은 항상 씬 루트에 독립적으로 둔다. BoardManager의 자식으로 두지 않는다 —
            // 그래야 타일판을 옮겨도(Board Offset, BoardManager Transform 위치 등) 배경이 절대 같이 따라 움직이지 않는다.
            ground.transform.SetParent(null, false);

            ground.transform.localPosition = position;
            ground.transform.localRotation = Quaternion.Euler(90.2f, 0f, 0f);
            ground.transform.localScale = new Vector3(width, depth, 1f);

            EditorUtility.SetDirty(ground);
            EditorSceneManager.MarkSceneDirty(ground.scene);
            return ground;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
