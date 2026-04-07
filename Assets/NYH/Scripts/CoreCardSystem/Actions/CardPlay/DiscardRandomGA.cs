namespace NYH.CoreCardSystem
{
    public class DiscardRandomGA : GameAction
    {
        public int Amount { get; private set; }

        public DiscardRandomGA(int amount)
        {
            Amount = amount;
        }
    }
}
