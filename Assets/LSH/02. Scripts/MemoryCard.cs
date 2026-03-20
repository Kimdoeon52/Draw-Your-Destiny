using NUnit.Framework;
using UnityEngine;

public class MemoryCard : MonoBehaviour
{
    //private List<> usedCardsThisTurn = new List<>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private HumanPool humanPool;

    void Start()
    {
        if (humanPool == null)
            humanPool = FindAnyObjectByType<HumanPool>();
    }

    //public void UseCard(CardType cardType)
    //{
    //    if(cardType == "인간전용")
    //    {
    //        usedCardsThisTurn.Add(cardType);
    //        Debug.Log($"카드 저장됨: {cardType}");
    //    }
    //}

    // Update is called once per frame
    //public void UseAllUnitTurnCards()
    //{
    //    Debug.Log("일괄 실행");

    //    foreach (CardType card in usedCardsThisTurn) //여기에 해당 카드 효과 집어넣자^^.
    //    {
    //        switch (card)
    //        {
    //            case CardType.AdultUnitCard://대충 이런 카드를 사용하면...?
    //                SpawnAdultHumans(5); //이런 효과. 
    //                break;
    //        }
    //    }

    //    usedCardsThisTurn.Clear(); //턴끝나면 싹다 버리고
    //}
}
