using UnityEngine;

namespace AnimalChess.Environment
{
    /// <summary>
    /// 서식지별 조형물 모양. 나중에 실제 아트가 준비되면 이 enum 대신 프리팹 참조로 바꿔도 된다.
    /// </summary>
    public enum PropShape
    {
        Tree,       // 나무 (숲)
        DeadTree,   // 기울어진 마른 나무 (늪)
        Rock,       // 작은 바위 무더기 (범용)
        Boulder,    // 크고 불규칙한 바위 하나
        Crystal,    // 뾰족한 얼음 결정 (극지)
        Cactus,     // 팔 달린 선인장 (사막)
        GrassTuft,  // 풀 뭉치 (초원)
        Bush,       // 둥근 덤불 (숲/초원)
        Mushroom,   // 버섯 무리 (늪)
        Log,        // 쓰러진 나무 둥치 (숲)
        Reed,       // 갈대/부들 (늪)
        SnowMound,  // 눈 쌓인 둔덕 (극지)
        Coral,      // 산호 형태 가지 (바다)
        SandDune    // 낮고 넓은 모래 둔덕 (사막)
    }

    /// <summary>
    /// 아트 리소스 없이 유니티 기본 프리미티브만으로 서식지별 플레이스홀더 조형물을 절차적으로 만든다.
    /// 나중에 실제 아트가 준비되면 Build() 대신 프리팹을 인스턴스화하는 코드로 교체하면 된다.
    /// </summary>
    public static class ProceduralPropBuilder
    {
        public static GameObject Build(PropShape shape, Color primary, Color secondary)
        {
            switch (shape)
            {
                case PropShape.Tree: return BuildTree(primary, secondary, twisted: false);
                case PropShape.DeadTree: return BuildTree(primary, secondary, twisted: true);
                case PropShape.Rock: return BuildRockCluster(primary, secondary, big: false);
                case PropShape.Boulder: return BuildRockCluster(primary, secondary, big: true);
                case PropShape.Crystal: return BuildCrystalCluster(primary, secondary);
                case PropShape.Cactus: return BuildCactus(primary, secondary);
                case PropShape.GrassTuft: return BuildGrassTuft(primary, secondary);
                case PropShape.Bush: return BuildBush(primary, secondary);
                case PropShape.Mushroom: return BuildMushroomCluster(primary, secondary);
                case PropShape.Log: return BuildLog(primary, secondary);
                case PropShape.Reed: return BuildReed(primary, secondary);
                case PropShape.SnowMound: return BuildMound(primary, secondary);
                case PropShape.Coral: return BuildCoral(primary, secondary);
                case PropShape.SandDune: return BuildDune(primary, secondary);
                default: return BuildRockCluster(primary, secondary, big: false);
            }
        }

        // 살짝 밝기를 흔들어서 같은 조형물이 여러 개 있어도 단조롭지 않게 만든다.
        private static Color Jitter(Color c)
        {
            float f = Random.Range(0.85f, 1.15f);
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);
        }

        private static Material _propMaterial;

        // 모든 조형물 파츠가 공유하는 무광 재질. 기본 프리미티브 재질은 플라스틱처럼 반짝여서
        // 광택(Smoothness)을 낮춰 자연물에 더 가깝게 보이도록 한다.
        private static Material PropMaterial
        {
            get
            {
                if (_propMaterial == null)
                {
                    _propMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    _propMaterial.SetFloat("_Smoothness", 0.12f);
                }
                return _propMaterial;
            }
        }

        private static GameObject AddPart(GameObject root, PrimitiveType type, Vector3 localPos, Vector3 localScale, Quaternion localRot, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            var col = part.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRot;

            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = PropMaterial;

            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", Jitter(color));
            renderer.SetPropertyBlock(mpb);

            return part;
        }

