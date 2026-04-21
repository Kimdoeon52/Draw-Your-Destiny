namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleCardSystem
     *
     * 전투 씬에서 사용하는 카드 시스템 본체입니다.
     *
     * 담당 역할:
     * - 전투 덱 / 손패 / 버림더미 같은 런타임 카드 상태 관리
     * - 시작 손패 드로우, 턴 드로우, 멀리건 처리
     * - 전투 카드 플레이 시 ActionSystem으로 액션 위임
     * - 행동력(AP) 기반 카드 코스트 계산
     *
     * 사용 흐름:
     * - 전투 시작 전에 SetupFromInspector() 또는 SetupBattleDeck()으로 덱 준비
     * - BattleManager가 DrawOpeningHand(), DrawTurnCards() 호출
     * - 카드 사용 시 PlayCard() 호출
     */
    public class BattleCardSystem : Singleton<BattleCardSystem>
    {
        [Header("Fallback Battle Deck Sources")]
        // BattleDeckCollection이 없는 경우에만 사용하는 기본 전투 덱/보상 덱입니다.
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();
        [SerializeField] private List<BattleCardData> earnedBattleCards = new();

        [Header("Battle Cost Rules")]
        // 카드 사용 시 적용되는 AP 최대치입니다.
        [SerializeField] private int maxActionPoints = 15;

        // 실제 전투 중 사용하는 런타임 더미/손패 상태입니다.
        private BattleCardPileState pileState;
        // 전투 카드 사용과 전술 액션을 처리하는 performer입니다.
        private BattlePlayPerformer playPerformer;
        private BattleTacticalPerformer tacticalPerformer;
        private BattleCardCostService costService;
        private BattleOpeningHandService openingHandService;

        public BattleCardPileState PileState => pileState;
        public int CurrentActionPoints => costService != null ? costService.CurrentActionPoints : 0;

        protected override void Awake()
        {
            base.Awake();

            // 전투 씬 진입 시 필요한 런타임 상태 객체와 액션 처리기를 준비합니다.
            pileState = new BattleCardPileState();
            costService = new BattleCardCostService(maxActionPoints);
            openingHandService = new BattleOpeningHandService();
            playPerformer = new BattlePlayPerformer(pileState, CanAffordCardCost, ResolveCardCost);
            tacticalPerformer = new BattleTacticalPerformer();

            // ActionSystem에서 전투 카드 관련 액션이 들어오면 이 시스템으로 라우팅합니다.
            ActionSystem.AttachPerformer<BattlePlayCardGA>(action => Perform(action));
            ActionSystem.AttachPerformer<BattleAttackGA>(action => Perform(action));
            ActionSystem.AttachPerformer<BattleMoveGA>(action => Perform(action));
        }

        /// <summary>
        /// 기본 전투 덱과 보상으로 얻은 전투 카드를 합쳐 현재 전투용 draw pile을 구성합니다.
        /// </summary>
        public void SetupBattleDeck(IEnumerable<BattleCardData> baseDeck, IEnumerable<BattleCardData> earnedCards)
        {
            List<BattleCardData> mergedDeck = new();
            if (baseDeck != null)
            {
                mergedDeck.AddRange(baseDeck);
            }

            if (earnedCards != null)
            {
                mergedDeck.AddRange(earnedCards);
            }

            pileState.Setup(mergedDeck);
        }

        /// <summary>
        /// 가능하면 BattleDeckCollection에서 전투 덱을 읽어오고,
        /// 컬렉션이 없으면 인스펙터에 넣어둔 fallback 덱으로 세팅합니다.
        /// 전투 시작 시 AP도 0으로 초기화합니다.
        /// </summary>
        public void SetupFromInspector()
        {
            if (BattleDeckCollection.Instance != null)
            {
                if (BattleDeckCollection.Instance.BaseBattleDeck.Count == 0 && baseBattleDeck.Count > 0)
                {
                    Debug.LogWarning($"[BattleCardSystem] BattleDeckCollection의 기본 덱이 비어 있어 fallback 기본 덱 {baseBattleDeck.Count}장을 복사합니다.");
                    BattleDeckCollection.Instance.ConfigureBaseDeck(baseBattleDeck);
                }

                SetupBattleDeck(
                    BattleDeckCollection.Instance.BaseBattleDeck,
                    BattleDeckCollection.Instance.EarnedBattleCards);
            }
            else
            {
                Debug.LogWarning($"[BattleCardSystem] SetupFromInspector: BattleDeckCollection이 없어 fallback 덱을 사용합니다. baseDeck={baseBattleDeck.Count}, earned={earnedBattleCards.Count}");
                SetupBattleDeck(baseBattleDeck, earnedBattleCards);
            }

            SetupActionPoints(0);
        }

        /// <summary>
        /// 현재 AP를 최대치 범위 안에서 강제로 설정합니다.
        /// </summary>
        public void SetupActionPoints(int actionPoints)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            costService.Setup(actionPoints);
        }

        /// <summary>
        /// AP를 증감시키되 0과 최대치 사이로 보정합니다.
        /// </summary>
        public void AddActionPoints(int amount)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            costService.Add(amount);
        }

        /// <summary>
        /// 플레이어 턴 시작 시 턴 수만큼 AP를 얻습니다.
        /// 예: 3턴 시작이면 AP 3 증가
        /// </summary>
        public int GainTurnActionPoints(int turnNumber)
        {
            int gainAmount = Mathf.Max(0, /*turnNumber*/ 10); // demo build: fixed 10 AP gain regardless of turn.
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.Add(gainAmount);
        }

        /// <summary>
        /// 전투 보상 카드를 전투 덱에 추가합니다.
        /// BattleDeckCollection이 있으면 영속 컬렉션에 반영하고,
        /// 없으면 현재 런타임 pileState에 직접 반영합니다.
        /// </summary>
        public BattleDeckAddResult AddEarnedBattleCard(BattleCardData data, BattleCard replaceTarget = null)
        {
            if (BattleDeckCollection.Instance != null)
            {
                BattleCardData replaceTargetData = replaceTarget != null ? replaceTarget.Data : null;
                return BattleDeckCollection.Instance.AddBattleRewardCard(data, replaceTargetData);
            }

            BattleDeckAddResult result = pileState.AddRewardCard(data, replaceTarget);
            if (result == BattleDeckAddResult.Added || result == BattleDeckAddResult.Replaced)
            {
                earnedBattleCards.Add(data);
            }

            return result;
        }

        /// <summary>
        /// 포션 카드는 일반 제한 카드와 다르게 바로 전투 덱에 추가합니다.
        /// </summary>
        public void AddPotionCard(BattleCardData potionData)
        {
            if (BattleDeckCollection.Instance != null)
            {
                BattleDeckCollection.Instance.AddPotionCard(potionData);
                return;
            }

            pileState.AddPotionCard(potionData);
        }

        /// <summary>
        /// 전투 시작 손패를 뽑습니다.
        /// 현재 규칙은 "살아 있는 플레이어 유닛 종류 수 + 1장"이며 최소 1장입니다.
        /// </summary>
        public List<BattleCard> DrawOpeningHand(int unitTypeCount)
        {
            openingHandService ??= new BattleOpeningHandService();
            int drawCount = openingHandService.CalculateDrawCountByAliveUnitTypes(unitTypeCount);
            return pileState.DrawCards(drawCount);
        }

        /// <summary>
        /// 멀리건 시 손패를 덱으로 되돌리고 섞은 뒤,
        /// 시작 손패 규칙으로 다시 카드를 뽑습니다.
        /// </summary>
        public List<BattleCard> MulliganOpeningHand(int unitTypeCount)
        {
            pileState.ReturnHandToDrawPileAndShuffle();
            return DrawOpeningHand(unitTypeCount);
        }

        /// <summary>
        /// 시작 멀리건에서 선택한 카드만 덱으로 되돌리고 다시 뽑습니다.
        /// 선택하지 않은 카드는 손패에 그대로 유지됩니다.
        /// </summary>
        public BattleMulliganResult MulliganSelectedCards(IReadOnlyList<BattleCard> selectedCards)
        {
            return pileState.MulliganSelectedCards(selectedCards);
        }

        /// <summary>
        /// 플레이어 턴 시작 시 드로우합니다.
        /// 현재 규칙은 시작 손패와 동일하게 "살아 있는 유닛 종류 수 + 1장"입니다.
        /// </summary>
        public List<BattleCard> DrawTurnCards(int aliveUnitTypeCount)
        {
            openingHandService ??= new BattleOpeningHandService();
            int drawCount = openingHandService.CalculateDrawCountByAliveUnitTypes(aliveUnitTypeCount);
            return pileState.DrawCards(drawCount);
        }

        /// <summary>
        /// 턴 종료 시 손패를 전부 버립니다.
        /// </summary>
        public void EndTurnDiscardHand()
        {
            pileState.DiscardHand();
        }

        /// <summary>
        /// 현재 draw pile을 일반 카드 미리보기 UI로 변환해 보여줍니다.
        /// </summary>
        public void ShowDeck()
        {
            if (pileState.DrawPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(
                ConvertToPreviewCards(pileState.GetShuffledDrawPileCopy()),
                "전투 덱 확인");
        }

        /// <summary>
        /// 현재 discard pile을 일반 카드 미리보기 UI로 변환해 보여줍니다.
        /// </summary>
        public void ShowDiscardPile()
        {
            if (pileState.DiscardPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(
                ConvertToPreviewCards(pileState.GetShuffledDiscardPileCopy()),
                "전투 버림더미 확인");
        }

        /// <summary>
        /// 전투 카드 플레이를 ActionSystem에 위임합니다.
        /// 실제 처리 자체는 BattlePlayPerformer / BattleTacticalPerformer 쪽에서 수행됩니다.
        /// </summary>
        public void PlayCard(
            BattleCard card,
            BattleUnit userUnit,
            Vector2Int targetPosition,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            BattleUnit targetUnit = null,
            bool skipFollowUpAttack = false,
            bool skipPostAttackMove = false,
            System.Action onFinished = null)
        {
            int currentHealth = userUnit != null ? userUnit.CurrentHealth : 0;
            int unitSpeed = userUnit != null ? userUnit.CurrentSpeed : 0;
            ActionSystem.Instance.Perform(
                new BattlePlayCardGA(card, userUnit, targetUnit, targetPosition, attackTargetPositions, plannedPath, currentHealth, unitSpeed, skipFollowUpAttack, skipPostAttackMove),
                onFinished);
        }

        /// <summary>
        /// ActionSystem에서 전달받은 전투 관련 액션을 적절한 performer로 라우팅합니다.
        /// </summary>
        public IEnumerator Perform(GameAction action)
        {
            if (playPerformer.CanHandle(action))
            {
                yield return playPerformer.Perform(action);
                yield break;
            }

            if (tacticalPerformer.CanHandle(action))
            {
                yield return tacticalPerformer.Perform(action);
            }
        }

        /// <summary>
        /// 카드 코스트를 현재 AP로 지불할 수 있는지 확인합니다.
        /// </summary>
        private bool CanAffordCardCost(BattleCard card)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.CanAfford(card);
        }

        /// <summary>
        /// 카드 코스트를 AP로 지불하고, 부족하면 실패합니다.
        /// </summary>
        private (bool paidByActionPoints, int actionPointsSpent, int healthPenalty) ResolveCardCost(BattleCard card, int userCurrentHealth)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.TryPay(card);
        }

        /// <summary>
        /// 전투 카드 목록을 CoreCardSystem용 미리보기 Card 목록으로 변환합니다.
        /// 덱/버림더미 UI에서 재사용하기 위한 어댑터 역할입니다.
        /// </summary>
        private static List<Card> ConvertToPreviewCards(IEnumerable<BattleCard> battleCards)
        {
            List<Card> previewCards = new();
            if (battleCards == null)
            {
                return previewCards;
            }

            foreach (var battleCard in battleCards)
            {
                Card previewCard = BattleCardViewAdapter.CreatePreviewCard(battleCard);
                if (previewCard != null)
                {
                    previewCards.Add(previewCard);
                }
            }

            return previewCards;
        }
    }
}