using System;
using UnityEngine;

namespace AnimalChess.Environment
{
    /// <summary>
    /// 배경 조형물 한 종류에 대한 스폰 설정 (모양 + 색상 + 개수 + 크기 범위).
    /// 서식지 테마 하나에 이걸 여러 개 넣으면 여러 종류의 조형물이 섞여서 배치된다.
    /// </summary>
    [Serializable]
    public struct PropVariant
    {
        public PropShape shape;
        public Color primaryColor;
        public Color secondaryColor;
        [Range(0, 30)] public int count;
        public float minScale;
        public float maxScale;
    }
}
