namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    // 전투 중 사용하는 카드 덱/손패/AP/카드 실행 흐름의 외부 진입점입니다.
    public class BattleCardSystem : Singleton<BattleCardSystem>
    {
        [Header("Fallback Battle Deck Sources")]
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();
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

        protected override void Awake()
        {
            base.Awake();

            pileState = new BattleCardPileState();
            costService = new BattleCardCostService(maxActionPoints);
            playPerformer = new BattlePlayPerformer(pileState, CanAffordCardCost, ResolveCardCost);
            tacticalPerformer = new BattleTacticalPerformer();

            deckSetupService = new BattleDeckSetupService(pileState);
            rewardService = new BattleCardRewardService(pileState, earnedBattleCards);
            handDrawService = new BattleHandDrawService(pileState);
            pileViewService = new BattleCardPileViewService(pileState);

            BattleCardActionRegistrar.RegisterAll(this);
        }

        // 기본 전투 덱과 보상 전투 카드를 합쳐 현재 전투 draw pile을 구성합니다.
        public void SetupBattleDeck(IEnumerable<BattleCardData> baseDeck, IEnumerable<BattleCardData> earnedCards)
        {
            deckSetupService.SetupBattleDeck(baseDeck, earnedCards);
        }

        // BattleDeckCollection이 있으면 그 덱을, 없으면 인스펙터 fallback 덱을 사용해 전투를 준비합니다.
        public void SetupFromInspector()
        {
            deckSetupService.SetupFromInspector(baseBattleDeck, earnedBattleCards);
            SetupActionPoints(0);
        }

        // 현재 AP를 최대 AP 범위 안에서 지정 값으로 초기화합니다.
        public void SetupActionPoints(int actionPoints)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            costService.Setup(actionPoints);
        }

        // 현재 AP를 지정량만큼 증감하고 최대 AP 범위 안으로 보정합니다.
        public void AddActionPoints(int amount)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            costService.Add(amount);
        }

        // 플레이어 턴 시작 시 얻는 AP를 계산해 현재 AP에 더합니다.
        public int GainTurnActionPoints(int turnNumber)
        {
            int gainAmount = Mathf.Max(0, /*turnNumber*/ 10); // demo build: fixed 10 AP gain regardless of turn.
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.Add(gainAmount);
        }

        // 전투 보상 카드를 영속 덱 또는 현재 전투 덱에 추가합니다.
        public BattleDeckAddResult AddEarnedBattleCard(BattleCardData data, BattleCard replaceTarget = null)
        {
            return rewardService.AddEarnedBattleCard(data, replaceTarget);
        }

        // 포션처럼 일반 제한과 다르게 즉시 전투 덱에 추가되는 카드를 넣습니다.
        public void AddPotionCard(BattleCardData potionData)
        {
            rewardService.AddPotionCard(potionData);
        }

        // 전투 시작 손패 규칙에 따라 카드를 뽑습니다.
        public List<BattleCard> DrawOpeningHand(int unitTypeCount)
        {
            return handDrawService.DrawOpeningHand(unitTypeCount);
        }

        // 멀리건에서 손패 전체를 덱으로 되돌린 뒤 시작 손패를 다시 뽑습니다.
        public List<BattleCard> MulliganOpeningHand(int unitTypeCount)
        {
            return handDrawService.MulliganOpeningHand(unitTypeCount);
        }

        // 멀리건에서 선택한 카드만 덱으로 되돌리고 같은 수만큼 다시 뽑습니다.
        public BattleMulliganResult MulliganSelectedCards(IReadOnlyList<BattleCard> selectedCards)
        {
            return handDrawService.MulliganSelectedCards(selectedCards);
        }

        // 플레이어 턴 시작 규칙에 따라 카드를 뽑습니다.
        public List<BattleCard> DrawTurnCards(int aliveUnitTypeCount)
        {
            return handDrawService.DrawTurnCards(aliveUnitTypeCount);
        }

        // 턴 종료 시 현재 손패를 모두 버림 더미로 보냅니다.
        public void EndTurnDiscardHand()
        {
            handDrawService.EndTurnDiscardHand();
        }

        // 현재 전투 draw pile을 일반 카드 미리보기 UI로 보여줍니다.
        public void ShowDeck()
        {
            pileViewService.ShowDeck();
        }

        // 현재 전투 discard pile을 일반 카드 미리보기 UI로 보여줍니다.
        public void ShowDiscardPile()
        {
            pileViewService.ShowDiscardPile();
        }

        // 전투 카드 사용 요청을 ActionSystem에 전달합니다.
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

        // ActionSystem에서 받은 전투 카드 관련 GameAction을 적절한 performer로 라우팅합니다.
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

        // 현재 AP로 카드 비용을 지불할 수 있는지 확인합니다.
        private bool CanAffordCardCost(BattleCard card)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.CanAfford(card);
        }

        // 카드 비용을 AP로 지불하고, AP 부족 시 체력 페널티 정보를 계산합니다.
        private (bool paidByActionPoints, int actionPointsSpent, int healthPenalty) ResolveCardCost(BattleCard card, int userCurrentHealth)
        {
            costService ??= new BattleCardCostService(maxActionPoints);
            return costService.TryPay(card);
        }
    }
}
