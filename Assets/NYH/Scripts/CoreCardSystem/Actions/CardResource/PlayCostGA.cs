namespace NYH.CoreCardSystem
{
    public class PlayCostGA : GameAction
    {
        public int Amount { get; private set; }

        public PlayCostGA(int amount)
        {
            Amount = amount;
        }
    }
}
