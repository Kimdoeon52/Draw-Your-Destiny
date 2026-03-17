using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
 using NYH.CoreCardSystem; // 중요: 제가 설정해드린 네임스페이스입니다!
    
     public class GameStarter : MonoBehaviour
     {
         // 에디터에서 우리가 만든 카드 데이터(CardData)들을 여기에 넣을 거예요.
         [SerializeField] private List<CardData> myDeck;
    
        void Start()
        {
            // 1. 게임 시작 시 덱을 설정합니다. (CardSystem에게 데이터를 넘겨줌)
            if (CardSystem.Instance != null && myDeck != null && myDeck.Count > 0)
            {
                CardSystem.Instance.Setup(myDeck);
   
                // 2. 처음 시작할 때 카드 5장을 뽑으라고 명령합니다.
                ActionSystem.Instance.Perform(new DrawCardsGA(5));
            }
         else
             {
                 Debug.LogWarning("덱 데이터가 비어있거나 매니저가 없습니다!");
             }
     }
   
        void Update()
        {
             // 테스트용: 게임 중에 [D] 키를 누르면 카드 1장을 더 뽑습니다.
            if (Input.GetKeyDown(KeyCode.D))
                 {
                     ActionSystem.Instance.Perform(new DrawCardsGA(1));
                 }
         }
 }