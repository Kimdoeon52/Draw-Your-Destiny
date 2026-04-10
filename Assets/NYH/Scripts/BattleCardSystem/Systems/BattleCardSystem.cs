namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleCardSystem
     *
     * 역할:
     * - 전투 씬에서 실제 전투 덱, 손패, 식량 코스트, 전투 카드 사용 흐름을 관리합니다.
     * - BattleDeckCollection에 저장된 기본 전투 덱/획득 전투 카드를 읽어 실제 전투용 런타임 덱을 구성합니다.
     *
     * 인스펙터에서 넣는 것:
     * - Fallback Battle Deck Sources:
     *   BattleDeckCollection이 없는 테스트 상황에서만 사용할 기본/획득 카드 목록
     * - Battle Cost Rules:
     *   시작 식량과 식량 부족 시 체력 페널티 비율
     *
     * 사용하는 법:
     * - 전투 씬에 1개만 둡니다.
     * - 전투 시작 시 SetupFromInspector() 또는 SetupBattleDeck()을 호출합니다.
     * - 카드 사용은 PlayCard()로 시작합니다.
     */
    public class BattleCardSystem : Singleton<BattleCardSystem>
    {
        [Header("대체 전투 덱 소스 (BattleDeckCollection이 없을 때만 사용)")]
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();
        [SerializeField] private List<BattleCardData> earnedBattleCards = new();

        [Header("전투 코스트 규칙")]
        [SerializeField] private int startingFood = 10;
        [SerializeField, Range(0f, 1f)] private float healthPenaltyPerCostStep = 0.1f;

        private BattleCardPileState pileState;
        private BattlePlayPerformer playPerformer;
        private BattleTacticalPerformer tacticalPerformer;

        public BattleCardPileState PileState => pileState;
        public int CurrentFood { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            pileState = new BattleCardPileState();
            playPerformer = new BattlePlayPerformer(pileState, ResolveCardCost);
            tacticalPerformer = new BattleTacticalPerformer();

            ActionSystem.AttachPerformer<BattlePlayCardGA>(action => Perform(action));
            ActionSystem.AttachPerformer<BattleAttackGA>(action => Perform(action));
            ActionSystem.AttachPerformer<BattleMoveGA>(action => Perform(action));
        }

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

        public void SetupFromInspector()
        {
            if (BattleDeckCollection.Instance != null)
            {
                if (BattleDeckCollection.Instance.BaseBattleDeck.Count == 0 && baseBattleDeck.Count > 0)
                {
                    BattleDeckCollection.Instance.ConfigureBaseDeck(baseBattleDeck);
                }

                SetupBattleDeck(
                    BattleDeckCollection.Instance.BaseBattleDeck,
                    BattleDeckCollection.Instance.EarnedBattleCards);
            }
            else
            {
                SetupBattleDeck(baseBattleDeck, earnedBattleCards);
            }

            SetupBattleFood(startingFood);
        }

        public void SetupBattleFood(int food)
        {
            CurrentFood = Mathf.Max(0, food);
        }

        public void AddFood(int amount)
        {
            CurrentFood = Mathf.Max(0, CurrentFood + amount);
        }

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

        public void AddPotionCard(BattleCardData potionData)
        {
            if (BattleDeckCollection.Instance != null)
            {
                BattleDeckCollection.Instance.AddPotionCard(potionData);
                return;
            }

            pileState.AddPotionCard(potionData);
        }

        public List<BattleCard> DrawOpeningHand(int unitTypeCount)
        {
            int drawCount = Mathf.Max(1, unitTypeCount + 1);
            return pileState.DrawCards(drawCount);
        }

        public List<BattleCard> MulliganOpeningHand(int unitTypeCount)
        {
            pileState.ReturnHandToDrawPileAndShuffle();
            return DrawOpeningHand(unitTypeCount);
        }

        public List<BattleCard> DrawTurnCards(int aliveUnitTypeCount)
        {
            int drawCount = Mathf.Max(1, aliveUnitTypeCount + 1);
            return pileState.DrawCards(drawCount);
        }

        public void EndTurnDiscardHand()
        {
            pileState.DiscardHand();
        }

        public void PlayCard(
            BattleCard card,
            BattleUnit userUnit,
            Vector2Int targetPosition,
            BattleUnit targetUnit = null,
            System.Action onFinished = null)
        {
            int currentHealth = userUnit != null ? userUnit.CurrentHealth : 0;
            int unitSpeed = userUnit != null ? userUnit.Speed : 0;
            ActionSystem.Instance.Perform(
                new BattlePlayCardGA(card, userUnit, targetUnit, targetPosition, currentHealth, unitSpeed),
                onFinished);
        }

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

        private (bool paidByFood, int foodSpent, int healthPenalty) ResolveCardCost(BattleCard card, int userCurrentHealth)
        {
            if (card == null)
            {
                return (false, 0, 0);
            }

            int cost = Mathf.Max(0, card.CurrentCost);
            if (CurrentFood >= cost)
            {
                CurrentFood -= cost;
                return (true, cost, 0);
            }

            int remainingFood = CurrentFood;
            CurrentFood = 0;

            if (userCurrentHealth <= 0 || cost <= 0)
            {
                return (false, remainingFood, 0);
            }

            float penaltyRatio = Mathf.Clamp01(cost * healthPenaltyPerCostStep);
            int healthPenalty = Mathf.Max(1, Mathf.FloorToInt(userCurrentHealth * penaltyRatio));
            return (false, remainingFood, healthPenalty);
        }
    }
}
