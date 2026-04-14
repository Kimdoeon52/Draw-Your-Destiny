namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
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

            if (!CanPlayCard(playCardGA))
            {
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

            Debug.Log(
                $"[BattleCardSystem] 카드 사용: {playCardGA.Card.Title}, actionPointsSpent={playCardGA.ActionPointsSpent}, hpPenalty={playCardGA.HealthPenaltyAmount}");

            QueueBattleCardAction(playCardGA);

            yield return null;
        }

        private static bool CanPlayCard(BattlePlayCardGA playCardGA)
        {
            if (playCardGA == null || playCardGA.Card == null)
            {
                return false;
            }

            BattleUnit userUnit = playCardGA.UserUnit;
            if (userUnit == null)
            {
                return true;
            }

            if (!userUnit.IsAlive)
            {
                Debug.LogWarning("[BattleCardSystem] 사망한 유닛은 카드를 사용할 수 없습니다.");
                return false;
            }

            bool requiresAttackCapability = playCardGA.Card.CardType == BattleCardType.Attack
                || HasEffect<BattleDamageEffect>(playCardGA.Card);
            bool requiresMoveCapability = playCardGA.Card.CardType == BattleCardType.Move
                || HasEffect<BattleMoveEffect>(playCardGA.Card);

            if (requiresAttackCapability)
            {
                if (userUnit.IsStunned)
                {
                    Debug.LogWarning("[BattleCardSystem] 기절 상태라 공격 카드를 사용할 수 없습니다.");
                    return false;
                }

                if (userUnit.IsDisarmed)
                {
                    Debug.LogWarning("[BattleCardSystem] 무장해제 상태라 공격 카드를 사용할 수 없습니다.");
                    return false;
                }
            }

            if (requiresMoveCapability && userUnit.IsStunned)
            {
                Debug.LogWarning("[BattleCardSystem] 기절 상태라 이동 카드를 사용할 수 없습니다.");
                return false;
            }

            return true;
        }

        private void QueueBattleCardAction(BattlePlayCardGA playCardGA)
        {
            if (playCardGA.Card == null)
            {
                return;
            }

            if (HasBattleEffects(playCardGA.Card))
            {
                ApplyDirectBattleEffects(playCardGA);
                return;
            }

            if (playCardGA.Card.CardType == BattleCardType.Attack)
            {
                BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(playCardGA.Card);
                if (attackEffect == null)
                {
                    Debug.LogWarning($"[BattleCardSystem] 공격 카드에 BattleAttackEffect가 없습니다: {playCardGA.Card.Title}");
                    return;
                }

                ActionSystem.Instance.AddReaction(
                    new BattleAttackGA(
                        playCardGA.Card,
                        playCardGA.UserUnit,
                        playCardGA.TargetUnit,
                        playCardGA.TargetPosition,
                        0,
                        attackEffect.Range,
                        attackEffect.TargetCount,
                        attackEffect.HitsAllTargetsInRange,
                        attackEffect.AttackPattern,
                        attackEffect.CustomAttackPattern));
                return;
            }

            if (playCardGA.Card.CardType == BattleCardType.Move)
            {
                ActionSystem.Instance.AddReaction(
                    new BattleMoveGA(
                        playCardGA.Card,
                        playCardGA.UserUnit,
                        playCardGA.TargetPosition,
                        playCardGA.PlannedPath,
                        ResolveLegacyMoveAmount(playCardGA.Card),
                        playCardGA.UserUnitSpeed));
                return;
            }

            ApplyDirectBattleEffects(playCardGA);
        }

        private static void ApplyDirectBattleEffects(BattlePlayCardGA playCardGA)
        {
            if (playCardGA?.Card?.RuntimeEffects == null)
            {
                return;
            }

            List<BattleUnit> resolvedTargets = ResolveEffectTargets(playCardGA);

            BattleEffectContext context = new(
                playCardGA.Card,
                playCardGA.UserUnit,
                playCardGA.TargetUnit,
                playCardGA.TargetPosition,
                playCardGA.PlannedPath,
                BattleBoardSystem.Instance,
                BattleCardSystem.Instance);

            foreach (var effect in playCardGA.Card.RuntimeEffects)
            {
                if (effect is not BattleEffect battleEffect)
                {
                    continue;
                }

                battleEffect.Apply(context, resolvedTargets);
            }
        }

        private static List<BattleUnit> ResolveEffectTargets(BattlePlayCardGA playCardGA)
        {
            List<BattleUnit> resolvedTargets = new();
            if (playCardGA?.Card == null)
            {
                return resolvedTargets;
            }

            if (ShouldResolveAttackArea(playCardGA))
            {
                BattleBoardSystem boardSystem = BattleBoardSystem.Instance;
                if (boardSystem != null && playCardGA.UserUnit != null)
                {
                    BattleAttackGA previewAttack = new(
                        playCardGA.Card,
                        playCardGA.UserUnit,
                        playCardGA.TargetUnit,
                        playCardGA.TargetPosition,
                        0,
                        BattleEffectResolver.GetAttackEffect(playCardGA.Card)?.Range ?? 1,
                        BattleEffectResolver.GetAttackEffect(playCardGA.Card)?.TargetCount ?? 1,
                        BattleEffectResolver.GetAttackEffect(playCardGA.Card)?.HitsAllTargetsInRange ?? false,
                        BattleEffectResolver.GetAttackEffect(playCardGA.Card)?.AttackPattern ?? BattleAttackPattern.None,
                        BattleEffectResolver.GetAttackEffect(playCardGA.Card)?.CustomAttackPattern);

                    resolvedTargets.AddRange(
                        boardSystem.GetUnitsInAttackArea(
                            playCardGA.UserUnit,
                            playCardGA.TargetPosition,
                            previewAttack));
                }

                return resolvedTargets;
            }

            if (playCardGA.TargetUnit != null)
            {
                resolvedTargets.Add(playCardGA.TargetUnit);
            }

            return resolvedTargets;
        }

        private static bool ShouldResolveAttackArea(BattlePlayCardGA playCardGA)
        {
            return playCardGA?.Card != null
                && (playCardGA.Card.CardType == BattleCardType.Attack || HasEffect<BattleDamageEffect>(playCardGA.Card));
        }

        private static bool HasBattleEffects(BattleCard card)
        {
            if (card?.RuntimeEffects == null)
            {
                return false;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleEffect)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasEffect<TEffect>(BattleCard card)
            where TEffect : BattleEffect
        {
            if (card?.RuntimeEffects == null)
            {
                return false;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is TEffect)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ResolveLegacyMoveAmount(BattleCard card)
        {
            return BattleEffectResolver.GetMoveEffect(card)?.Amount ?? 0;
        }
    }
}
