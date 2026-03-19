namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    [CreateAssetMenu(menuName = "Data/Card")]
    public class CardData : ScriptableObject
    {
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public int Mana { get; private set; }
        [field: SerializeField] public Sprite Image { get; private set; }
    }
}
