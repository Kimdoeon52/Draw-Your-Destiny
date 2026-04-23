namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleDeckCollection
     *
     * 역할:
     * - 전투 시작에 사용할 "현재 전투덱"을 보관합니다.
     * - 보상 전투카드 추가, 30장 제한 확인, 기존 카드 교체를 담당합니다.
     * - 실제 저장/로드 방식은 BattleDeckPersistenceService에 위임합니다.
     *
     * 주의:
     * - baseBattleDeck은 기본 지급 덱입니다.
     * - earnedBattleCards는 보상으로 얻은 카드 기록입니다.
     * - currentBattleDeck은 실제 전투에 들어갈 최종 덱입니다.
     */
    public class BattleDeckCollection : Singleton<BattleDeckCollection>
    {
        [Header("Base Battle Deck")]
        [SerializeField] private List<BattleCardData> baseBattleDeck = new();

        [Header("Earned Battle Cards")]
        [SerializeField] private List<BattleCardData> earnedBattleCards = new();

        [Header("Current Battle Deck")]
        [SerializeField] private List<BattleCardData> currentBattleDeck = new();

        public IReadOnlyList<BattleCardData> BaseBattleDeck => baseBattleDeck;
        public IReadOnlyList<BattleCardData> EarnedBattleCards => earnedBattleCards;

        public IReadOnlyList<BattleCardData> CurrentBattleDeck
        {
            get
            {
                EnsureCurrentDeckInitialized();
                return currentBattleDeck;
            }
        }

        // 덱 제한에 포함되는 현재 전투덱 카드 수를 반환합니다.
        // 포션이나 IgnoresDeckLimit 카드는 이 수에 포함되지 않습니다.
        public int LimitedDeckCount
        {
            get
            {
                EnsureCurrentDeckInitialized();
                return CountLimitedCards(currentBattleDeck);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                Debug.LogWarning($"[BattleDeckCollection] Duplicate instance will be destroyed: scene={gameObject.scene.name}, object={name}");
                return;
            }

            DontDestroyOnLoad(gameObject);
            EnsureCurrentDeckInitialized();
            Debug.Log($"[BattleDeckCollection] Awake complete: scene={gameObject.scene.name}, object={name}, baseDeck={baseBattleDeck.Count}, earned={earnedBattleCards.Count}, current={currentBattleDeck.Count}");
        }

        // 외부에서 받은 기본 전투덱을 등록합니다.
        // 저장된 전투덱이 아직 없다면 기본덱과 획득 카드를 합쳐 currentBattleDeck을 처음 만듭니다.
        public void ConfigureBaseDeck(IEnumerable<BattleCardData> source)
        {
            baseBattleDeck.Clear();
            if (source != null)
            {
                baseBattleDeck.AddRange(source);
            }

            if (!BattleDeckPersistenceService.HasSavedDeck())
            {
                RebuildCurrentDeckFromParts();
                SaveCurrentDeck();
            }

            Debug.Log($"[BattleDeckCollection] Base deck configured: baseDeck={baseBattleDeck.Count}, current={currentBattleDeck.Count}");
        }

        // 새 런을 시작할 때 보상 카드 기록만 초기화합니다.
        // 현재 전투덱 저장값은 유지되므로, 영구 교체 결과는 지워지지 않습니다.
        public void ResetRun()
        {
            earnedBattleCards.Clear();
            Debug.Log("[BattleDeckCollection] Run reset: earnedBattleCards cleared");
        }

        // BattleCardPileState.Setup()에 넘길 현재 전투덱 사본을 만듭니다.
        // 호출자가 리스트를 수정해도 저장소 내부 리스트가 바뀌지 않게 복사본을 반환합니다.
        public List<BattleCardData> BuildBattleDeckSources()
        {
            EnsureCurrentDeckInitialized();
            return new List<BattleCardData>(currentBattleDeck);
        }

        // 기존 코드 호환용 진입점입니다.
        // replaceTarget이 있으면 즉시 교체하고, 없으면 일반 보상 카드 추가 흐름을 탑니다.
        public BattleDeckAddResult AddBattleRewardCard(BattleCardData data, BattleCardData replaceTarget = null)
        {
            if (replaceTarget != null)
            {
                return ReplaceCard(replaceTarget, data);
            }

            return AddRewardCard(data);
        }

        // 보상 전투카드를 현재 전투덱에 추가합니다.
        // 30장 제한에 걸리면 직접 교체하지 않고 NeedsReplacement를 반환합니다.
        public BattleDeckAddResult AddRewardCard(BattleCardData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleDeckCollection] Add reward failed: data is null");
                return BattleDeckAddResult.Invalid;
            }

            EnsureCurrentDeckInitialized();
            if (CanAddWithoutReplacement(data))
            {
                AddCardToCurrentDeck(data, trackAsEarned: true);
                Debug.Log($"[BattleDeckCollection] Reward added: {data.CardName}, current={currentBattleDeck.Count}");
                return BattleDeckAddResult.Added;
            }

            Debug.LogWarning($"[BattleDeckCollection] Replacement required: {data.CardName}");
            return BattleDeckAddResult.NeedsReplacement;
        }

        // 현재 전투덱에서 removeTarget을 제거하고 addTarget을 추가합니다.
        // 기본덱 카드도 currentBattleDeck에 들어있으면 교체 대상이 될 수 있습니다.
        public BattleDeckAddResult ReplaceCard(BattleCardData removeTarget, BattleCardData addTarget)
        {
            if (removeTarget == null || addTarget == null)
            {
                Debug.LogWarning("[BattleDeckCollection] Replace failed: removeTarget or addTarget is null");
                return BattleDeckAddResult.Invalid;
            }

            EnsureCurrentDeckInitialized();
            if (ShouldIgnoreDeckLimit(removeTarget))
            {
                Debug.LogWarning($"[BattleDeckCollection] Replace failed: target ignores deck limit, target={removeTarget.CardName}");
                return BattleDeckAddResult.Invalid;
            }

            int removeIndex = currentBattleDeck.IndexOf(removeTarget);
            if (removeIndex < 0)
            {
                Debug.LogWarning($"[BattleDeckCollection] Replace failed: target not found, new={addTarget.CardName}, target={removeTarget.CardName}");
                return BattleDeckAddResult.Invalid;
            }

            currentBattleDeck.RemoveAt(removeIndex);
            earnedBattleCards.Remove(removeTarget);
            currentBattleDeck.Add(addTarget);
            earnedBattleCards.Add(addTarget);
            SaveCurrentDeck();

            Debug.Log($"[BattleDeckCollection] Reward replaced: removed={removeTarget.CardName}, added={addTarget.CardName}, current={currentBattleDeck.Count}");
            return BattleDeckAddResult.Replaced;
        }

        // 새 카드가 교체 없이 들어갈 수 있는지 확인합니다.
        // 제한 무시 카드는 덱이 30장 이상이어도 true입니다.
        public bool CanAddWithoutReplacement(BattleCardData data)
        {
            if (data == null)
            {
                return false;
            }

            EnsureCurrentDeckInitialized();
            return ShouldIgnoreDeckLimit(data) || LimitedDeckCount < BattleCardPileState.MaxDeckSize;
        }

        // 교체 UI에 보여줄 수 있는 카드만 골라 반환합니다.
        // 포션이나 제한 무시 카드는 교체 후보에서 제외합니다.
        public List<BattleCardData> GetReplaceableCards()
        {
            EnsureCurrentDeckInitialized();
            List<BattleCardData> result = new();
            foreach (BattleCardData card in currentBattleDeck)
            {
                if (card != null && !ShouldIgnoreDeckLimit(card))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        // 포션처럼 덱 제한을 무시하는 카드를 현재 전투덱에 추가합니다.
        public void AddPotionCard(BattleCardData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleDeckCollection] Add potion failed: data is null");
                return;
            }

            EnsureCurrentDeckInitialized();
            AddCardToCurrentDeck(data, trackAsEarned: true);
            Debug.Log($"[BattleDeckCollection] Potion added: {data.CardName}, current={currentBattleDeck.Count}");
        }

        // currentBattleDeck이 비어 있을 때 저장값 또는 기본 구성으로 초기화합니다.
        // 저장값이 있는데 카탈로그가 아직 준비되지 않았다면 빈 덱으로 확정하지 않고 다음 호출을 기다립니다.
        private void EnsureCurrentDeckInitialized()
        {
            if (currentBattleDeck.Count > 0)
            {
                return;
            }

            if (BattleDeckPersistenceService.HasSavedDeck())
            {
                if (BattleDeckPersistenceService.TryLoadDeck(out List<BattleCardData> savedDeck))
                {
                    currentBattleDeck.AddRange(savedDeck);
                    return;
                }

                if (BattleCardCatalog.Instance == null)
                {
                    return;
                }
            }

            RebuildCurrentDeckFromParts();
        }

        // 저장값이 없을 때 기본덱과 획득 카드를 합쳐 현재 전투덱을 만듭니다.
        private void RebuildCurrentDeckFromParts()
        {
            currentBattleDeck.Clear();
            currentBattleDeck.AddRange(baseBattleDeck);
            currentBattleDeck.AddRange(earnedBattleCards);
        }

        // 현재 전투덱에 카드를 넣고 필요하면 획득 카드 기록에도 남긴 뒤 저장합니다.
        private void AddCardToCurrentDeck(BattleCardData data, bool trackAsEarned)
        {
            currentBattleDeck.Add(data);
            if (trackAsEarned)
            {
                earnedBattleCards.Add(data);
            }

            SaveCurrentDeck();
        }

        // 현재 전투덱 구성을 PlayerPrefs 저장 서비스에 넘깁니다.
        private void SaveCurrentDeck()
        {
            BattleDeckPersistenceService.SaveDeck(currentBattleDeck);
        }

        // 덱 제한에 포함되는 카드 수를 계산합니다.
        private static int CountLimitedCards(IEnumerable<BattleCardData> source)
        {
            int count = 0;
            if (source == null)
            {
                return count;
            }

            foreach (BattleCardData card in source)
            {
                if (card != null && !ShouldIgnoreDeckLimit(card))
                {
                    count++;
                }
            }

            return count;
        }

        // 덱 30장 제한과 교체 후보 제한을 무시해야 하는 카드인지 확인합니다.
        private static bool ShouldIgnoreDeckLimit(BattleCardData data)
        {
            return data != null && (data.IgnoresDeckLimit || data.CardType == BattleCardType.Potion);
        }
    }
}
