namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    public enum CardType
    {
        None,
        Fight,
        Money,
        Science,
        Normal,
        Common
    }

    public class Card : IRuntimeCard
    {
        public string Title => data.cardName;
        public string Description => BuildDescription();
        public Sprite Image => data.Image;
        public List<Effect> Effects => data.Effects;
        public int CardID => data.cardID;
        public CardType _CardType => data.cardType;

        public int Cost { get; set; }
        public CardData data { get; private set; }

        public int RuntimeCardID => CardID;
        public string RuntimeTitle => Title;
        public string RuntimeDescription => Description;
        public Sprite RuntimeImage => Image;
        public int RuntimeCost => Cost;
        public IReadOnlyList<Effect> RuntimeEffects => Effects;

        public Card(CardData cardData)
        {
            data = cardData;
            Cost = cardData.Cost;
        }

        private string BuildDescription()
        {
            string description = data.Description;
            if (string.IsNullOrEmpty(description) || data.Effects == null)
            {
                return description;
            }

            StringBuilder builder = new StringBuilder(description);
            foreach (var effect in data.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                var tokens = effect.GetDescriptionTokens(this);
                if (tokens == null)
                {
                    continue;
                }

                foreach (var pair in tokens)
                {
                    builder.Replace($"{{{pair.Key}}}", pair.Value);
                }
            }

            return builder.ToString();
        }
    }
}
