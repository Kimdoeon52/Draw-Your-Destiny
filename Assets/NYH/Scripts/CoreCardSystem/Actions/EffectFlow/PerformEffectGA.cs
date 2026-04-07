using NYH.CoreCardSystem;
using UnityEngine;

public class PerformEffectGA : GameAction
{
    public Card SourceCard { get; }
    public Effect Effect { get; }
    public int EffectIndex { get; }

    public PerformEffectGA(Card sourceCard, Effect effect, int effectIndex)
    {
        SourceCard = sourceCard;
        Effect = effect;
        EffectIndex = effectIndex;
    }
}
