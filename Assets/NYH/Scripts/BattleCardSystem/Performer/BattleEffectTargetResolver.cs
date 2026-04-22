namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 직접 적용되는 전투 이펙트가 실제로 맞힐 유닛 목록을 해석합니다.
    /// 카드 실행 순서나 비용 처리는 담당하지 않고, 타겟 해석만 담당합니다.
    /// </summary>
    internal static class BattleEffectTargetResolver
    {
        public static List<BattleUnit> Resolve(BattlePlayCardGA playCardGA)
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
                    foreach (Vector2Int targetPosition in BattleCardActionFactory.EnumerateAttackTargetPositions(playCardGA))
                    {
                        BattleAttackGA previewAttack = BattleCardActionFactory.CreateAttackGA(
                            playCardGA,
                            attackEffect,
                            targetPosition);

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

            if (ShouldResolveHealArea(playCardGA))
            {
                ResolveHealAreaTargets(playCardGA, resolvedTargets);
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

        private static bool ShouldResolveHealArea(BattlePlayCardGA playCardGA)
        {
            return playCardGA?.Card != null && HasEffect<BattleHealEffect>(playCardGA.Card);
        }

        private static void ResolveHealAreaTargets(BattlePlayCardGA playCardGA, List<BattleUnit> resolvedTargets)
        {
            BattleBoardSystem boardSystem = BattleBoardSystem.Instance;
            BattleHealEffect healEffect = BattleEffectResolver.GetHealEffect(playCardGA.Card);
            if (boardSystem == null || healEffect == null || playCardGA.UserUnit == null || resolvedTargets == null)
            {
                return;
            }

            foreach (Vector2Int targetPosition in BattleCardActionFactory.EnumerateAttackTargetPositions(playCardGA))
            {
                HashSet<Vector2Int> healCells = BattleAttackImpactCellResolver.ResolveImpactCells(
                    playCardGA.UserUnit.GridPosition,
                    targetPosition,
                    healEffect.Range,
                    healEffect.HealPattern,
                    healEffect.CustomHealPattern,
                    healEffect.HealPatternOriginMode);

                foreach (BattleUnit unit in boardSystem.GetUnitsInCells(
                    playCardGA.UserUnit,
                    healCells,
                    healEffect.HealTargetFilter))
                {
                    if (unit != null && !resolvedTargets.Contains(unit))
                    {
                        resolvedTargets.Add(unit);
                    }
                }
            }
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
    }
}
