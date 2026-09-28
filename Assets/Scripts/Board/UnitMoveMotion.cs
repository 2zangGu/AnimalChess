using UnityEngine;

namespace AnimalChess.Board
{
    /// <summary>
    /// 전투 중 유닛이 한 칸씩 이동할 때 쓰는 "통통 튀는" 절차적 이동 모션의 수치 계산을 모아둔 곳.
    /// BoardUnitView/EnemyUnitView가 구조가 같아서 이 계산 로직을 공유한다.
    ///
    /// 실제 3D 모델/스프라이트 애니메이션이 아직 없는 상태라서, 이동할 때 살짝 뛰어오르고
    /// 착지할 때 눌렸다가 스프링처럼 돌아오는 스케일 변화만으로도 훨씬 덜 뻣뻣해 보이게 하는 게 목적이다.
    /// </summary>
    internal static class UnitMoveMotion
    {
        /// <summary>착지 스쿼시가 가라앉는 데 걸리는 시간(초).</summary>
        public const float LandingSquashDuration = 0.12f;

        /// <summary>이동 도중(t: 0~1) 유닛이 떠오르는 높이(월드 단위). 포물선 모양(사인 곡선)으로 뛰었다가 착지한다.</summary>
        public static float EvaluateHopHeight(float t, float hopHeight = 0.18f)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Sin(t * Mathf.PI) * hopHeight;
        }

        /// <summary>
        /// 이동 도중(t: 0~1) 스프라이트의 (가로, 세로, 앞뒤) 스케일 배율.
        /// 뛰어오르는 정점(t=0.5)에서 세로로 살짝 늘어나고 가로/앞뒤는 그만큼 줄어든다(부피 보존 느낌).
        /// </summary>
        public static Vector3 EvaluateHopScale(float t)
        {
            t = Mathf.Clamp01(t);
            float stretch = Mathf.Sin(t * Mathf.PI) * 0.15f;
            float scaleY = 1f + stretch;
            float scaleXZ = 1f - stretch * 0.6f;
            return new Vector3(scaleXZ, scaleY, scaleXZ);
        }

        /// <summary>
        /// 착지 직후(u: 0~1, LandingSquashDuration 동안) 스프라이트의 (가로, 세로, 앞뒤) 스케일 배율.
        /// 착지 순간 살짝 눌렸다가(세로로 납작해짐) 다시 원래 크기로 튕겨 돌아온다.
        /// </summary>
        public static Vector3 EvaluateLandingScale(float u)
        {
            u = Mathf.Clamp01(u);
            float squash = Mathf.Sin(u * Mathf.PI) * 0.22f;
            float scaleY = 1f - squash;
            float scaleXZ = 1f + squash * 0.6f;
            return new Vector3(scaleXZ, scaleY, scaleXZ);
        }
    }
}
