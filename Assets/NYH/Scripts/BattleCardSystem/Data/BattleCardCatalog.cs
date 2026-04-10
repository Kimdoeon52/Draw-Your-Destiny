namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleCardCatalog
     *
     * 역할:
     * - 게임 전체에서 사용할 전투 카드 원본 SO 목록을 보관하는 카탈로그입니다.
     * - 보상 후보를 랜덤으로 뽑거나, 타입별 전투 카드 후보를 찾을 때 사용합니다.
     *
     * 인스펙터에서 넣는 것:
     * - All Battle Cards: 게임에서 등장 가능한 전투 카드 SO 전체 목록
     *
     * 사용하는 법:
     * - 문명 씬에 1개만 둡니다.
     * - 보상 선택 시 GetRandom()으로 전투 카드 후보를 뽑습니다.
     * - 실제 덱 저장소가 아니라 '전체 카드 풀'입니다.
     */
    public class BattleCardCatalog : Singleton<BattleCardCatalog>
    {
        [Header("전체 전투 카드 목록 (인스펙터에서 BattleCardData SO 등록)")]
        [SerializeField] private List<BattleCardData> allBattleCards = new();

        private readonly Dictionary<int, BattleCardData> idMap = new();

        protected override void Awake()
        {
            base.Awake();
            BuildIdMap();
        }

        public IReadOnlyList<BattleCardData> GetAll() => allBattleCards;

        public BattleCardData GetById(int id)
        {
            idMap.TryGetValue(id, out var card);
            return card;
        }

        public List<BattleCardData> GetRandom(int amount)
        {
            List<BattleCardData> pool = new(allBattleCards);
            pool.Shuffle();
            int count = Mathf.Min(amount, pool.Count);
            return pool.GetRange(0, count);
        }

        public List<BattleCardData> GetRandomByType(BattleCardType type, int amount)
        {
            List<BattleCardData> pool = new();
            foreach (var card in allBattleCards)
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

        private void BuildIdMap()
        {
            idMap.Clear();
            foreach (var card in allBattleCards)
            {
                if (card == null || idMap.ContainsKey(card.CardID))
                {
                    continue;
                }

                idMap[card.CardID] = card;
            }
        }
    }
}
