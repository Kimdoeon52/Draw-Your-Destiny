namespace NYH.CoreCardSystem
{
    using NYH.BattleCardSystem;

    public class RewardCardBundleChoice
    {
        public CardData CivilizationCardData { get; }
        public BattleCardData BattleCardData { get; }

        public RewardCardBundleChoice(CardData civilizationCardData, BattleCardData battleCardData)
        {
            CivilizationCardData = civilizationCardData;
            BattleCardData = battleCardData;
        }
    }
}
