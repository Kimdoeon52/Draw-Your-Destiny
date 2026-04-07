using NYH.CoreCardSystem;

public class DisableCardTypeGA : GameAction
{
    public CardType TargetType { get; private set; }
    public int DurationTurns { get; private set; }

    public DisableCardTypeGA(CardType targetType, int durationTurns)
    {
        TargetType = targetType;
        DurationTurns = durationTurns;
    }
}