        private static GameObject BuildTree(Color trunkColor, Color canopyColor, bool twisted)
        {
            var root = new GameObject(twisted ? "DeadTree" : "Tree");
            float trunkHeight = twisted ? 1.4f : 1.6f;
            float tiltZ = twisted ? Random.Range(-20f, 20f) : Random.Range(-4f, 4f);

            AddPart(root, PrimitiveType.Cylinder,
                new Vector3(0f, trunkHeight * 0.5f, 0f),
                new Vector3(0.18f, trunkHeight * 0.5f, 0.18f),
                Quaternion.Euler(0f, 0f, tiltZ), trunkColor);

            // 밑동을 살짝 넓혀 뿌리 느낌
            AddPart(root, PrimitiveType.Sphere,
                new Vector3(0f, 0.08f, 0f),
                new Vector3(0.32f, 0.16f, 0.32f),
                Quaternion.identity, trunkColor);

            if (!twisted)
            {
                // 수관을 두 덩어리로 겹쳐서 좀 더 자연스러운 실루엣을 만든다
                AddPart(root, PrimitiveType.Sphere,
                    new Vector3(0f, trunkHeight + 0.45f, 0f),
                    new Vector3(1.1f, 0.9f, 1.1f),
                    Quaternion.identity, canopyColor);
                AddPart(root, PrimitiveType.Sphere,
                    new Vector3(Random.Range(-0.3f, 0.3f), trunkHeight + 0.75f, Random.Range(-0.3f, 0.3f)),
                    new Vector3(0.7f, 0.6f, 0.7f),
                    Quaternion.identity, canopyColor);
            }
            else
            {
                int branches = Random.Range(2, 4);
                for (int i = 0; i < branches; i++)
                {
                    float ang = Random.Range(0f, 360f);
                    float h = Random.Range(trunkHeight * 0.5f, trunkHeight * 0.95f);
                    AddPart(root, PrimitiveType.Cylinder,
                        new Vector3(0f, h, 0f),
                        new Vector3(0.05f, Random.Range(0.25f, 0.45f), 0.05f),
                        Quaternion.Euler(Random.Range(40f, 75f), ang, 0f), canopyColor);
                }
            }

            return root;
        }

        private static GameObject BuildRockCluster(Color primary, Color secondary, bool big)
        {
            var root = new GameObject(big ? "Boulder" : "RockCluster");
            int count = big ? Random.Range(2, 3) : Random.Range(3, 5);
            float baseSize = big ? Random.Range(0.9f, 1.4f) : Random.Range(0.25f, 0.6f);

            for (int i = 0; i < count; i++)
            {
                float s = baseSize * Random.Range(0.7f, 1f);
                Vector3 pos = new Vector3(Random.Range(-0.4f, 0.4f) * (big ? 0.6f : 1f), s * 0.35f, Random.Range(-0.4f, 0.4f) * (big ? 0.6f : 1f));
                Color c = Random.value > 0.5f ? primary : secondary;
                PrimitiveType type = Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
                AddPart(root, type, pos, new Vector3(s, s * Random.Range(0.6f, 0.85f), s),
                    Quaternion.Euler(Random.Range(0f, 25f), Random.Range(0f, 360f), Random.Range(0f, 25f)), c);
            }

            if (!big)
            {
                // 작은 자갈 한두 개로 디테일 추가
                for (int i = 0; i < Random.Range(0, 2); i++)
                {
                    float s = baseSize * 0.3f;
                    Vector3 pos = new Vector3(Random.Range(-0.5f, 0.5f), s * 0.5f, Random.Range(-0.5f, 0.5f));
                    AddPart(root, PrimitiveType.Sphere, pos, new Vector3(s, s, s), Quaternion.identity, secondary);
                }
            }

            return root;
        }

