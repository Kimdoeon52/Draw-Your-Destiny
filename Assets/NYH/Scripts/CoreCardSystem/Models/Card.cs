namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    public class Card
    {
        public string Title => data.name;
        public string Description => data.Description;
        public Sprite Image => data.Image;
        public int Mana { get; private set; }
        private readonly CardData data;
        public Card(CardData cardData)
        {
            data = cardData;
            Mana = cardData.Mana;
        }
    }
}
