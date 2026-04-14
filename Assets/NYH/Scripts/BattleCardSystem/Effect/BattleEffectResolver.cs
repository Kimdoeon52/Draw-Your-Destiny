namespace NYH.BattleCardSystem
{
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
    }
}
