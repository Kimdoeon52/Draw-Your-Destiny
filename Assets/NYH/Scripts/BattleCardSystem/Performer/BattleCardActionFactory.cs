namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 전투 카드 한 장이 만들어야 할 GameAction 체인을 조립합니다.
    /// 카드 사용 가능 여부와 비용 처리는 담당하지 않고, Move/Attack/Hybrid 실행 순서만 담당합니다.
    /// </summary>
    internal static class BattleCardActionFactory
    {
        public static void Queue(BattlePlayCardGA playCardGA)
        {
            if (playCardGA?.Card == null)
            {
                return;
            }

            BattleCardTargetingMode targetingMode = BattleCardTargetingUtility.ResolveTargetingMode(playCardGA.Card);
            if (targetingMode == BattleCardTargetingMode.MoveThenAttack)
            {
                QueueMoveThenAttack(playCardGA);
                return;
            }

            if (targetingMode == BattleCardTargetingMode.AttackThenMove)
            {
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
                playCardGA.PlannedPath != null && playCardGA.PlannedPath.Count > 0
                    ? playCardGA.PlannedPath[playCardGA.PlannedPath.Count - 1]
                    : playCardGA.UserUnit.GridPosition,
                playCardGA.PlannedPath,
                moveEffect.Amount,
                moveEffect.IncludeSourceUnitSpeed ? playCardGA.UserUnitSpeed : 0);

            if (playCardGA.SkipFollowUpAttack)
            {
                // 이동 후 공격 카드라도 확정된 공격 대상이 없으면 이동만 실행합니다.
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
                // 이동을 고르지 않았거나 이동 가능한 칸이 없으면 공격만 실행합니다.
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

            List<BattleUnit> resolvedTargets = BattleEffectTargetResolver.Resolve(playCardGA);

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

        private static int ResolveLegacyMoveAmount(BattleCard card)
        {
            return BattleEffectResolver.GetMoveEffect(card)?.Amount ?? 0;
        }

        internal static IEnumerable<Vector2Int> EnumerateAttackTargetPositions(BattlePlayCardGA playCardGA)
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

        internal static BattleAttackGA CreateAttackGA(
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
                attackEffect != null && attackEffect.BlocksBehindTargets,
                attackEffect != null ? attackEffect.ImpactPattern : BattleAttackPattern.None,
                attackEffect != null ? attackEffect.CustomImpactPattern : null,
                attackEffect != null ? attackEffect.PatternOriginMode : BattleAttackPatternOriginMode.RangedPattern,
                attackEffect != null ? attackEffect.ImpactTargetFilter : BattleUnitTargetFilter.EnemiesOnly);
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
