namespace NYH.CoreCardSystem
{
    public class ResearchpointsGA : GameAction
    {
        public int Amount { get; private set; }

        public ResearchpointsGA(int amount)
        {
            Amount = amount;
        }
    }
}