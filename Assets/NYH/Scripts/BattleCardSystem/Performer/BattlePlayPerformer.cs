namespace NYH.BattleCardSystem
{
    using System.Collections;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public class BattlePlayPerformer
    {
        private readonly BattleCardPileState pileState;
        private readonly System.Func<BattleCard, int, (bool paidByActionPoints, int actionPointsSpent, int healthPenalty)> resolveCost;

        public BattlePlayPerformer(
            BattleCardPileState pileState,
            System.Func<BattleCard, int, (bool paidByActionPoints, int actionPointsSpent, int healthPenalty)> resolveCost)
        {
            this.pileState = pileState;
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

            var costResult = resolveCost(playCardGA.Card, playCardGA.UserCurrentHealth);

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

            Debug.Log(
                $"[BattleCardSystem] 카드 사용: {playCardGA.Card.Title}, actionPointsSpent={playCardGA.ActionPointsSpent}, hpPenalty={playCardGA.HealthPenaltyAmount}");

            QueueBattleCardAction(playCardGA);

            yield return null;
        }

        private void QueueBattleCardAction(BattlePlayCardGA playCardGA)
        {
            if (playCardGA.Card == null)
            {
                return;
            }

            if (playCardGA.Card.CardType == BattleCardType.Attack)
            {
                ActionSystem.Instance.AddReaction(
                    new BattleAttackGA(
                        playCardGA.Card,
                        playCardGA.UserUnit,
                        playCardGA.TargetUnit,
                        playCardGA.TargetPosition,
                        playCardGA.Card.AttackDamage,
                        playCardGA.Card.AttackRange,
                        playCardGA.Card.AttackTargetCount,
                        playCardGA.Card.HitsAllTargetsInRange,
                        playCardGA.Card.AttackPattern));
            }
            else if (playCardGA.Card.CardType == BattleCardType.Move)
            {
                ActionSystem.Instance.AddReaction(
                    new BattleMoveGA(
                        playCardGA.Card,
                        playCardGA.UserUnit,
                        playCardGA.TargetPosition,
                        playCardGA.Card.MoveAmount,
                        playCardGA.UserUnitSpeed));
            }
        }
    }
}
