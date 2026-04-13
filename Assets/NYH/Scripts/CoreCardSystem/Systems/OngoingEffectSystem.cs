using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

public class OngoingEffectSystem : Singleton<OngoingEffectSystem>
{
    private readonly List<OngoingEffectEntry> entries = new();

    private abstract class OngoingEffectEntry
    {
        public Card SourceCard;
        public int StartEffectIndex;
        public abstract bool ShouldContinue();
        public abstract void Execute();
    }

    private class TurnOngoingEffectEntry : OngoingEffectEntry
    {
        public int RemainingTurns;

        public override bool ShouldContinue()
        {
            return RemainingTurns > 0;
        }

        public override void Execute()
        {
            if (SourceCard?.Effects == null)
                return;

            for (int j = StartEffectIndex; j < SourceCard.Effects.Count; j++)
            {
                var effect = SourceCard.Effects[j];
                ActionSystem.Instance.Perform(new PerformEffectGA(SourceCard, effect, j));
            }

            RemainingTurns--;
        }
    }

    public void Register(Card sourceCard, int startEffectIndex, int turnAmount)
    {
        entries.Add(new TurnOngoingEffectEntry
        {
            SourceCard = sourceCard,
            StartEffectIndex = startEffectIndex,
            RemainingTurns = turnAmount - 1
        });
    }

    public void Register(Card sourceCard, int startEffectIndex, BuildingType targetBuildingType)
    {
        entries.Add(new BuildingOngoingEffectEntry
        {
            SourceCard = sourceCard,
            StartEffectIndex = startEffectIndex,
            TargetBuildingType = targetBuildingType
        });
    }

    public void OnTurnStartOrEnd()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            entry.Execute();

            if (!entry.ShouldContinue())
            {
                entries.RemoveAt(i);
            }
        }
    }


    private class BuildingOngoingEffectEntry : OngoingEffectEntry
    {
        public BuildingType TargetBuildingType;

        public override bool ShouldContinue()
        {
            return TileMapManager.HasBuildingOfType(TargetBuildingType);
        }
        public override void Execute()
        {
            if (SourceCard?.Effects == null) return;

            for (int j = StartEffectIndex; j < SourceCard.Effects.Count; j++)
            {
                var effect = SourceCard.Effects[j];
                ActionSystem.Instance.Perform(new PerformEffectGA(SourceCard, effect, j));
            }
        }
    }
}