namespace NYH.BattleCardSystem
{
    /*
     * BattleEffectResolver
     *
     * 역할:
     * - BattleCard가 가진 이펙트 목록에서 특정 전투 이펙트를 빠르게 찾아줍니다.
     * - 타겟팅/프리뷰/카드 실행 코드가 이펙트 리스트를 직접 반복하지 않게 해줍니다.
     */
    public static class BattleEffectResolver
    {
        public static BattleAttackEffect GetAttackEffect(BattleCard card)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleAttackEffect attackEffect)
                {
                    return attackEffect;
                }
            }

            return null;
        }

        public static BattleAttackEffect GetAttackEffect(BattleCard card, BattleUnit sourceUnit)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleAttackEffect attackEffect
                    && attackEffect.CanApplyToSourceUnit(sourceUnit))
                {
                    return attackEffect;
                }
            }

            return null;
        }

        public static BattleMoveEffect GetMoveEffect(BattleCard card)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleMoveEffect moveEffect)
                {
                    return moveEffect;
                }
            }

            return null;
        }

        public static BattleMoveEffect GetMoveEffect(BattleCard card, BattleUnit sourceUnit)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleMoveEffect moveEffect
                    && moveEffect.CanApplyToSourceUnit(sourceUnit))
                {
                    return moveEffect;
                }
            }

            return null;
        }

        public static BattleHealEffect GetHealEffect(BattleCard card)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleHealEffect healEffect)
                {
                    return healEffect;
                }
            }

            return null;
        }

        public static BattleHealEffect GetHealEffect(BattleCard card, BattleUnit sourceUnit)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleHealEffect healEffect
                    && healEffect.CanApplyToSourceUnit(sourceUnit))
                {
                    return healEffect;
                }
            }

            return null;
        }

        public static BattlePotionEffect GetPotionEffect(BattleCard card)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattlePotionEffect potionEffect)
                {
                    return potionEffect;
                }
            }

            return null;
        }

        public static BattleTrapEffect GetTrapEffect(BattleCard card)
        {
            if (card?.RuntimeEffects == null)
            {
                return null;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is BattleTrapEffect trapEffect)
                {
                    return trapEffect;
                }
            }

            return null;
        }
    }
}
