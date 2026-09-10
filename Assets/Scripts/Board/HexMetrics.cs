using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// Pointy-top(위/아래가 뾰족한) 육각형 좌표 ↔ 월드 좌표 변환 유틸리티.
    ///
    /// 순수 axial 공식(x = q + r/2)을 그대로 쓰면 행이 내려갈수록 오프셋이 계속 누적되어
    /// 보드 전체가 평행사변형처럼 기울어져 보인다(지그재그로 보이는 원인).
    /// 그래서 axial 좌표를 "odd-r 오프셋" 좌표로 변환해서 렌더링한다:
    /// 홀수 행만 반 칸 밀어주고 짝수 행은 그대로 둬서, 행이 바뀌어도 보드 왼쪽 끝(col=0)이
    /// 항상 같은 두 x값(짝수행/홀수행) 사이를 오가게 만들어 보드가 직사각형 형태를 유지한다.
    /// HexCoord 자체는 이웃/거리 계산에 필요한 진짜 axial 좌표를 유지하고,
    /// 변환은 이 렌더링 함수 안에서만 처리한다.
    /// </summary>
    public static class HexMetrics
    {
        /// <summary>
        /// axial 좌표(q, r)를 월드 좌표(XZ 평면)로 변환한다.
        /// hexSize는 타일 중심에서 꼭짓점까지의 거리(반지름).
        /// </summary>
        public static Vector3 AxialToWorld(HexCoord coord, float hexSize)
        {
            int row = coord.r;
            int col = coord.q + (row - (row & 1)) / 2; // axial -> odd-r 오프셋 좌표

            float x = hexSize * Mathf.Sqrt(3f) * (col + 0.5f * (row & 1));
            float z = hexSize * 1.5f * row;
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// odd-r 오프셋 좌표(col, row)를 axial 좌표(HexCoord)로 변환한다.
        /// 보드를 생성할 때 "몇 번째 줄의 몇 번째 칸"으로 반복하면서 이 함수로
        /// 저장/조회용 axial HexCoord를 얻는 용도.
        /// </summary>
        public static HexCoord OffsetToAxial(int col, int row)
        {
            int q = col - (row - (row & 1)) / 2;
            return new HexCoord(q, row);
        }
    }
}
