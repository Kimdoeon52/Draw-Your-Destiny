namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// Global catalog of every battle card asset available in the game.
    /// It is used both for reward generation and for resolving saved CardIDs back to assets.
    /// </summary>
    public class BattleCardCatalog : Singleton<BattleCardCatalog>
    {
        [Header("All Battle Card Assets")]
        [SerializeField] private List<BattleCardData> allBattleCards = new();

        private readonly Dictionary<int, BattleCardData> idMap = new();

        /// <summary>
        /// Builds the CardID lookup once the singleton is available.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            BuildIdMap();
        }

        /// <summary>
        /// Returns every registered battle card.
        /// </summary>
        public IReadOnlyList<BattleCardData> GetAll() => allBattleCards;

        /// <summary>
        /// Looks up a battle card by its persistent CardID.
        /// </summary>
        public BattleCardData GetById(int id)
        {
            idMap.TryGetValue(id, out var card);
            return card;
        }

        /// <summary>
        /// Returns a shuffled subset from the full catalog.
        /// </summary>
        public List<BattleCardData> GetRandom(int amount)
        {
            List<BattleCardData> pool = new(allBattleCards);
            pool.Shuffle();
            int count = Mathf.Min(amount, pool.Count);
            return pool.GetRange(0, count);
        }

        /// <summary>
        /// Returns a shuffled subset filtered by battle card type.
        /// </summary>
        public List<BattleCardData> GetRandomByType(BattleCardType type, int amount)
        {
            List<BattleCardData> pool = new();
            foreach (BattleCardData card in allBattleCards)
            {
                if (card != null && card.CardType == type)
                {
                    pool.Add(card);
                }
            }

            pool.Shuffle();
            int count = Mathf.Min(amount, pool.Count);
            return pool.GetRange(0, count);
        }

        /// <summary>
        /// Builds the fast CardID lookup used by save/load.
        /// Duplicate IDs are warned and ignored because persistent data depends on uniqueness.
        /// </summary>
        private void BuildIdMap()
        {
            idMap.Clear();
            foreach (BattleCardData card in allBattleCards)
            {
                if (card == null)
                {
                    continue;
                }

                if (idMap.ContainsKey(card.CardID))
                {
                    Debug.LogWarning($"[BattleCardCatalog] Duplicate CardID detected: id={card.CardID}, card={card.CardName}");
                    continue;
                }

                idMap[card.CardID] = card;
            }
        }
    }
}
