namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using SerializeReferenceEditor;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Data/Card")]
    public class CardData : ScriptableObject
    {
        [field: SerializeField] public int cardID { get; private set; }
        [field: SerializeField] private string cardName;
        [field: SerializeField] public CardType cardType { get; private set; }
        [field: SerializeField] public Sprite Image { get; private set; }
        [field: SerializeField] public int Cost { get; private set; }
        [field: SerializeField][TextArea(3, 5)] private string description;
        public string Description => description;
        [field: SerializeReference, SR] public List<Effect> Effects { get; private set; }
    }
}
