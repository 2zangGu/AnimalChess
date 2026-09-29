using UnityEngine;
using UnityEngine.UI;

namespace AnimalChess.Game
{
    /// <summary>
    /// 유닛(벤치/보드 어느 쪽이든)을 드래그해서 여기 놓으면 파는 "판매 칸" UI 마커.
    /// 실제 판매/골드 지급 로직은 UnitDragController.EndDrag가
    /// PlayerRoster.TrySellFromBench/TrySellFromBoard를 호출해서 처리하고, 이 컴포넌트는
    /// 그 드롭 대상 사각형(RectTransform)의 위치를 표시하고, 드래그 중인 유닛을 이 칸 위로
    /// 올렸을 때 배경색을 바꿔서 "여기 놓으면 판다"는 걸 시각적으로 알려주는 역할만 한다.
    /// </summary>
    public class SellZoneUI : MonoBehaviour
    {
        public static SellZoneUI Instance { get; private set; }

        [Tooltip("드래그 중인 유닛을 이 칸 위로 올렸을 때 색이 바뀌는 배경 이미지.")]
        public Image background;

        [Tooltip("평소(드래그 중이 아니거나, 다른 곳에 드래그 중일 때) 배경색.")]
        public Color normalColor = new Color(0.5f, 0.12f, 0.12f, 0.85f);

        [Tooltip("드래그 중인 유닛을 이 칸 위에 올렸을 때(놓으면 판매됨) 배경색.")]
        public Color hoverColor = new Color(0.9f, 0.25f, 0.25f, 0.95f);

        private void Awake()
        {
            Instance = this;
            SetHover(false);
        }

        /// <summary>드래그 중인 유닛이 지금 이 칸 위에 있는지에 따라 배경색을 바꾼다.</summary>
        public void SetHover(bool isHovering)
        {
            if (background != null) background.color = isHovering ? hoverColor : normalColor;
        }
    }
}
