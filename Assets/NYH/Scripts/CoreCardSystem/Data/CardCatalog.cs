namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // ============================================================
    // CardCatalog — 게임에 존재하는 모든 카드 종류를 보관하는 카탈로그
    //
    // 용도:
    //   - 카드 3택 보상 시스템에서 랜덤 카드 선택
    //   - 카드 ID로 CardData 빠른 조회
    //
    // 사용법:
    //   인스펙터에서 allCards 리스트에 CardData SO를 등록해두면 됨
    //   CardCatalog.Instance.GetRandom(3)     → 랜덤 3장 CardData 반환
    //   CardCatalog.Instance.GetByID(id)      → ID로 조회
    //   CardCatalog.Instance.GetByType(type)  → 타입별 필터링
    // ============================================================
    public class CardCatalog : Singleton<CardCatalog>
    {
        [Header("전체 카드 목록 (인스펙터에서 CardData SO 등록)")]
        [SerializeField] private List<CardData> allCards = new();

        // cardID → CardData 빠른 조회용 딕셔너리 (Awake에서 빌드)
        private Dictionary<int, CardData> idMap = new();

        protected override void Awake()
        {
            base.Awake();
            BuildIDMap();
        }

        // allCards 리스트를 순회해 idMap 구성
        private void BuildIDMap()
        {
            idMap.Clear();
            foreach (var card in allCards)
            {
                if (card == null) continue;
                if (idMap.ContainsKey(card.cardID))
                {
                    Debug.LogWarning($"[CardCatalog] cardID 중복: {card.cardID} ({card.name})");
                    continue;
                }
                idMap[card.cardID] = card;
            }
            Debug.Log($"[CardCatalog] 카드 {idMap.Count}종 등록 완료");
        }

        // ── 조회 API ──────────────────────────────────────────────

        // 전체 카드 목록 반환
        public IReadOnlyList<CardData> GetAll() => allCards;

        // cardID로 조회 — 없으면 null 반환
        public CardData GetByID(int id)
        {
            idMap.TryGetValue(id, out CardData card);
            return card;
        }

        // 특정 타입 카드만 필터링해서 반환
        public List<CardData> GetByType(CardType type)
        {
            List<CardData> result = new();
            foreach (var card in allCards)
            {
                if (card != null && card.cardType == type)
                    result.Add(card);
            }
            return result;
        }

        // 카탈로그에서 랜덤 N장 CardData 반환 (중복 없음)
        // 카드 3택 보상 시스템에서 사용
        public List<CardData> GetRandom(int amount)
        {
            List<CardData> pool = new(allCards);
            pool.Shuffle();

            int count = Mathf.Min(amount, pool.Count);
            return pool.GetRange(0, count);
        }

        // 특정 타입에서 랜덤 N장 반환
        public List<CardData> GetRandomByType(CardType type, int amount)
        {
            List<CardData> pool = GetByType(type);
            pool.Shuffle();

            int count = Mathf.Min(amount, pool.Count);
            return pool.GetRange(0, count);
        }
    }
}
