namespace NYH.CoreCardSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class CardModifierSystem
    {
        private class TimedTypeMultiplier
        {
            public CardType Type;
            public float Multiplier;
            public int RemainingTurns;
        }

        private class TimedBlockedType
        {
            public CardType Type;
            public int RemainingTurns;
        }

        private static readonly List<TimedTypeMultiplier> timedMultipliers = new();
        private static readonly List<TimedBlockedType> blockedTypes = new();

        public static event Action OnModifiersChanged;

        public static void SetTypeMultiplier(CardType type, float multiplier, int durationTurns = -1)
        {
            ClearTypeMultiplier(type);
            timedMultipliers.Add(new TimedTypeMultiplier
            {
                Type = type,
                Multiplier = multiplier,
                RemainingTurns = durationTurns
            });

            Debug.Log($"[CardModifierSystem] {type} multiplier set to {multiplier}, duration={durationTurns}");
            OnModifiersChanged?.Invoke();
        }

        public static void ClearTypeMultiplier(CardType type)
        {
            bool removed = timedMultipliers.RemoveAll(m => m.Type == type) > 0;
            if (removed)
            {
                Debug.Log($"[CardModifierSystem] {type} multiplier cleared");
                OnModifiersChanged?.Invoke();
            }
        }

        public static void ClearAll()
        {
            if (timedMultipliers.Count == 0 && blockedTypes.Count == 0) return;

            timedMultipliers.Clear();
            blockedTypes.Clear();
            Debug.Log("[CardModifierSystem] all modifiers cleared");
            OnModifiersChanged?.Invoke();
        }

        public static void OnTurnEnd()
        {
            bool changed = false;

            for (int i = timedMultipliers.Count - 1; i >= 0; i--)
            {
                if (timedMultipliers[i].RemainingTurns < 0) continue;

                timedMultipliers[i].RemainingTurns--;
                if (timedMultipliers[i].RemainingTurns <= 0)
                {
                    Debug.Log($"[CardModifierSystem] {timedMultipliers[i].Type} multiplier expired");
                    timedMultipliers.RemoveAt(i);
                    changed = true;
                }
            }

            for (int i = blockedTypes.Count - 1; i >= 0; i--)
            {
                if (blockedTypes[i].RemainingTurns < 0) continue;

                blockedTypes[i].RemainingTurns--;
                if (blockedTypes[i].RemainingTurns <= 0)
                {
                    Debug.Log($"[CardModifierSystem] {blockedTypes[i].Type} block expired");
                    blockedTypes.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                OnModifiersChanged?.Invoke();
            }
        }

        public static int Apply(Card sourceCard, int baseValue)
        {
            if (sourceCard == null) return baseValue;
            return Mathf.RoundToInt(baseValue * GetMultiplier(sourceCard._CardType));
        }

        public static float Apply(Card sourceCard, float baseValue)
        {
            if (sourceCard == null) return baseValue;
            return baseValue * GetMultiplier(sourceCard._CardType);
        }

        public static float GetMultiplier(CardType type)
        {
            float multiplier = 1f;
            for (int i = 0; i < timedMultipliers.Count; i++)
            {
                if (timedMultipliers[i].Type == type)
                {
                    multiplier *= timedMultipliers[i].Multiplier;
                }
            }

            return multiplier;
        }

        public static void BlockType(CardType type, int durationTurns = -1)
        {
            blockedTypes.RemoveAll(x => x.Type == type);

            blockedTypes.Add(new TimedBlockedType
            {
                Type = type,
                RemainingTurns = durationTurns
            });

            Debug.Log($"[CardModifierSystem] {type} type blocked, duration={durationTurns}");
            OnModifiersChanged?.Invoke();
        }

        public static bool IsTypeBlocked(CardType type)
        {
            foreach (var blocked in blockedTypes)
            {
                if (blocked.Type == type)
                    return true;
            }

            return false;
        }

        // 한턴동안 드로우하지 못하도록 막는 부분
        public static bool IsDrawLocked = false;
        public static void DrawLock()
        {
            IsDrawLocked = true;
        }
    }
}
