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

            Debug.Log($"[BattleCardSystem] Awake 완료: scene={gameObject.scene.name}, fallbackBaseDeck={baseBattleDeck.Count}, fallbackEarned={earnedBattleCards.Count}, hasBattleDeckCollection={(BattleDeckCollection.Instance != null)}");

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
            Debug.Log($"[BattleCardSystem] SetupBattleDeck 완료: mergedDeck={mergedDeck.Count}, drawPile={pileState.DrawPileCount}, hand={pileState.HandCount}, discard={pileState.DiscardPileCount}");
        }

        public void SetupFromInspector()
        {
            if (BattleDeckCollection.Instance != null)
            {
                Debug.Log($"[BattleCardSystem] SetupFromInspector: BattleDeckCollection 사용, baseDeck={BattleDeckCollection.Instance.BaseBattleDeck.Count}, earned={BattleDeckCollection.Instance.EarnedBattleCards.Count}");
                if (BattleDeckCollection.Instance.BaseBattleDeck.Count == 0 && baseBattleDeck.Count > 0)
                {
                    Debug.LogWarning($"[BattleCardSystem] BattleDeckCollection 기본 덱이 비어 있어 fallback 기본 덱 {baseBattleDeck.Count}장을 복사합니다.");
                    BattleDeckCollection.Instance.ConfigureBaseDeck(baseBattleDeck);
                }

                SetupBattleDeck(
                    BattleDeckCollection.Instance.BaseBattleDeck,
                    BattleDeckCollection.Instance.EarnedBattleCards);
            }
            else
            {
                Debug.LogWarning($"[BattleCardSystem] SetupFromInspector: BattleDeckCollection 없음, fallback 사용 baseDeck={baseBattleDeck.Count}, earned={earnedBattleCards.Count}");
                SetupBattleDeck(baseBattleDeck, earnedBattleCards);
            }

            SetupActionPoints(0);
            Debug.Log($"[BattleCardSystem] 행동력 초기화 완료: currentActionPoints={CurrentActionPoints}");
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
                "전투 덱 확인");
        }

        public void ShowDiscardPile()
        {
            if (pileState.DiscardPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(
                ConvertToPreviewCards(pileState.GetShuffledDiscardPileCopy()),
                "전투 버림 더미 확인");
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
