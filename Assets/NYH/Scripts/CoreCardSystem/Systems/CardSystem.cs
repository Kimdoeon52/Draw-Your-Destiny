namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using DG.Tweening;

    /// <summary>
    /// 카드 게임의 규칙(드로우, 플레이, 버리기 등)과 데이터(덱, 손패, 무덤)를 관리하는 핵심 시스템입니다.
    /// ActionSystem에서 전달받은 '동작'들을 실제로 수행(Perform)합니다.
    /// </summary>
    public class CardSystem : Singleton<CardSystem>
    {
        [Header("UI & Positions")]
        [SerializeField] private HandView handView;        // 손패를 보여주는 뷰
        [SerializeField] private Transform drawPilePoint;    // 덱 위치
        [SerializeField] private Transform discardPilePoint; // 무덤 위치

        [SerializeField] private Text deackCount;           // 덱 숫자를 띄울 텍스트
        [SerializeField] private Text discardCount;         // 무덤의 카드 숫자 띄울 텍스트


        // 게임 내 카드 데이터들
        private List<Card> drawPile = new();       // 덱
        private List<Card> hand = new();           // 손패
        private List<Card> discardPile = new();    // 무덤(버려진 카드)
        private List<Card> extinctionPile = new(); // 소멸칸

        protected override void Awake()
        {
            base.Awake();

            // 액션 연결 (AttachPerformer)
            ActionSystem.AttachPerformer<DrawCardsGA>(action => Perform(action));                 //카드 드로우 액션
            ActionSystem.AttachPerformer<PlayCardGA>(action => Perform(action));                  //카드 플레이 액션
            ActionSystem.AttachPerformer<DiscardAllCardsGA>(action => Perform(action));           //카드 전부 버리기 액션
            ActionSystem.AttachPerformer<DiscardRandomGA>(action => Perform(action));             //카드 랜덤 버리기 액션
            ActionSystem.AttachPerformer<ExtinctionCardGA>(action => Perform(action));            //카드 소멸 액션
            ActionSystem.AttachPerformer<GoldCardGA>(action => Perform(action));                  //골드 획득 액션
            ActionSystem.AttachPerformer<PlayCostGA>(action => Perform(action));                  //골드 사용 액션
            ActionSystem.AttachPerformer<ChooseOneGA>(action => Perform(action));                 //선택 카드 액션
            ActionSystem.AttachPerformer<ResearchpointsGA>(action => Perform(action));            //연구 포인트 획득 액션
            ActionSystem.AttachPerformer<IncreasePopulationGA>(action => Perform(action));        //인구 증가 획득 액션
            ActionSystem.AttachPerformer<CostPlusGA>(action => Perform(action));                  //코스트 증가
            ActionSystem.AttachPerformer<CountCardByTypeGA>(action => Perform(action));                  //손패 중 특정 타입 카드 갯수 세기

            Debug.Log("[CardSystem] 초기화 및 액션 등록 완료");
        }


        public void Setup(List<CardData> initialDeck)
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            foreach (var data in initialDeck)
            {
                if (data != null) drawPile.Add(new Card(data));
            }
            drawPile.Shuffle();
            Debug.Log($"[CardSystem] 덱 세팅 완료: {drawPile.Count}장");
        }

        /// <summary>
        /// ActionSystem에서 전달받은 모든 액션을 여기서 분기하여 처리합니다.
        /// </summary>
        public IEnumerator Perform(GameAction action)
        {
            if (action is DrawCardsGA drawCardsGA)
            {
                for (int i = 0; i < drawCardsGA.Amount; i++)
                {
                    if (drawPile.Count == 0) RefillDeck();
                    if (hand.Count >= 10) break;
                    if (drawPile.Count > 0)
                    {
                        yield return DrawOneCard();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            else if (action is PlayCardGA playCardGA)
            {
                yield return PlayCardPerformer(playCardGA);
            }
            else if (action is DiscardAllCardsGA discardAllCardsGA)
            {
                yield return DiscardAllCardsPerformer(discardAllCardsGA);
            }
            else if (action is DiscardRandomGA discardRandomGA)
            {
                for (int i = 0; i < discardRandomGA.Amount; i++)
                {
                    if (hand.Count == 0) break;
                    Card targetCard = hand[Random.Range(0, hand.Count)];
                    yield return DiscardCardByInstance(targetCard);
                }
            }
            else if (action is ExtinctionCardGA extinctionCardGA)
            {
                // 소멸 효과가 활성화되어 있고 대상 카드가 지정되어 있다면 소멸 처리
                if (extinctionCardGA.IsExtinction && extinctionCardGA.TargetCard != null)
                {
                    yield return ExtinctionCardByInstance(extinctionCardGA.TargetCard);
                }
            }
            else if (action is GoldCardGA goldCardGA)
            {
                GameManager.Instance.AddGold(goldCardGA.Amount);
                yield return null;
            }
            else if (action is PlayCostGA playCostGA)
            {
                GameManager.Instance.SpendGold(playCostGA.Amount);
                yield return null;
            }
            else if (action is ChooseOneGA chooseOneGA)
            {
                yield return ChooseCardPerformer(chooseOneGA.Amount);
            }
            else if (action is ResearchpointsGA researchpointsGA)
            {
                GameManager.Instance.AddResearch(researchpointsGA.Amount);
                yield return null;
            }
            else if (action is IncreasePopulationGA increasPoulationGA)
            {
                GameManager.Instance.IncreasePopulationCap(increasPoulationGA.Amount);
                yield return null;
            }
            else if (action is ContinueBehaviourGA continueGA)
            {
                OngoingEffectSystem.Instance.Register(
                    continueGA.SourceCard,
                    continueGA.StartEffectIndex,
                    continueGA.TurnAmount
                );
                yield return null;
            }
            else if (action is CostPlusGA costPlusGA)
            {
                costPlusGA.SourceCard.Cost += costPlusGA.Cost;
            }
            else if (action is CountCardByTypeGA countCardByTypeGA)
            {

            }
        }

        /// <summary>
        /// [발견 로직] 덱에서 N장을 보여주고 하나를 고릅니다.
        /// </summary>
        private IEnumerator ChooseCardPerformer(int amount)
        {
            if (drawPile.Count == 0) yield break;

            // 보여줄 카드 리스트 만들기 (덱에서 빼지 않고 복사만 함)
            List<Card> choices = new List<Card>();
            int actualAmount = Mathf.Min(amount, drawPile.Count);
            for (int i = 0; i < actualAmount; i++) choices.Add(drawPile[i]);

            // UI 호출 및 유저 선택 대기
            Card selectedCard = null;
            bool isChosen = false;

            if (CardSelectionUI.Instance == null)
            {
                Debug.LogError("[CardSystem] CardSelectionUI가 없습니다!");
                yield break;
            }

            CardSelectionUI.Instance.Show(choices, (card) => { selectedCard = card; isChosen = true; });
            yield return new WaitUntil(() => isChosen);

            // 선택된 카드 처리
            if (selectedCard != null)
            {
                drawPile.Remove(selectedCard); // 덱에서 제거
                deackCount.text = $"{drawPile.Count}장";
                hand.Add(selectedCard);        // 데이터 추가
                discardCount.text = $"{discardPile.Count}장";
                // 화면 연출
                CardView cardView = CardViewCreator.Instance.CreateCardView(selectedCard, Vector3.zero, Quaternion.identity);
                yield return handView.AddCard(cardView);
            }
        }

        private IEnumerator DrawOneCard()
        {

            Card card = drawPile.Draw();
            hand.Add(card);
            deackCount.text = $"{drawPile.Count}장";
            CardView cardView = CardViewCreator.Instance.CreateCardView(card, drawPilePoint.position, drawPilePoint.rotation);
            if (cardView != null) yield return handView.AddCard(cardView);
        }

        private IEnumerator PlayCardPerformer(PlayCardGA playCardGA)
        {
            hand.Remove(playCardGA.Card);
            CardView cardView = handView.RemoveCard(playCardGA.Card);

            if (cardView == null)
            {
                CardView[] allViews = FindObjectsByType<CardView>(FindObjectsSortMode.None);
                foreach (var cv in allViews)
                {
                    if (cv.Card == playCardGA.Card && !cv.IsHoverPreview) { cardView = cv; break; }
                }
            }

            // --- 소멸 효과 체크 로직 추가 ---
            bool isExtinction = false;
            if (playCardGA.Card?.Effects != null)
            {
                foreach (var effect in playCardGA.Card.Effects)
                {
                    if (effect is ExtinctionCardEffect)
                    {
                        isExtinction = true;
                        break;
                    }
                }
            }

            if (isExtinction)
            {
                yield return ExtinctionCardAnimation(cardView);
            }
            else
            {
                yield return DiscardCardAnimation(cardView);
            }
            // ---------------------------

            if (playCardGA.Card?.Effects != null)
            {
                for (int i = 0; i < playCardGA.Card.Effects.Count; i++)
                {
                    var effect = playCardGA.Card.Effects[i];
                    ActionSystem.Instance.AddReaction(new PerformEffectGA(playCardGA.Card, effect, i));
                }
            }
        }

        private void RefillDeck()
        {
            if (discardPile.Count > 0)
            {
                Debug.Log($"<color=yellow>[CardSystem] 덱이 비어있어 무덤의 {discardPile.Count}장을 다시 섞어 넣습니다!</color>");
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                drawPile.Shuffle();
                discardCount.text = $"{discardPile.Count}장";
            }
            else
            {
                Debug.LogWarning("[CardSystem] 덱과 무덤이 모두 비어있어 더 이상 뽑을 카드가 없습니다!");
            }
        }

        private IEnumerator DiscardCardAnimation(CardView cardView)
        {
            if (cardView == null) yield break;

            //데이터를 무덤 리스트에 추가.
            discardPile.Add(cardView.Card);
            discardCount.text = $"{discardPile.Count}장";

            // 2. 애니메이션 연출 (무덤 위치로 이동하며 작아짐)
            cardView.transform.DOKill();
            cardView.transform.DOScale(Vector3.zero, 0.2f);
            yield return cardView.transform.DOMove(discardPilePoint.position, 0.2f).WaitForCompletion();

            // 3. 게임 오브젝트 파괴 (메모리 관리)
            Destroy(cardView.gameObject);
        }

        private IEnumerator DiscardCardByInstance(Card targetCard)
        {
            if (targetCard == null) yield break;
            hand.Remove(targetCard);
            discardCount.text = $"{discardPile.Count}장";
            CardView cardView = handView.RemoveCard(targetCard);
            yield return DiscardCardAnimation(cardView);
        }

        private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
        {
            List<Card> cardsToDiscard = new List<Card>(hand);
            hand.Clear();
            foreach (var card in cardsToDiscard)
            {
                CardView cardView = handView.RemoveCard(card);
                StartCoroutine(DiscardCardAnimation(cardView));
                discardCount.text = $"{discardPile.Count}장";
                yield return new WaitForSeconds(0.05f);
            }
        }

        //카드를 소멸 칸으로
        private IEnumerator ExtinctionCardAnimation(CardView cardView)
        {
            if (cardView == null) yield break;

            // 1. 무덤이 아닌 소멸 리스트에 추가
            extinctionPile.Add(cardView.Card);
            // 소멸 카운트 텍스트가 있다면 업데이트 (예: extinctionCount.text = ...)

            // 2. 소멸 연출 (예: 무덤 위치가 아닌 다른 곳으로 이동하거나 그냥 제자리에서 소멸)
            cardView.transform.DOKill();
            // 무덤 대신 소멸 포인트로 이동하거나, Scale을 0으로 줄임
            yield return cardView.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).WaitForCompletion();

            // 3. 파괴
            Destroy(cardView.gameObject);
        }

        // 소멸 함수
        private IEnumerator ExtinctionCardByInstance(Card targetCard)
        {
            if (targetCard == null) yield break;
            // 손패에서 데이터 제거
            hand.Remove(targetCard);
            // 뷰 오브젝트 가져오기
            CardView cardView = handView.RemoveCard(targetCard);
            // 소멸 애니메이션 (무덤 대신 소멸)
            yield return ExtinctionCardAnimation(cardView);

        }

        /// <summary>
        /// 덱에 있는 모든 카드를 무작위 순서로 보여줍니다 (단순 확인용).
        /// </summary>
        public void ShowDeck()
        {
            if (drawPile.Count == 0) return;
            if (CardListUI.Instance == null) return;

            // 덱 데이터를 복사한 뒤 무작위로 섞습니다 (실제 덱 순서에는 영향을 주지 않음)
            List<Card> shuffledCopy = new List<Card>(drawPile);
            shuffledCopy.Shuffle();

            CardListUI.Instance.Show(shuffledCopy, "덱 확인");
        }

        /// <summary>
        /// 무덤에 있는 모든 카드를 무작위 순서로 보여줍니다 (단순 확인용).
        /// </summary>
        public void ShowGraveyard()
        {
            if (discardPile.Count == 0) return;
            if (CardListUI.Instance == null) return;

            List<Card> shuffledCopy = new List<Card>(discardPile);
            shuffledCopy.Shuffle();

            CardListUI.Instance.Show(shuffledCopy, "무덤 확인");
        }
    }
}
