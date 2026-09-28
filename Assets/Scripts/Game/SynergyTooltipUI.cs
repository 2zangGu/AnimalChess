using UnityEngine;
using UnityEngine.UI;

namespace AnimalChess.Game
{
    /// <summary>
    /// 화면 왼쪽 시너지 패널(서식지/종족) 줄에 마우스를 올리면 뜨는 설명 말풍선 하나를
    /// 공유해서 쓴다. 어느 줄 위에 있든 같은 자리(패널들 바로 오른쪽)에 뜨고, 내용만 바뀐다.
    /// SynergyTooltipSetupTool이 씬에 미리 만들어두고, SynergyRowHover가 Show/Hide를 호출한다.
    /// </summary>
    public class SynergyTooltipUI : MonoBehaviour
    {
        public static SynergyTooltipUI Instance { get; private set; }

        [Tooltip("실제로 보이고 안 보이고를 여기서 켰다 껐다 한다(이 컴포넌트가 붙은 루트 오브젝트 " +
                 "자신이 아니라 자식). 루트는 항상 켜둬야 Awake가 Play 시작하자마자 실행돼서 " +
                 "Instance가 제때 잡힌다.")]
        public GameObject panel;
        public Text titleText;
        public Text bodyText;

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>title/body를 채우고 툴팁을 보여준다. body는 여러 줄(\n)로 구성될 수 있다.</summary>
        public void Show(string title, string body)
        {
            if (panel == null) return;
            panel.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
