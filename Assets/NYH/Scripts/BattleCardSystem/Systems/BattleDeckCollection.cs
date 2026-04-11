namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleDeckCollection
     *
     * 역할:
     * - 문명 씬에서 획득한 전투 카드를 씬이 바뀌어도 계속 보관하는 저장소입니다.
     * - 전투 시작 시 기본 전투 덱 + 이번 런에서 획득한 전투 카드를 합쳐서 실제 전투 덱 재료를 제공합니다.
     *
     * 인스펙터에서 넣는 것:
     * - Base Battle Deck: 전투 시작 시 항상 들고 가는 기본 전투 카드들
     * - Earned Battle Cards: 테스트용으로만 직접 넣고, 실제 게임 중에는 보상 선택으로 자동 누적됩니다
     *
     * 사용하는 법:
     * - 문명 씬에 1개만 둡니다.
     * - 씬 이동 후에도 유지되어야 하므로 DontDestroyOnLoad로 동작합니다.
     * - 보상으로 전투 카드를 얻었을 때 AddBattleRewardCard()를 호출합니다.
     */
    public class BattleDeckCollection : Singleton<BattleDeckCollection>
    {
        [Header("기본 전투 덱 (전투 시작 시 항상 들고 가는 카드들)")]
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();

        [Header("획득한 전투 카드 (문명 진행 중 보상으로 얻은 카드들)")]
        [SerializeField] private List<BattleCardData> earnedBattleCards = new();

        public IReadOnlyList<BattleCardData> BaseBattleDeck => baseBattleDeck;
        public IReadOnlyList<BattleCardData> EarnedBattleCards => earnedBattleCards;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                Debug.LogWarning($"[BattleDeckCollection] 중복 인스턴스 감지로 파괴 예정: scene={gameObject.scene.name}, object={name}");
                return;
            }

            DontDestroyOnLoad(gameObject);
            Debug.Log($"[BattleDeckCollection] Awake 완료: scene={gameObject.scene.name}, object={name}, baseDeck={baseBattleDeck.Count}, earned={earnedBattleCards.Count}");
        }

        public void ConfigureBaseDeck(IEnumerable<BattleCardData> source)
        {
            baseBattleDeck.Clear();
            if (source != null)
            {
                baseBattleDeck.AddRange(source);
            }

            Debug.Log($"[BattleDeckCollection] 기본 전투 덱 설정: baseDeck={baseBattleDeck.Count}");
        }

        public void ResetRun()
        {
            earnedBattleCards.Clear();
            Debug.Log("[BattleDeckCollection] 런 초기화: earnedBattleCards cleared");
        }

        public List<BattleCardData> BuildBattleDeckSources()
        {
            List<BattleCardData> result = new();
            result.AddRange(baseBattleDeck);
            result.AddRange(earnedBattleCards);
            return result;
        }

        public BattleDeckAddResult AddBattleRewardCard(BattleCardData data, BattleCardData replaceTarget = null)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleDeckCollection] 보상 카드 추가 실패: data is null");
                return BattleDeckAddResult.Invalid;
            }

            if (ShouldIgnoreDeckLimit(data))
            {
                earnedBattleCards.Add(data);
                Debug.Log($"[BattleDeckCollection] 제한 무시 카드 추가: {data.CardName}, earned={earnedBattleCards.Count}");
                return BattleDeckAddResult.Added;
            }

            if (GetLimitedEarnedCount() < BattleCardPileState.MaxDeckSize)
            {
                earnedBattleCards.Add(data);
                Debug.Log($"[BattleDeckCollection] 보상 카드 추가: {data.CardName}, earned={earnedBattleCards.Count}");
                return BattleDeckAddResult.Added;
            }

            if (replaceTarget == null)
            {
                Debug.LogWarning($"[BattleDeckCollection] 교체 대상 필요: {data.CardName}");
                return BattleDeckAddResult.NeedsReplacement;
            }

            if (!earnedBattleCards.Remove(replaceTarget))
            {
                Debug.LogWarning($"[BattleDeckCollection] 교체 실패: remove target not found, new={data.CardName}, target={replaceTarget.CardName}");
                return BattleDeckAddResult.Invalid;
            }

            earnedBattleCards.Add(data);
            Debug.Log($"[BattleDeckCollection] 보상 카드 교체: removed={replaceTarget.CardName}, added={data.CardName}, earned={earnedBattleCards.Count}");
            return BattleDeckAddResult.Replaced;
        }

        public void AddPotionCard(BattleCardData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleDeckCollection] 포션 카드 추가 실패: data is null");
                return;
            }

            earnedBattleCards.Add(data);
            Debug.Log($"[BattleDeckCollection] 포션 카드 추가: {data.CardName}, earned={earnedBattleCards.Count}");
        }

        private int GetLimitedEarnedCount()
        {
            int count = 0;
            foreach (var card in earnedBattleCards)
            {
                if (card != null && !ShouldIgnoreDeckLimit(card))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool ShouldIgnoreDeckLimit(BattleCardData data)
        {
            return data != null && (data.IgnoresDeckLimit || data.CardType == BattleCardType.Potion);
        }
    }
}
