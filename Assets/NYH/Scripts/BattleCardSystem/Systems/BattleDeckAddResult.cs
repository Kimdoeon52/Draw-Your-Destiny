namespace NYH.BattleCardSystem
{
    /// <summary>
    /// Result of attempting to add a battle reward card into a deck.
    /// </summary>
    public enum BattleDeckAddResult
    {
        Added,
        Replaced,
        NeedsReplacement,
        Invalid,
    }
}
