using UnityEngine;
using NYH.CoreCardSystem;

[CreateAssetMenu(menuName = "Data/Era Card")]
public class EraCardData : ScriptableObject
{
    public int sharedCardID;
    public CardData stoneCard;
    public CardData bronzeCard;
    public CardData ironCard;

    public CardData GetCardByEra(Era era)
    {
        return era switch
        {
            Era.Stone => stoneCard,
            Era.Bronze => bronzeCard != null ? bronzeCard : stoneCard,
            Era.Iron => ironCard != null ? ironCard : bronzeCard != null ? bronzeCard : stoneCard,
            _ => stoneCard
        };
    }
}
