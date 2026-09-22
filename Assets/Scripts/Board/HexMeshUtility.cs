using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// 플레이스홀더용 평평한 pointy-top(위/아래가 뾰족한) 육각형 메쉬를 절차적으로 생성한다.
    /// 나중에 실제 타일 아트가 준비되면 이 메쉬 대신 프리팹으로 교체하면 된다.
    /// </summary>
    public static class HexMeshUtility
    {
        public static Mesh CreatePointyTopHex(float radius)
        {
            var vertices = new Vector3[7];
            var normals = new Vector3[7];
            var uvs = new Vector2[7];
            var triangles = new int[6 * 3];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            var corners = GetCorners(radius);
            for (int i = 0; i < 6; i++)
            {
                vertices[i + 1] = corners[i];
                float angleRad = Mathf.Deg2Rad * (60f * i + 30f);
                uvs[i + 1] = new Vector2(0.5f + 0.5f * Mathf.Cos(angleRad), 0.5f + 0.5f * Mathf.Sin(angleRad));
            }

            // 위(카메라가 내려다보는 +Y 방향)를 향하도록 삼각형 감김 순서(winding)를 맞춘다.
            for (int i = 0; i < 6; i++)
            {
                int current = i + 1;
                int next = (i + 1) % 6 + 1;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = next;
                triangles[i * 3 + 2] = current;
            }

            for (int i = 0; i < 7; i++)
            {
                normals[i] = Vector3.up;
            }

            var mesh = new Mesh { name = "HexTile" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 육각형의 6개 꼭짓점 로컬 좌표(y=0, XZ 평면)를 반환한다.
        /// 타일 테두리(LineRenderer) 등 메쉬 없이 꼭짓점만 필요한 곳에서 재사용한다.
        /// </summary>
        public static Vector3[] GetCorners(float radius)
        {
            var corners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angleRad = Mathf.Deg2Rad * (60f * i + 30f);
                corners[i] = new Vector3(radius * Mathf.Cos(angleRad), 0f, radius * Mathf.Sin(angleRad));
            }
            return corners;
        }
    }
}
