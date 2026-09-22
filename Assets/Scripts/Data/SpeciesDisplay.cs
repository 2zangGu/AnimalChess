namespace AnimalChess.Data
{
    /// <summary>
    /// Species(종족) enum 값을 화면에 보여줄 한글 이름으로 바꿔주는 헬퍼.
    /// HabitatDisplay와 짝을 이루며, 상점 칸 등에서 "포유류 · 숲"처럼 표시할 때 쓴다.
    /// </summary>
    public static class SpeciesDisplay
    {
        public static string GetKoreanName(Species species)
        {
            switch (species)
            {
                case Species.Mammal: return "포유류";
                case Species.Fish: return "어류";
                case Species.Reptile: return "파충류";
                case Species.Bird: return "조류";
                case Species.Amphibian: return "양서류";
                case Species.Insect: return "곤충";
                default: return species.ToString();
            }
        }
    }
}
