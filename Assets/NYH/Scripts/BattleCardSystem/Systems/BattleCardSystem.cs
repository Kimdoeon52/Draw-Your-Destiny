namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleCardSystem
     *
     * Owns the runtime battle deck, hand, action point cost rules,
     * and the flow for playing battle cards.
     *
     * Inspector fields:
     * - Fallback Battle Deck Sources: used only when BattleDeckCollection is missing
     * - Battle Cost Rules: action point and health penalty tuning
     *
     * Usage:
     * - Keep one instance in the battle scene.
     * - Call SetupFromInspector() or SetupBattleDeck() before drawing.
     * - Card play starts through PlayCard().
     */
    public class BattleCardSystem : Singleton<BattleCardSystem>
    {
        [Header("Fallback Battle Deck Sources")]
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();
        [SerializeField] private List<BattleCardData> earnedBattleCards = new();

        [Header("Battle Cost Rules")]
        [SerializeField] private int maxActionPoints = 15;
        [SerializeField, Range(0f, 1f)] private float healthPenaltyPerCostStep = 0.1f;

        private BattleCardPileState pileState;
        private BattlePlayPerformer playPerformer;
        private BattleTacticalPerformer tacticalPerformer;

        public BattleCardPileState PileState => pileState;
        public int CurrentActionPoints { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            pileState = new BattleCardPileState();
            playPerformer = new BattlePlayPerformer(pileState, ResolveCardCost);
            tacticalPerformer = new BattleTacticalPerformer();

            Debug.Log($"[BattleCardSystem] Awake ?袁⑥┷: scene={gameObject.scene.name}, fallbackBaseDeck={baseBattleDeck.Count}, fallbackEarned={earnedBattleCards.Count}, hasBattleDeckCollection={(BattleDeckCollection.Instance != null)}");

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
            Debug.Log($"[BattleCardSystem] SetupBattleDeck ?袁⑥┷: mergedDeck={mergedDeck.Count}, drawPile={pileState.DrawPileCount}, hand={pileState.HandCount}, discard={pileState.DiscardPileCount}");
        }

        public void SetupFromInspector()
        {
            if (BattleDeckCollection.Instance != null)
            {
                Debug.Log($"[BattleCardSystem] SetupFromInspector: BattleDeckCollection ???? baseDeck={BattleDeckCollection.Instance.BaseBattleDeck.Count}, earned={BattleDeckCollection.Instance.EarnedBattleCards.Count}");
                if (BattleDeckCollection.Instance.BaseBattleDeck.Count == 0 && baseBattleDeck.Count > 0)
                {
                    Debug.LogWarning($"[BattleCardSystem] BattleDeckCollection 疫꿸퀡???源놁뵠 ??쑴堉???됰선 fallback 疫꿸퀡????{baseBattleDeck.Count}?關??癰귣벊沅??몃빍??");
                    BattleDeckCollection.Instance.ConfigureBaseDeck(baseBattleDeck);
                }

                SetupBattleDeck(
                    BattleDeckCollection.Instance.BaseBattleDeck,
                    BattleDeckCollection.Instance.EarnedBattleCards);
            }
            else
            {
                Debug.LogWarning($"[BattleCardSystem] SetupFromInspector: BattleDeckCollection ??곸벉, fallback ????baseDeck={baseBattleDeck.Count}, earned={earnedBattleCards.Count}");
                SetupBattleDeck(baseBattleDeck, earnedBattleCards);
            }

            SetupActionPoints(0);
            Debug.Log($"[BattleCardSystem] ??곕짗???λ뜃由???袁⑥┷: currentActionPoints={CurrentActionPoints}");
        }

        public void SetupActionPoints(int actionPoints)
        {
            CurrentActionPoints = Mathf.Clamp(actionPoints, 0, maxActionPoints);
        }

        public void AddActionPoints(int amount)
        {
            CurrentActionPoints = Mathf.Clamp(CurrentActionPoints + amount, 0, maxActionPoints);
        }

        public int GainTurnActionPoints(int turnNumber)
        {
            int gainAmount = Mathf.Max(0, turnNumber);
            int before = CurrentActionPoints;
            AddActionPoints(gainAmount);
            return CurrentActionPoints - before;
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

        public void ShowDeck()
        {
            if (pileState.DrawPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(
                ConvertToPreviewCards(pileState.GetShuffledDrawPileCopy()),
                "?袁る떮 ???類ㅼ뵥");
        }

        public void ShowDiscardPile()
        {
            if (pileState.DiscardPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(
                ConvertToPreviewCards(pileState.GetShuffledDiscardPileCopy()),
                "?袁る떮 甕곌쑬???遺? ?類ㅼ뵥");
        }

        public void PlayCard(
            BattleCard card,
            BattleUnit userUnit,
            Vector2Int targetPosition,
            IReadOnlyList<Vector2Int> plannedPath,
            BattleUnit targetUnit = null,
            bool skipFollowUpAttack = false,
            bool skipPostAttackMove = false,
            System.Action onFinished = null)
        {
            int currentHealth = userUnit != null ? userUnit.CurrentHealth : 0;
            int unitSpeed = userUnit != null ? userUnit.CurrentSpeed : 0;
            ActionSystem.Instance.Perform(
                new BattlePlayCardGA(card, userUnit, targetUnit, targetPosition, plannedPath, currentHealth, unitSpeed, skipFollowUpAttack, skipPostAttackMove),
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

        private (bool paidByActionPoints, int actionPointsSpent, int healthPenalty) ResolveCardCost(BattleCard card, int userCurrentHealth)
        {
            if (card == null)
            {
                return (false, 0, 0);
            }

            int cost = Mathf.Max(0, card.CurrentCost);
            if (CurrentActionPoints >= cost)
            {
                CurrentActionPoints -= cost;
                return (true, cost, 0);
            }

            int remainingActionPoints = CurrentActionPoints;
            CurrentActionPoints = 0;

            if (userCurrentHealth <= 0 || cost <= 0)
            {
                return (false, remainingActionPoints, 0);
            }

            float penaltyRatio = Mathf.Clamp01(cost * healthPenaltyPerCostStep);
            int healthPenalty = Mathf.Max(1, Mathf.FloorToInt(userCurrentHealth * penaltyRatio));
            return (false, remainingActionPoints, healthPenalty);
        }

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
