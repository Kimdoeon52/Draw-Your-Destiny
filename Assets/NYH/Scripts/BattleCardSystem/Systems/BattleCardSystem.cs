namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// High-level runtime entry point for battle card flow.
    /// It owns pile state, AP handling, draw/mulligan services, and action dispatch.
    /// </summary>
    public class BattleCardSystem : Singleton<BattleCardSystem>
    {
        [Header("Fallback Battle Deck Sources")]
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();
        // Kept for inspector compatibility with older scenes. Persistent reward flow now comes from
        // BattleDeckCollection rather than this serialized list.
        [SerializeField] private List<BattleCardData> earnedBattleCards = new();

        [Header("Battle Cost Rules")]
        [SerializeField] private int maxActionPoints = 15;

        private BattleCardPileState pileState;
        private BattlePlayPerformer playPerformer;
        private BattleTacticalPerformer tacticalPerformer;
        private BattleCardCostService costService;
        private BattleDeckSetupService deckSetupService;
        private BattleCardRewardService rewardService;
        private BattleHandDrawService handDrawService;
        private BattleCardPileViewService pileViewService;

        public BattleCardPileState PileState => pileState;
        public int CurrentActionPoints => costService != null ? costService.CurrentActionPoints : 0;

        /// <summary>
        /// Creates the support services used during battle runtime.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            pileState = new BattleCardPileState();
            costService = new BattleCardCostService(maxActionPoints);
            playPerformer = new BattlePlayPerformer(pileState, CanAffordCardCost, ResolveCardCost);
            tacticalPerformer = new BattleTacticalPerformer();

            deckSetupService = new BattleDeckSetupService(pileState);
            rewardService = new BattleCardRewardService();
            handDrawService = new BattleHandDrawService(pileState);
            pileViewService = new BattleCardPileViewService(pileState);

            BattleCardActionRegistrar.RegisterAll(this);
        }

        /// <summary>
        /// Fallback setup path that directly merges base and earned cards into the runtime pile.
        /// </summary>
        public void SetupBattleDeck(IEnumerable<BattleCardData> baseDeck, IEnumerable<BattleCardData> earnedCards)
        {
            deckSetupService.SetupBattleDeck(baseDeck, earnedCards);
        }

        /// <summary>
        /// Standard setup path used by battle sessions.
        /// Ensures the persistent deck authority exists before building the runtime pile.
        /// </summary>
        public void SetupFromInspector()
        {
            BattleDeckCollection.GetOrCreate();
            IReadOnlyList<BattleCardData> configuredBaseDeck =
                baseBattleDeck != null && baseBattleDeck.Count > 0
                    ? baseBattleDeck
                    : null;

            deckSetupService.SetupFromInspector(configuredBaseDeck);
            SetupActionPoints(0);
        }

        /// <summary>
        /// Sets the current action points through the AP service.
        /// </summary>
        public void SetupActionPoints(int actionPoints)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            costService.Setup(actionPoints);
        }

        /// <summary>
        /// Adds or removes action points and clamps through the AP service rules.
        /// </summary>
        public void AddActionPoints(int amount)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            costService.Add(amount);
        }

        /// <summary>
        /// Applies the start-of-turn AP gain rule and returns the updated AP total.
        /// </summary>
        public int GainTurnActionPoints(int turnNumber)
        {
            int gainAmount = Mathf.Max(0, /*turnNumber*/ 10); // Demo build: fixed AP gain regardless of turn.
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.Add(gainAmount);
        }

        /// <summary>
        /// Adds a persistent battle reward card, optionally replacing an existing card immediately.
        /// </summary>
        public BattleDeckAddResult AddEarnedBattleCard(BattleCardData data, BattleCard replaceTarget = null)
        {
            return rewardService.AddEarnedBattleCard(data, replaceTarget);
        }

        /// <summary>
        /// Adds a battle card that ignores the normal deck size limit, such as a potion.
        /// </summary>
        public void AddPotionCard(BattleCardData potionData)
        {
            rewardService.AddPotionCard(potionData);
        }

        /// <summary>
        /// Draws the opening hand using the battle opening-hand rules.
        /// </summary>
        public List<BattleCard> DrawOpeningHand(int unitTypeCount)
        {
            return handDrawService.DrawOpeningHand(unitTypeCount);
        }

        /// <summary>
        /// Performs a full opening-hand mulligan.
        /// </summary>
        public List<BattleCard> MulliganOpeningHand(int unitTypeCount)
        {
            return handDrawService.MulliganOpeningHand(unitTypeCount);
        }

        /// <summary>
        /// Replaces only the selected opening-hand cards.
        /// </summary>
        public BattleMulliganResult MulliganSelectedCards(IReadOnlyList<BattleCard> selectedCards)
        {
            return handDrawService.MulliganSelectedCards(selectedCards);
        }

        /// <summary>
        /// Draws cards for the start of a normal turn.
        /// </summary>
        public List<BattleCard> DrawTurnCards(int aliveUnitTypeCount)
        {
            return handDrawService.DrawTurnCards(aliveUnitTypeCount);
        }

        /// <summary>
        /// Discards the full hand at end of turn.
        /// </summary>
        public void EndTurnDiscardHand()
        {
            handDrawService.EndTurnDiscardHand();
        }

        /// <summary>
        /// Opens the draw-pile preview UI.
        /// </summary>
        public void ShowDeck()
        {
            pileViewService.ShowDeck();
        }

        /// <summary>
        /// Opens the discard-pile preview UI.
        /// </summary>
        public void ShowDiscardPile()
        {
            pileViewService.ShowDiscardPile();
        }

        /// <summary>
        /// Converts a battle card play request into a game action handled by ActionSystem.
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
        /// Lets ActionSystem route a game action into the appropriate battle performer.
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
        /// Returns whether the current AP pool can pay the card cost.
        /// </summary>
        private bool CanAffordCardCost(BattleCard card)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.CanAfford(card);
        }

        /// <summary>
        /// Attempts to pay the battle card cost and returns how the payment was resolved.
        /// </summary>
        private (bool paidByActionPoints, int actionPointsSpent, int healthPenalty) ResolveCardCost(BattleCard card, int userCurrentHealth)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.TryPay(card);
        }
    }
}
