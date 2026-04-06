namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class CardModifierSystem
    {
        private static readonly Dictionary<CardType, float> typeMultipliers = new();

        public static void SetTypeMultiplier(CardType type, float multiplier)
        {
            typeMultipliers[type] = multiplier;
            Debug.Log($"[CardModifierSystem] {type} multiplier set to {multiplier}");
        }

        public static void ClearTypeMultiplier(CardType type)
        {
            typeMultipliers.Remove(type);
            Debug.Log($"[CardModifierSystem] {type} multiplier cleared");
        }

        public static void ClearAll()
        {
            typeMultipliers.Clear();
            Debug.Log("[CardModifierSystem] all multipliers cleared");
        }

        public static int Apply(Card sourceCard, int baseValue)
        {
            if (sourceCard == null) return baseValue;

            float multiplier = 1f;
            if (typeMultipliers.TryGetValue(sourceCard._CardType, out float foundMultiplier))
            {
                multiplier = foundMultiplier;
            }

            return Mathf.RoundToInt(baseValue * multiplier);
        }
    }
}
