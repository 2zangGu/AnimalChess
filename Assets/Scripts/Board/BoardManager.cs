using System.Collections.Generic;
using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// 육각형 보드를 절차적으로 생성하고 타일을 관리한다.
    /// 아래쪽 rows(플레이어 존)는 동물 배치 가능, 위쪽 rows(적 존)는
    /// 전투 시작 시 인간 웨이브가 스폰되는 자리로 구분한다.
    ///
    /// 사용법: 빈 GameObject를 하나 만들어 이 스크립트를 붙이면 Awake 시점에 보드가 생성된다.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [Header("보드 크기")]
        [Tooltip("한 줄(row)의 타일 개수")]
        public int columns = 7;
        [Tooltip("플레이어 존 세로 줄 수 (레벨10=10마리 배치를 감안해 여유있게 잡는다)")]
        public int playerRows = 4;
        [Tooltip("적 존 세로 줄 수")]
        public int enemyRows = 4;

        [Header("타일 비주얼")]
        public float hexSize = 1f;
        [Tooltip("타일 사이 시각적 여백 비율 (0~0.3 권장)")]
        [Range(0f, 0.3f)] public float tileGap = 0.05f;

        /// <summary>보드 중앙(플레이어 존/적 존 경계) 근처의 참조점. 카메라 타겟으로 사용.</summary>
        public Transform BoardCenter { get; private set; }

        private readonly Dictionary<HexCoord, HexTile> _tiles = new Dictionary<HexCoord, HexTile>();
        private Material _tileMaterial;

        private void Awake()
        {
            Instance = this;
            BuildBoard();
        }

        private void BuildBoard()
        {
            Mesh hexMesh = HexMeshUtility.CreatePointyTopHex(hexSize * (1f - tileGap));
            _tileMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _tileMaterial.SetFloat("_Smoothness", 0.15f); // 광택을 낮춰서 플라스틱처럼 반짝이지 않게

            int totalRows = playerRows + enemyRows;
            for (int r = 0; r < totalRows; r++)
            {
                bool isPlayerZone = r < playerRows;
                // 플레이어 존은 row를 음수로, 적 존은 0 이상으로 둬서
                // row=-1(플레이어 최전방)과 row=0(적 최전방) 사이가 보드의 경계선(프론트라인)이 되게 한다.
                int row = r - playerRows;
                for (int col = 0; col < columns; col++)
                {
                    // col(0..columns-1)을 그대로 axial로 쓰지 않고 오프셋 변환을 거쳐야
                    // 행이 바뀌어도 보드 왼쪽 끝이 밀리지 않고 직사각형 형태를 유지한다.
                    CreateTile(HexMetrics.OffsetToAxial(col, row), hexMesh, isPlayerZone);
                }
            }

            CreateBoardCenter();
        }

        private void CreateTile(HexCoord coord, Mesh mesh, bool isPlayerZone)
        {
            var go = new GameObject($"Tile_{coord.q}_{coord.r}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = HexMetrics.AxialToWorld(coord, hexSize);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _tileMaterial;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            var tile = go.AddComponent<HexTile>();
            tile.Initialize(coord, isPlayerZone);

            _tiles[coord] = tile;
        }

        private void CreateBoardCenter()
        {
            var centerGO = new GameObject("BoardCenter");
            centerGO.transform.SetParent(transform, false);

            float centerX = hexSize * Mathf.Sqrt(3f) * (columns - 1) * 0.5f;
            float centerZ = -hexSize * 0.75f; // 프론트라인(플레이어 최전방과 적 최전방 사이) 대략적인 위치
            centerGO.transform.localPosition = new Vector3(centerX, 0f, centerZ);

            BoardCenter = centerGO.transform;
        }

        public bool TryGetTile(HexCoord coord, out HexTile tile) => _tiles.TryGetValue(coord, out tile);

        public IEnumerable<HexTile> AllTiles => _tiles.Values;
    }
}
