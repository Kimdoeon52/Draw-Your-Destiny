using NYH.CoreCardSystem;
using UnityEngine;

public class ContinueBehaviourGA : GameAction
{
    public Card SourceCard { get; }
    public int StartEffectIndex { get; }
    public int TurnAmount { get; private set; }

    public ContinueBehaviourGA(Card sourceCard, int startEffectIndex, int turnAmount)
    {
        SourceCard = sourceCard;
        StartEffectIndex = startEffectIndex;
        TurnAmount = turnAmount;
    }
}
