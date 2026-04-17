namespace NYH.CoreCardSystem
{
    public enum CardVisualKind
    {
        Civilization,
        Battle,
    }

    public sealed class CardPresentationData
    {
        public CardVisualKind VisualKind { get; set; } = CardVisualKind.Civilization;
        public string CardTypeText { get; set; } = "-";
        public string CardUseTypeText { get; set; } = "-";
        public string MoveRangeText { get; set; } = "-";
    }
}
