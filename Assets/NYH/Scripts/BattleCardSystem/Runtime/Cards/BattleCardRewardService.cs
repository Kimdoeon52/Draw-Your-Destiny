namespace NYH.BattleCardSystem
{
    /// <summary>
    /// Applies battle rewards to the persistent deck authority.
    /// Permanent battle deck mutations should go through this service instead of runtime pile state.
    /// </summary>
    internal sealed class BattleCardRewardService
    {
        /// <summary>
        /// Adds a battle reward card, optionally replacing an existing deck card immediately.
        /// </summary>
        public BattleDeckAddResult AddEarnedBattleCard(BattleCardData data, BattleCard replaceTarget = null)
        {
            BattleDeckCollection deckCollection = BattleDeckCollection.GetOrCreate();
            BattleCardData replaceTargetData = replaceTarget != null ? replaceTarget.Data : null;
            return deckCollection.AddBattleRewardCard(data, replaceTargetData);
        }

        /// <summary>
        /// Adds a potion or other deck-limit-ignoring battle card.
        /// </summary>
        public void AddPotionCard(BattleCardData potionData)
        {
            BattleDeckCollection.GetOrCreate().AddPotionCard(potionData);
        }

        /// <summary>
        /// Permanently removes one saved copy of a consumable battle card after it is played.
        /// </summary>
        public bool ConsumePersistentBattleCard(BattleCard card)
        {
            return card?.Data != null && BattleDeckCollection.GetOrCreate().RemoveSinglePersistedCard(card.Data);
        }
    }
}
