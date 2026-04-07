namespace NYH.CoreCardSystem
{
    using UnityEngine;

    public class PlayCardGA : GameAction
    {
        public Card Card { get; private set; }
        public PlayCardGA(Card card)
        {
            Card = card;
        }
    }
}
