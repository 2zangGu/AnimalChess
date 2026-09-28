using UnityEngine;
using UnityEngine.EventSystems;

namespace AnimalChess.Game
{
    /// <summary>
    /// 시너지 패널(서식지/종족)의 줄(행) 하나에 붙어서, 마우스를 올리면 SynergyTooltipUI에
    /// 그 시너지의 동/은/금 3단계 효과를 전부 보여주고, 마우스를 떼면 숨긴다.
    /// title/tooltipBody는 TraitPanelUI/SpeciesTraitPanelUI가 마릿수가 바뀔 때마다
    /// TraitSynergy.GetHabitatTooltip/GetSpeciesTooltip으로 최신 내용으로 갱신해준다.
    /// 이 줄의 배경 Image(raycastTarget=true)가 있어야 마우스 이벤트를 받을 수 있다
    /// (TraitPanelSetupTool/SpeciesTraitPanelSetupTool이 만드는 줄에는 이미 있다).
    /// </summary>
    public class SynergyRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [HideInInspector] public string title;
        [HideInInspector] public string tooltipBody;

        public void OnPointerEnter(PointerEventData eventData)
        {
            SynergyTooltipUI.Instance?.Show(title, tooltipBody);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SynergyTooltipUI.Instance?.Hide();
        }
    }
}
