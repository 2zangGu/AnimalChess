using System;
using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// 육각형 보드의 축좌표(axial coordinate, q/r) 표현.
    /// Pointy-top(위/아래가 뾰족한) 육각형 기준. (이웃/거리 계산 자체는 방향과 무관하게 동일)
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int q;
        public int r;

        public HexCoord(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        // 6방향 이웃 오프셋 (axial 좌표계 기준, 육각형 방향과 무관하게 동일)
        private static readonly HexCoord[] Directions =
        {
            new HexCoord(+1, 0), new HexCoord(+1, -1), new HexCoord(0, -1),
            new HexCoord(-1, 0), new HexCoord(-1, +1), new HexCoord(0, +1)
        };

        public HexCoord GetNeighbor(int direction) => this + Directions[((direction % 6) + 6) % 6];

        public static int Distance(HexCoord a, HexCoord b)
        {
            int dq = a.q - b.q;
            int dr = a.r - b.r;
            return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
        }

        public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.q + b.q, a.r + b.r);
        public static bool operator ==(HexCoord a, HexCoord b) => a.q == b.q && a.r == b.r;
        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        public bool Equals(HexCoord other) => this == other;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(q, r);
        public override string ToString() => $"({q}, {r})";
    }
}
