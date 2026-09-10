using System.Collections.Generic;
using UnityEngine;
using AnimalChess.Board;

namespace AnimalChess.Environment
{
    /// <summary>
    /// 현재 배경 테마에 맞는 플레이스홀더 조형물을 보드 바깥 링(도넛) 모양 범위에 흩뿌려 배치한다.
    /// BackgroundThemeManager가 테마를 바꿀 때 SpawnForTheme()을 호출해서 사용한다.
    /// </summary>
    public class HabitatDecorationSpawner : MonoBehaviour
    {
        [Header("배치 범위 (보드 중심 기준 반지름)")]
        public float ringMinRadius = 8f;
        public float ringMaxRadius = 14f;

        private Transform _decorationParent;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        public void SpawnForTheme(HabitatBackgroundTheme theme)
        {
            ClearDecorations();
            if (theme == null || theme.propVariants == null) return;

            Vector3 center = (BoardManager.Instance != null && BoardManager.Instance.BoardCenter != null)
                ? BoardManager.Instance.BoardCenter.position
                : Vector3.zero;

            foreach (var variant in theme.propVariants)
            {
                for (int i = 0; i < variant.count; i++)
                {
                    float angle = Random.Range(0f, 360f);
                    float radius = Random.Range(ringMinRadius, ringMaxRadius);
                    Vector3 pos = center + Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * radius);
                    float scale = Random.Range(variant.minScale, variant.maxScale);

                    GameObject prop = ProceduralPropBuilder.Build(variant.shape, variant.primaryColor, variant.secondaryColor);
                    prop.transform.SetParent(GetParent(), true);
                    prop.transform.position = pos;
                    prop.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    prop.transform.localScale *= scale;

                    _spawned.Add(prop);
                }
            }
        }

        public void ClearDecorations()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }
            _spawned.Clear();
        }

        private Transform GetParent()
        {
            if (_decorationParent == null)
            {
                var go = new GameObject("Decorations");
                _decorationParent = go.transform;
            }
            return _decorationParent;
        }
    }
}
