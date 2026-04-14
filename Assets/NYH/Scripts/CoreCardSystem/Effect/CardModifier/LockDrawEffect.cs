using NYH.CoreCardSystem;
using UnityEngine;

public class LockDrawEffect : Effect
{
    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        return new LockDrawGA();
    }
}
