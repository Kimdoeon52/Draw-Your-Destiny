namespace NYH.BattleCardSystem
{
    using UnityEngine;

    /// <summary>
    /// 전투 카드가 지금 사용 가능한지만 판단합니다.
    /// 비용 지불, 카드 더미 이동, 실제 액션 생성은 BattlePlayPerformer와 BattleCardActionFactory가 담당합니다.
    /// </summary>
    internal static class BattleCardPlayValidator
    {
        public static bool CanPlay(BattlePlayCardGA playCardGA)
        {
            if (playCardGA == null || playCardGA.Card == null)
            {
                return false;
            }

            BattleUnit userUnit = playCardGA.UserUnit;
            BattlePotionEffect potionEffect = BattleEffectResolver.GetPotionEffect(playCardGA.Card);
            BattleTrapEffect trapEffect = BattleEffectResolver.GetTrapEffect(playCardGA.Card);

            if (potionEffect != null)
            {
                return potionEffect.TargetingType == BattlePotionTargetingType.All
                    || BattleGridCoordinateService.Instance.IsCombatCell(playCardGA.TargetPosition);
            }

            if (trapEffect != null)
            {
                return BattleBoardSystem.Instance != null
                    && BattleTargetingQueryService.IsValidTrapInstallCell(BattleBoardSystem.Instance, playCardGA.TargetPosition);
            }

            if (userUnit == null)
            {
                return false;
            }

            if (!userUnit.IsAlive)
            {
                Debug.LogWarning("[BattleCardSystem] 사망한 유닛은 카드를 사용할 수 없습니다.");
                return false;
            }

            if (!BattleCardUnitTypeRestriction.CanUserUnitPlay(playCardGA.Card, userUnit))
            {
                Debug.LogWarning($"[BattleCardSystem] {userUnit.UnitType} 병종은 이 카드를 사용할 수 없습니다: {playCardGA.Card.Title}");
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
