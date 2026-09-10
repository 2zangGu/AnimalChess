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
    }
}