        private static GameObject BuildCrystalCluster(Color primary, Color secondary)
        {
            var root = new GameObject("CrystalCluster");
            int count = Random.Range(3, 5);
            for (int i = 0; i < count; i++)
            {
                float h = Random.Range(0.5f, 1.7f);
                float thickness = Random.Range(0.1f, 0.2f);
                Vector3 pos = new Vector3(Random.Range(-0.35f, 0.35f), h * 0.5f, Random.Range(-0.35f, 0.35f));
                Color c = Random.value > 0.5f ? primary : secondary;
                AddPart(root, PrimitiveType.Cube, pos, new Vector3(thickness, h * 0.5f, thickness),
                    Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f)), c);
            }
            return root;
        }

        private static GameObject BuildCactus(Color primary, Color secondary)
        {
            var root = new GameObject("Cactus");
            float h = Random.Range(1.0f, 1.6f);
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.22f, h * 0.5f, 0.22f), Quaternion.identity, primary);

            // 몸통의 세로 능선 디테일
            int ridges = 4;
            for (int i = 0; i < ridges; i++)
            {
                float ang = i * (360f / ridges);
                AddPart(root, PrimitiveType.Cylinder,
                    Quaternion.Euler(0f, ang, 0f) * new Vector3(0.2f, 0f, 0f) + new Vector3(0f, h * 0.5f, 0f),
                    new Vector3(0.04f, h * 0.5f, 0.04f),
                    Quaternion.Euler(0f, ang, 0f), secondary);
            }

            int arms = Random.Range(1, 3);
            for (int i = 0; i < arms; i++)
            {
                float armY = Random.Range(0.5f, h * 0.85f);
                float side = i % 2 == 0 ? 1f : -1f;
                AddPart(root, PrimitiveType.Cylinder,
                    new Vector3(side * 0.3f, armY, 0f),
                    new Vector3(0.12f, 0.35f, 0.12f),
                    Quaternion.Euler(0f, 0f, side * 60f), primary);
            }
            return root;
        }

        private static GameObject BuildGrassTuft(Color primary, Color secondary)
        {
            var root = new GameObject("GrassTuft");
            int blades = Random.Range(5, 9);
            for (int i = 0; i < blades; i++)
            {
                float h = Random.Range(0.3f, 0.65f);
                float ang = Random.Range(0f, 360f);
                float tilt = Random.Range(10f, 28f);
                Color c = Random.value > 0.5f ? primary : secondary;
                AddPart(root, PrimitiveType.Cylinder,
                    new Vector3(0f, h * 0.5f, 0f),
                    new Vector3(0.04f, h * 0.5f, 0.04f),
                    Quaternion.Euler(tilt, ang, 0f), c);
            }
            return root;
        }

        private static GameObject BuildBush(Color primary, Color secondary)
        {
            var root = new GameObject("Bush");
            int count = Random.Range(3, 5);
            for (int i = 0; i < count; i++)
            {
                float s = Random.Range(0.35f, 0.65f);
                Vector3 pos = new Vector3(Random.Range(-0.3f, 0.3f), s * 0.45f, Random.Range(-0.3f, 0.3f));
                Color c = Random.value > 0.5f ? primary : secondary;
                AddPart(root, PrimitiveType.Sphere, pos, new Vector3(s, s * 0.85f, s), Quaternion.identity, c);
            }
            return root;
        }

        private static GameObject BuildMushroomCluster(Color primary, Color secondary)
        {
            var root = new GameObject("MushroomCluster");
            int count = Random.Range(1, 4);
            for (int i = 0; i < count; i++)
            {
                float stemH = Random.Range(0.18f, 0.35f);
                float capR = Random.Range(0.18f, 0.3f);
                Vector3 basePos = new Vector3(Random.Range(-0.25f, 0.25f), 0f, Random.Range(-0.25f, 0.25f));

                AddPart(root, PrimitiveType.Cylinder, basePos + new Vector3(0f, stemH * 0.5f, 0f),
                    new Vector3(0.05f, stemH * 0.5f, 0.05f), Quaternion.identity, secondary);
                AddPart(root, PrimitiveType.Sphere, basePos + new Vector3(0f, stemH + capR * 0.25f, 0f),
                    new Vector3(capR, capR * 0.55f, capR), Quaternion.identity, primary);
            }
            return root;
        }

        private static GameObject BuildLog(Color primary, Color secondary)
        {
            var root = new GameObject("Log");
            float length = Random.Range(1.0f, 1.8f);
            float radius = 0.18f;
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, radius, 0f), new Vector3(radius, length * 0.5f, radius),
                Quaternion.Euler(0f, 0f, 90f), primary);

            int twigs = Random.Range(0, 2);
            for (int i = 0; i < twigs; i++)
            {
                float along = Random.Range(-length * 0.3f, length * 0.3f);
                AddPart(root, PrimitiveType.Cylinder, new Vector3(along, radius * 1.8f, 0f),
                    new Vector3(0.03f, 0.2f, 0.03f), Quaternion.Euler(Random.Range(30f, 60f), Random.Range(0f, 360f), 0f), secondary);
            }
            return root;
        }

        private static GameObject BuildReed(Color primary, Color secondary)
        {
            var root = new GameObject("Reed");
            int stalks = Random.Range(3, 6);
            for (int i = 0; i < stalks; i++)
            {
                float h = Random.Range(0.7f, 1.15f);
                float tilt = Random.Range(0f, 10f);
                float ang = Random.Range(0f, 360f);
                Vector3 basePos = new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
                Quaternion rot = Quaternion.Euler(tilt, ang, 0f);

                AddPart(root, PrimitiveType.Cylinder, basePos + new Vector3(0f, h * 0.5f, 0f),
                    new Vector3(0.025f, h * 0.5f, 0.025f), rot, secondary);
                AddPart(root, PrimitiveType.Cylinder, basePos + new Vector3(0f, h * 0.9f, 0f),
                    new Vector3(0.05f, 0.12f, 0.05f), rot, primary);
            }
            return root;
        }

        private static GameObject BuildMound(Color primary, Color secondary)
        {
            var root = new GameObject("SnowMound");
            float s = Random.Range(0.5f, 0.95f);
            AddPart(root, PrimitiveType.Sphere, new Vector3(0f, s * 0.22f, 0f), new Vector3(s, s * 0.45f, s), Quaternion.identity, primary);
            if (Random.value > 0.5f)
            {
                float s2 = s * 0.55f;
                AddPart(root, PrimitiveType.Sphere, new Vector3(s * 0.4f, s2 * 0.22f, s * 0.2f), new Vector3(s2, s2 * 0.4f, s2), Quaternion.identity, secondary);
            }
            return root;
        }

        private static GameObject BuildCoral(Color primary, Color secondary)
        {
            var root = new GameObject("Coral");
            float baseH = Random.Range(0.45f, 0.85f);
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, baseH * 0.5f, 0f), new Vector3(0.12f, baseH * 0.5f, 0.12f), Quaternion.identity, primary);

            int branches = Random.Range(3, 5);
            for (int i = 0; i < branches; i++)
            {
                float ang = i * (360f / branches) + Random.Range(-15f, 15f);
                float tilt = Random.Range(35f, 65f);
                float branchH = Random.Range(0.3f, 0.65f);
                Color c = Random.value > 0.5f ? primary : secondary;
                AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, baseH * 0.8f, 0f),
                    new Vector3(0.06f, branchH * 0.5f, 0.06f), Quaternion.Euler(tilt, ang, 0f), c);
            }
            return root;
        }

        private static GameObject BuildDune(Color primary, Color secondary)
        {
            var root = new GameObject("SandDune");
            float s = Random.Range(0.8f, 1.6f);
            AddPart(root, PrimitiveType.Sphere, new Vector3(0f, s * 0.13f, 0f), new Vector3(s, s * 0.28f, s * 0.8f),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), primary);
            if (Random.value > 0.5f)
            {
                float s2 = s * 0.5f;
                AddPart(root, PrimitiveType.Sphere, new Vector3(s * 0.5f, s2 * 0.13f, 0f), new Vector3(s2, s2 * 0.25f, s2 * 0.7f),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), secondary);
            }
            return root;
        }
    }
}
