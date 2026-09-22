using UnityEngine;
using UnityEngine.UI;

namespace AnimalChess.Game
{
    /// <summary>
    /// 벤치(보관 칸) 한 칸을 표시한다. slotIndex로 PlayerRoster.Bench[slotIndex]를 매 프레임
    /// 확인해서, 유닛이 들어오거나 나가거나 강등되는 등 값이 바뀌었을 때만 UI를 갱신한다.
    /// </summary>
    public class BenchSlotUI : MonoBehaviour
    {
        public int slotIndex;
        public Text nameText;
        public Text starText;
        public Image costPip;
        public Image iconImage;

        public Color[] costColors = new Color[5]
        {
            new Color(0.75f, 0.75f, 0.75f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.3f, 0.55f, 1f),
            new Color(0.7f, 0.35f, 0.9f),
            new Color(1f, 0.8f, 0.15f),
        };

        private UnitInstance _lastUnit;
        private int _lastStar = -1;
        private bool _lastAlive = true;
        private bool _initialized;

        private void Update()
        {
            var roster = PlayerRoster.Instance;
            if (roster == null) return;
            if (slotIndex < 0 || slotIndex >= roster.Bench.Length) return;

            var unit = roster.Bench[slotIndex];
            bool sameUnit = ReferenceEquals(unit, _lastUnit);
            bool starChanged = unit != null && unit.StarLevel != _lastStar;
            bool aliveChanged = unit != null && unit.isAlive != _lastAlive;

            if (_initialized && sameUnit && !starChanged && !aliveChanged) return;

            _initialized = true;
            _lastUnit = unit;
            _lastStar = unit != null ? unit.StarLevel : -1;
            _lastAlive = unit == null || unit.isAlive;

            Refresh(unit);
        }

        private void Refresh(UnitInstance unit)
        {
            bool empty = unit == null;

            if (nameText != null) nameText.text = empty ? "" : unit.DisplayName;
            if (starText != null) starText.text = empty ? "" : new string('★', Mathf.Max(0, unit.StarLevel));

            if (costPip != null)
            {
                costPip.enabled = !empty;
                if (!empty && unit.currentData != null)
                {
                    int idx = Mathf.Clamp(unit.currentData.cost - 1, 0, costColors.Length - 1);
                    costPip.color = costColors[idx];
                }
            }

            if (iconImage != null)
            {
                var icon = (!empty && unit.currentData != null) ? unit.currentData.icon : null;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
        }
    }
}
