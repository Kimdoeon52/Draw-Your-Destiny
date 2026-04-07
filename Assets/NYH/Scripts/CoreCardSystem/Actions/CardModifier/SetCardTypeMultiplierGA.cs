namespace NYH.CoreCardSystem
{
    public class SetCardTypeMultiplierGA : GameAction
    {
        public CardType TargetType { get; private set; }
        public float Multiplier { get; private set; }
        public int DurationTurns { get; private set; }

        public SetCardTypeMultiplierGA(CardType targetType, float multiplier, int durationTurns)
        {
            TargetType = targetType;
            Multiplier = multiplier;
            DurationTurns = durationTurns;
        }
    }
}