using NYH.CoreCardSystem;
using UnityEngine;

public class SelectOneAndCopyEffect : Effect
{
    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new SelectOneAndCopyGA();
    }
}
