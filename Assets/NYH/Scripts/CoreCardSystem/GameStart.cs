using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class GameStarter : MonoBehaviour
{
    [SerializeField] private List<CardData> myDeck;
    [SerializeField] private GameManager gameManager; // 인스펙터에서 확인 가능하도록 수정

    private IEnumerator Start()
    {
        yield return null; // 시스템 초기화 대기

        // GameManager가 연결되지 않았다면 씬에서 찾습니다.
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }
        }

        if (gameManager == null)
        {
            Debug.LogError("GameStarter: 씬에 GameManager가 없습니다!");
        }

        if (CardSystem.Instance != null && myDeck != null && myDeck.Count > 0)
        {
            CardSystem.Instance.Setup(myDeck);
            yield return new WaitForSeconds(0.1f);
            ActionSystem.Instance.Perform(new DrawCardsGA(5));
        }
        else
        {
            Debug.LogError("GameStarter: 덱 정보가 없거나 CardSystem이 없습니다."); 
        }
    }

    private void EndTurnCard()
    {
        Debug.Log("턴 종료: 모든 카드를 버립니다.");
        ActionSystem.Instance.Perform(new DiscardAllCardsGA());
        if(gameManager != null) gameManager.endTurn = false; // 플래그 리셋
    }

    private void StartTurnCard()
    {
        if (gameManager != null) gameManager.startTurn = false; // 플래그 리셋
        StartCoroutine(StartTurnCardRoutine());
    }

    // 턴 시작시 카드 3장 덱에 넣기 카드 시스템 합칠때까지 주석처리
    private IEnumerator StartTurnCardRoutine()
    {
        //카드 선택
        Debug.Log("턴 시작: 카드 3장 중 1장을 덱에 추가한 뒤 5장을 뽑습니다.");

        if (CardSystem.Instance != null)
        {
            yield return CardSystem.Instance.OfferRandomCatalogCardToDeck(3);
        }

        ActionSystem.Instance.Perform(new DrawCardsGA(5));
    }

    void Update()
    {
        // 디버그용 단축키
        if (Input.GetKeyDown(KeyCode.D))
        {
            ActionSystem.Instance.Perform(new DrawCardsGA(1));
        }

        // F키를 누르면 수동으로 턴 전환 로직 테스트
        if (Input.GetKeyDown(KeyCode.F))
        {
            EndTurnCard();
            StartTurnCard();
        }

        // [추가] 매 프레임 GameManager의 상태를 감지하여 자동으로 반응
        if (gameManager != null)
        {
            if (gameManager.endTurn) EndTurnCard();
            if (gameManager.startTurn) StartTurnCard();
        }
    }
}
