namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using SerializeReferenceEditor;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Data/Card")]
    public class CardData : CardDataBase
    {
        [field: SerializeField] public int cardID { get; private set; }
        [field: SerializeField] public string cardName;
        [field: SerializeField] public CardType cardType { get; private set; }
        [field: SerializeField] public CardUseType cardUseType { get; private set; }
        [field: SerializeField] public Sprite Image { get; private set; }
        [field: SerializeField] public int Cost { get; private set; }
        [field: SerializeField]
        [TextArea(3, 5)]
        private string description;
        public string Description => description;
        [field: SerializeReference, SR] public List<Effect> Effects { get; private set; }

        public override int SharedCardID => cardID;
        public override string SharedCardName => cardName;
        public override Sprite SharedImage => Image;
        public override string SharedDescription => Description;
        public override int SharedBaseCost => Cost;
        public override IReadOnlyList<Effect> SharedEffects => Effects;
    }
}
