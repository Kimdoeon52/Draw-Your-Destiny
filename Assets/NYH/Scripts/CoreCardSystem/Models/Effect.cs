using System.Collections.Generic;
using NYH.CoreCardSystem;

[System.Serializable]
public abstract class Effect
{
    public abstract GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null);

    public virtual Dictionary<string, string> GetDescriptionTokens()
    {
        return null;
    }

    public virtual Dictionary<string, string> GetDescriptionTokens(Card sourceCard)
    {
        return GetDescriptionTokens();
    }
}