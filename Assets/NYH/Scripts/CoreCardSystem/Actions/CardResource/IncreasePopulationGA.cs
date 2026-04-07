namespace NYH.CoreCardSystem
{
    public class IncreasePopulationGA : GameAction
    {
        public int Amount { get; private set; }

        public IncreasePopulationGA(int amount)
        {
            Amount = amount;
        }
    }
}