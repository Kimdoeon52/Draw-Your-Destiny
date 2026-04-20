namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

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

            if (!CanPlayCard(playCardGA))
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

            BattleCardTargetingMode targetingMode = BattleCardTargetingUtility.ResolveTargetingMode(playCardGA.Card);
            if (targetingMode == BattleCardTargetingMode.MoveThenAttack)
            {
                // 이동 후 공격 카드는 단일 액션처럼 보이더라도
                // 내부적으로는 이동 GA에 공격 GA를 반응으로 연결해 순서를 보장합니다.
                QueueMoveThenAttack(playCardGA);
                return;
            }

            if (targetingMode == BattleCardTargetingMode.AttackThenMove)
            {
                // 공격 후 이동 카드도 순서를 보장하기 위해
                // 공격 GA 뒤에 이동 GA를 연결하는 방식으로 처리합니다.
                QueueAttackThenMove(playCardGA);
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

                GameAction rootAttackAction = BuildAttackReactionChain(playCardGA, attackEffect);
                if (rootAttackAction != null)
                {
                    ActionSystem.Instance.AddReaction(rootAttackAction);
                }
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

        private static void QueueMoveThenAttack(BattlePlayCardGA playCardGA)
        {
            if (playCardGA?.Card == null || playCardGA.UserUnit == null)
            {
                return;
            }

            BattleMoveEffect moveEffect = BattleEffectResolver.GetMoveEffect(playCardGA.Card);
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(playCardGA.Card);

            if (moveEffect == null || attackEffect == null)
            {
                Debug.LogWarning($"[BattleCardSystem] 이동 후 공격 구성이 불완전합니다: {playCardGA.Card.Title}");
                ApplyDirectBattleEffects(playCardGA);
                return;
            }

            BattleMoveGA moveGA = new(
                playCardGA.Card,
                playCardGA.UserUnit,
                // 하이브리드 카드에서는 클릭한 공격 칸이 아니라
                // 실제 이동 경로의 마지막 칸이 이동 목적지입니다.
                playCardGA.PlannedPath != null && playCardGA.PlannedPath.Count > 0
                    ? playCardGA.PlannedPath[playCardGA.PlannedPath.Count - 1]
                    : playCardGA.UserUnit.GridPosition,
                playCardGA.PlannedPath,
                moveEffect.Amount,
                moveEffect.IncludeSourceUnitSpeed ? playCardGA.UserUnitSpeed : 0);

            if (playCardGA.SkipFollowUpAttack)
            {
                // 후속 공격 대상이 없을 때는 이동만 처리하고 카드를 종료합니다.
                ActionSystem.Instance.AddReaction(moveGA);
                return;
            }

            GameAction rootAttackAction = BuildAttackReactionChain(playCardGA, attackEffect);
            if (rootAttackAction != null)
            {
                moveGA.PerformReactions.Add(rootAttackAction);
            }

            ActionSystem.Instance.AddReaction(moveGA);
        }

        private static void QueueAttackThenMove(BattlePlayCardGA playCardGA)
        {
            if (playCardGA?.Card == null || playCardGA.UserUnit == null)
            {
                return;
            }

            BattleMoveEffect moveEffect = BattleEffectResolver.GetMoveEffect(playCardGA.Card);
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(playCardGA.Card);

            if (moveEffect == null || attackEffect == null)
            {
                Debug.LogWarning($"[BattleCardSystem] 공격 후 이동 구성이 불완전합니다: {playCardGA.Card.Title}");
                ApplyDirectBattleEffects(playCardGA);
                return;
            }

            GameAction rootAttackAction = BuildAttackReactionChain(playCardGA, attackEffect);
            if (rootAttackAction == null)
            {
                return;
            }

            if (playCardGA.SkipPostAttackMove || playCardGA.PlannedPath == null || playCardGA.PlannedPath.Count == 0)
            {
                // 이동 경로를 고르지 않았거나 이동을 생략한 경우에는 공격만 처리합니다.
                ActionSystem.Instance.AddReaction(rootAttackAction);
                return;
            }

            BattleMoveGA moveGA = new(
                playCardGA.Card,
                playCardGA.UserUnit,
                playCardGA.PlannedPath[playCardGA.PlannedPath.Count - 1],
                playCardGA.PlannedPath,
                moveEffect.Amount,
                moveEffect.IncludeSourceUnitSpeed ? playCardGA.UserUnitSpeed : 0);

            AttachReactionToChain(rootAttackAction, moveGA);
            ActionSystem.Instance.AddReaction(rootAttackAction);
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
                    BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(playCardGA.Card);
                    foreach (Vector2Int targetPosition in EnumerateAttackTargetPositions(playCardGA))
                    {
                        BattleAttackGA previewAttack = CreateAttackGA(playCardGA, attackEffect, targetPosition);
                        foreach (BattleUnit unit in boardSystem.GetUnitsInAttackArea(
                            playCardGA.UserUnit,
                            targetPosition,
                            previewAttack))
                        {
                            if (unit != null && !resolvedTargets.Contains(unit))
                            {
                                resolvedTargets.Add(unit);
                            }
                        }
                    }
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

        private static IEnumerable<Vector2Int> EnumerateAttackTargetPositions(BattlePlayCardGA playCardGA)
        {
            if (playCardGA?.AttackTargetPositions != null && playCardGA.AttackTargetPositions.Count > 0)
            {
                for (int i = 0; i < playCardGA.AttackTargetPositions.Count; i++)
                {
                    yield return playCardGA.AttackTargetPositions[i];
                }

                yield break;
            }

            if (playCardGA != null)
            {
                yield return playCardGA.TargetPosition;
            }
        }

        private static BattleAttackGA CreateAttackGA(
            BattlePlayCardGA playCardGA,
            BattleAttackEffect attackEffect,
            Vector2Int targetPosition)
        {
            BattleUnit resolvedTargetUnit = null;
            if (BattleBoardSystem.Instance != null)
            {
                BattleUnit unitAtTarget = BattleBoardSystem.Instance.GetUnitAt(targetPosition);
                if (unitAtTarget != null && playCardGA?.UserUnit != null && unitAtTarget.Team != playCardGA.UserUnit.Team)
                {
                    resolvedTargetUnit = unitAtTarget;
                }
            }

            if (resolvedTargetUnit == null
                && playCardGA?.TargetUnit != null
                && playCardGA.TargetPosition == targetPosition)
            {
                resolvedTargetUnit = playCardGA.TargetUnit;
            }

            return new BattleAttackGA(
                playCardGA.Card,
                playCardGA.UserUnit,
                resolvedTargetUnit,
                targetPosition,
                0,
                attackEffect != null ? attackEffect.ImpactRange : 1,
                attackEffect != null ? attackEffect.TargetCount : 1,
                attackEffect != null && attackEffect.HitsAllTargetsInRange,
                attackEffect != null ? attackEffect.ImpactPattern : BattleAttackPattern.None,
                attackEffect != null ? attackEffect.CustomImpactPattern : null);
        }

        private static GameAction BuildAttackReactionChain(BattlePlayCardGA playCardGA, BattleAttackEffect attackEffect)
        {
            GameAction rootAction = null;
            GameAction currentAction = null;
            foreach (Vector2Int targetPosition in EnumerateAttackTargetPositions(playCardGA))
            {
                BattleAttackGA attackGA = CreateAttackGA(playCardGA, attackEffect, targetPosition);
                if (rootAction == null)
                {
                    rootAction = attackGA;
                    currentAction = attackGA;
                    continue;
                }

                currentAction.PerformReactions.Add(attackGA);
                currentAction = attackGA;
            }

            return rootAction;
        }

        private static void AttachReactionToChain(GameAction rootAction, GameAction tailReaction)
        {
            if (rootAction == null || tailReaction == null)
            {
                return;
            }

            GameAction current = rootAction;
            while (current.PerformReactions.Count > 0)
            {
                current = current.PerformReactions[current.PerformReactions.Count - 1];
            }

            current.PerformReactions.Add(tailReaction);
        }
    }
}
