using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Data;

namespace AnimalChess.Environment
{
    /// <summary>
    /// 서식지 하나에 대응하는 배경 테마(하늘/안개/주변광 색상).
    /// 라운드마다 플레이어 보드에서 가장 많은 서식지 시너지를 계산해서
    /// BackgroundThemeManager.SetDominantHabitat()로 전환하는 데 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHabitatTheme", menuName = "AnimalChess/Habitat Background Theme", order = 2)]
    public class HabitatBackgroundTheme : ScriptableObject
    {
        [Tooltip("기본(중립) 테마라면 아무 값이나 둬도 되고 BackgroundThemeManager의 defaultTheme 슬롯에만 연결하면 된다.")]
        public Habitat habitat;

        [Header("하늘 (Skybox/Procedural 기준)")]
        public Color skyTint = Color.white;
        public Color groundColor = Color.gray;

        [Header("안개 / 주변광")]
        public Color fogColor = Color.gray;
        public Color ambientColor = Color.gray;

        [Header("장식 (플레이스홀더 조형물, 여러 종류를 섞어서 배치)")]
        [Tooltip("아트 리소스 없이 프리미티브로 만드는 서식지 특유의 조형물들. 여러 개를 넣으면 섞여서 배치된다.")]
        public List<PropVariant> propVariants = new List<PropVariant>();

        [Header("바닥 텍스처 (선택)")]
        [Tooltip("비워두면 BackgroundThemeManager가 기본 바닥(GroundBase) 텍스처를 그대로 유지한다. " +
                 "넣으면 이 서식지가 활성화될 때 바닥 텍스처가 이걸로 바뀐다.")]
        public Texture2D groundTexture;

        [Header("바닥 위치 / 크기 / 각도 오버라이드 (선택)")]
        [Tooltip("체크하면 이 서식지일 때만 아래 위치/크기/각도를 쓴다. 체크 안 하면 " +
                 "BackgroundThemeManager의 기본값(Ground Width, GroundBase의 원래 위치/각도)을 그대로 쓴다. " +
                 "즉, 기본 배경은 안 건드리고 이 서식지 바닥만 따로 위치/크기/각도를 바꾸고 싶을 때 체크한다. " +
                 "Play 모드에서 GroundBase를 직접 드래그해서 원하는 모양을 찾은 뒤, 그 Transform 값을 " +
                 "그대로 아래 칸에 옮겨 적으면 된다.")]
        public bool overrideGroundTransform = false;
        [Tooltip("이 서식지일 때 바닥의 로컬 위치 (GroundBase의 부모인 BoardManager 기준).")]
        public Vector3 groundPositionOverride = Vector3.zero;
        [Tooltip("이 서식지일 때 바닥의 가로 폭(월드 유닛).")]
        public float groundWidthOverride = 30f;
        [Tooltip("이 서식지일 때 바닥의 세로 깊이(월드 유닛). 0이면 텍스처 비율에 맞춰 자동 계산한다. " +
                 "GroundBase를 직접 드래그해서 가로/세로를 따로 맞췄다면, 그 세로 값을 여기 그대로 넣으면 된다.")]
        public float groundDepthOverride = 0f;
        [Tooltip("이 서식지일 때 바닥의 회전 각도. 보통 X=90이 평평하게 위를 보는 각도다.")]
        public Vector3 groundRotationOverride = new Vector3(90f, 0f, 0f);
    }
}
