namespace NYH.CoreCardSystem
{
    public class GoldCardGA : GameAction
    {
        public int Amount { get; private set; }

        public GoldCardGA(int amount)
        {
            Amount = amount;
        }
    }
}
