using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class OngoingEffectSystem : Singleton<OngoingEffectSystem>
{
    private class OngoingEffectEntry
    {
        public Card SourceCard;
        public int StartEffectIndex;
        public int RemainingTurns;
    }

    private readonly List<OngoingEffectEntry> entries = new();

    public void Register(Card sourceCard, int startEffectIndex, int turnAmount)
    {
        entries.Add(new OngoingEffectEntry
        {
            SourceCard = sourceCard,
            StartEffectIndex = startEffectIndex,
            RemainingTurns = turnAmount - 1
        });
    }

    public void OnTurnStartOrEnd()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            if (entry.SourceCard?.Effects != null)
            {
                for (int j = entry.StartEffectIndex; j < entry.SourceCard.Effects.Count; j++)
                {
                    var effect = entry.SourceCard.Effects[j];

                    // AddReaction 대신 Perform을 사용해 보세요.
                    // 이렇게 하면 매 효과마다 ActionSystem 엔진이 체크하고 실행합니다.
                    ActionSystem.Instance.Perform(new PerformEffectGA(entry.SourceCard, effect, j));
                }
            }

            entry.RemainingTurns--;
            if (entry.RemainingTurns <= 0)
                entries.RemoveAt(i);
        }
    }
}