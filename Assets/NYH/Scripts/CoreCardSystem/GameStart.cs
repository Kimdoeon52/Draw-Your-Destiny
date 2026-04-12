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

        // 문명씬 복귀 시 덱/손패/무덤 상태를 복원할지 결정하는 시작 지점입니다.
        // 전투씬으로 넘어가기 직전에 Store(CaptureRuntimeState())를 해두었다면,
        // 여기서 TryConsume()으로 저장 상태를 꺼내 RestoreRuntimeState()로 넘깁니다.
        CivilizationDeckStateStore deckStateStore = CivilizationDeckStateStore.GetOrCreate();

        
        bool hasStoredState = deckStateStore.HasStoredState;
        bool consumed = deckStateStore.TryConsume(out CardPileRuntimeState storedState);

        Debug.Log($"GameStarter: 덱 상태 저장소에 저장된 상태가 {(hasStoredState ? "있습니다" : "없습니다")}. TryConsume 결과: {(consumed ? "성공" : "실패")}. storedState는 {(storedState != null ? "유효합니다" : "null입니다")}.");


        if (CardSystem.Instance != null && consumed)
        {
            // 저장된 런타임 상태가 있으면 myDeck으로 새로 시작하지 않고,
            // 저장해둔 draw/hand/discard/extinction 상태를 그대로 복원합니다.
            if (!CardSystem.Instance.RestoreRuntimeState(storedState))
            {
                Debug.LogWarning("GameStarter: 저장된 문명 덱 복원에 실패해 기본 덱으로 다시 세팅합니다.");
                if (myDeck != null && myDeck.Count > 0)
                {
                    CardSystem.Instance.Setup(myDeck);
                    yield return new WaitForSeconds(0.1f);
                    ActionSystem.Instance.Perform(new DrawCardsGA(5));
                }
            }
        }
        else if (CardSystem.Instance != null && myDeck != null && myDeck.Count > 0)
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
        StartCoroutine(StartTurnFlow());
    }

    private IEnumerator DrawTurnCardsRoutine()
    {
        ActionSystem.Instance.Perform(new DrawCardsGA(5)); //카드 5장 드로우
        yield break;
    }

    private IEnumerator StartTurnFlow()
    {
        Debug.Log("턴 시작: 카드 세트 3개 중 1개를 고른 뒤 5장을 뽑습니다.");

        if (CardSystem.Instance == null)
        {
            Debug.LogError("GameStarter: CardSystem이 없어 턴 시작 보상/드로우를 진행할 수 없습니다.");
            yield break;
        }

        yield return StartCoroutine(CardSystem.Instance.OfferRewardBundlesToDecks(3));
        yield return StartCoroutine(DrawTurnCardsRoutine());
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
