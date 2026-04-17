namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using NYH.BattleCardSystem;

    public static class CardPresentationTextFormatter
    {
        public static CardPresentationData CreateForCivilization(CardData cardData)
        {
            return new CardPresentationData
            {
                VisualKind = CardVisualKind.Civilization,
                CardTypeText = FormatCardType(cardData != null ? cardData.cardType : CardType.None),
                CardUseTypeText = FormatCardUseTypes(cardData != null ? cardData.cardUseType : null),
                MoveRangeText = "-",
                GridImage = null,
            };
        }

        public static CardPresentationData CreateForBattle(BattleCardData battleCardData)
        {
            return new CardPresentationData
            {
                VisualKind = CardVisualKind.Battle,
                CardTypeText = "-",
                CardUseTypeText = "-",
                MoveRangeText = FormatMoveRange(battleCardData != null ? battleCardData.DisplayMoveRange : 0),
                GridImage = battleCardData != null ? battleCardData.GridImage : null,
            };
        }

        public static string FormatCardType(CardType cardType)
        {
            return cardType switch
            {
                CardType.Fight => "전투",
                CardType.Money => "경제",
                CardType.Normal => "일반",
                CardType.Common => "공용",
                _ => "-",
            };
        }

        public static string FormatCardUseTypes(IReadOnlyList<CardUseType> cardUseTypes)
        {
            if (cardUseTypes == null || cardUseTypes.Count == 0)
            {
                return "-";
            }

            List<string> labels = new();
            HashSet<CardUseType> usedTypes = new();

            for (int i = 0; i < cardUseTypes.Count; i++)
            {
                CardUseType cardUseType = cardUseTypes[i];
                if (!usedTypes.Add(cardUseType))
                {
                    continue;
                }

                string label = FormatCardUseType(cardUseType);
                if (label == "-")
                {
                    continue;
                }

                labels.Add(label);
            }

            return labels.Count == 0 ? "-" : string.Join(",", labels);
        }

        public static string FormatMoveRange(int moveRange)
        {
            return moveRange > 0 ? moveRange.ToString() : "-";
        }

        private static string FormatCardUseType(CardUseType cardUseType)
        {
            return cardUseType switch
            {
                CardUseType.Building => "건물",
                CardUseType.Remove => "소멸",
                CardUseType.Volatile => "휘발성",
                CardUseType.Forever => "영구",
                CardUseType.Skill => "스킬",
                _ => "-",
            };
        }
    }
}
