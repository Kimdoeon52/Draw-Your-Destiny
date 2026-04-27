namespace NYH.BattleCardSystem
{
    using System.Collections;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 전투 카드 사용 요청을 처리하는 얇은 실행 조정자입니다.
    /// 이 클래스는 비용/카드 더미 상태를 확정하고, 실제 이동/공격 액션 조립은 BattleCardActionFactory에 넘깁니다.
    /// </summary>
    public class BattlePlayPerformer
    {
        private readonly BattleCardPileState pileState;
        private readonly System.Func<BattleCard, bool> canAffordCost;
        private readonly System.Func<BattleCard, int, (bool paidByActionPoints, int actionPointsSpent, int healthPenalty)> resolveCost;

        public BattlePlayPerformer(
            BattleCardPileState pileState,
            System.Func<BattleCard, bool> canAffordCost,
            System.Func<BattleCard, int, (bool paidByActionPoints, int actionPointsSpent, int healthPenalty)> resolveCost)
        {
            this.pileState = pileState;
            this.canAffordCost = canAffordCost;
            this.resolveCost = resolveCost;
        }

        public bool CanHandle(GameAction action)
        {
            return action is BattlePlayCardGA;
        }

        public IEnumerator Perform(GameAction action)
        {
            if (action is not BattlePlayCardGA playCardGA)
            {
                yield break;
            }

            if (playCardGA.Card == null)
            {
                Debug.LogWarning("[BattleCardSystem] 사용할 전투 카드가 없습니다.");
                yield break;
            }

            if (!pileState.ContainsInHand(playCardGA.Card))
            {
                Debug.LogWarning($"[BattleCardSystem] 손패에 없는 전투 카드를 사용하려고 했습니다: {playCardGA.Card.Title}");
                yield break;
            }

            if (!BattleCardPlayValidator.CanPlay(playCardGA))
            {
                yield break;
            }

            if (canAffordCost != null && !canAffordCost(playCardGA.Card))
            {
                Debug.LogWarning($"[BattleCardSystem] 행동력이 부족해 카드를 사용할 수 없습니다: {playCardGA.Card.Title}, need={Mathf.Max(0, playCardGA.Card.CurrentCost)}");
                yield break;
            }

            var costResult = resolveCost(playCardGA.Card, playCardGA.UserCurrentHealth);

            if (costResult.healthPenalty > 0 && playCardGA.UserUnit != null)
            {
                playCardGA.UserUnit.TakeDamage(costResult.healthPenalty);
            }

            pileState.RemoveFromHand(playCardGA.Card);
            if (playCardGA.Card.IsConsumable)
            {
                pileState.Exhaust(playCardGA.Card);
            }
            else
            {
                pileState.SendToDiscard(playCardGA.Card);
            }

            playCardGA.WasPlayed = true;
            playCardGA.PaidByActionPoints = costResult.paidByActionPoints;
            playCardGA.UsedHealthPenalty = costResult.healthPenalty > 0;
            playCardGA.ActionPointsSpent = costResult.actionPointsSpent;
            playCardGA.HealthPenaltyAmount = costResult.healthPenalty;

            BattleCardActionFactory.Queue(playCardGA);
            yield return null;
        }
    }
}
