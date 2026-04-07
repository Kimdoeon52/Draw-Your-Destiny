namespace NYH.CoreCardSystem
{
    using UnityEngine;

    public class ChooseOneGA : GameAction
    {
        public Card Card { get; private set; }
        public int Amount { get; private set; }

        public ChooseOneGA(int amount)
        {
            Amount = amount;
        }
        public ChooseOneGA(Card card)
        {
            Card = card;
        }
    }
}
