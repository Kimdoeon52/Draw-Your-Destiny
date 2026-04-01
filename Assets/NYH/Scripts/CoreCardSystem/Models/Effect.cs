
using NYH.CoreCardSystem;
[System.Serializable]
public abstract class Effect 
{
    public abstract GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null);

    public virtual System.Collections.Generic.Dictionary<string, string> GetDescriptionTokens()
    {
        return null;
    }
}
