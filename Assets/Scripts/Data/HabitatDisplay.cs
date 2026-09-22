namespace AnimalChess.Data
{
    /// <summary>
    /// Habitat(서식지) enum 값을 화면에 보여줄 한글 이름으로 바꿔주는 헬퍼.
    /// 시너지 패널(TraitPanelUI) 등 여러 곳에서 재사용한다.
    /// </summary>
    public static class HabitatDisplay
    {
        public static string GetKoreanName(Habitat habitat)
        {
            switch (habitat)
            {
                case Habitat.Forest: return "숲";
                case Habitat.Sea: return "바다";
                case Habitat.Swamp: return "늪";
                case Habitat.Desert: return "사막";
                case Habitat.Grassland: return "초원";
                case Habitat.Tundra: return "극지";
                default: return habitat.ToString();
            }
        }
    }
}
