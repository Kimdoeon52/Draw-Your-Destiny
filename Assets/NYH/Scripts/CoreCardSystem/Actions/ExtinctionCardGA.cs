namespace NYH.CoreCardSystem
{
    public class ExtinctionCardGA : GameAction
    {
        public bool IsExtinction { get; private set; }
        public Card TargetCard { get; private set; }

        public ExtinctionCardGA(bool isExtinction, Card targetCard = null)
        {
            IsExtinction = isExtinction;
            TargetCard = targetCard;
        }
    }
}
