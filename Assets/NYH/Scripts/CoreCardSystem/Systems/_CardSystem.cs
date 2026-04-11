namespace NYH.CoreCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    // 
    public class CardSystem : Singleton<CardSystem>
    {
        [Header("UI & Positions")]
        [SerializeField] private HandView handView;
        [SerializeField] private Transform drawPilePoint;
        [SerializeField] private Transform discardPilePoint;
        [SerializeField] private Text deackCount;
        [SerializeField] private Text discardCount;

        [Header("Optional Era Data")]
        [SerializeField] private List<EraCardData> eraCards = new();

        private readonly Dictionary<int, EraCardData> eraCardMap = new();

        private CardPileState pileState;                     // 카드 더미 상태 관리 객체 (덱, 손패, 무덤, 소멸 더미 상태 관리) - 여러 performer에서 공유
        private CardPlayPerformer playPerformer;             // 카드 사용, 드로우, 버리기, 소멸, 손 복귀 등 카드 흐름 처리 Performer
        private CardResourcePerformer resourcePerformer;     // 골드, 식량, 연구, 인구, 코스트 변경 등 수치/자원 처리 Performer
        private CardSelectionPerformer selectionPerformer;   // 선택형 카드 및 선택 UI 처리 Performer
        private BuildingCardPerformer buildingPerformer;     // 설치 카드 및 건물 배치 처리 Performer
        private CardModifierPerformer modifierPerformer;     // 카드 타입 배율, 카드 타입 비활성 같은 modifier 처리 Performer
        private CardViewRefreshService viewRefreshService;   // 카드 뷰 갱신 서비스 (여러 performer에서 호출)
        private int cardTypeCount;

        protected override void Awake()
        {
            base.Awake();

            pileState = new CardPileState();
            viewRefreshService = new CardViewRefreshService();

            playPerformer = new CardPlayPerformer(
                this,
                pileState,
                handView,
                drawPilePoint,
                discardPilePoint,
                RefreshPileCounts);

            resourcePerformer = new CardResourcePerformer(
                pileState,
                viewRefreshService.RefreshVisibleCardViews,
                value => cardTypeCount = value);

            selectionPerformer = new CardSelectionPerformer(
                pileState,
                handView,
                deackCount,
                discardCount,
                RefreshPileCounts);

            buildingPerformer = new BuildingCardPerformer();
            modifierPerformer = new CardModifierPerformer(viewRefreshService.RefreshVisibleCardViews);

            CardActionRegistrar.RegisterAll(this);

            RefreshPileCounts();
            Debug.Log("[_CardSystem] 책임 분리 버전 초기화 완료");
        }

        public void Setup(List<CardData> initialDeck)
        {
            pileState.Setup(initialDeck);
            RefreshPileCounts();
            Debug.Log($"[_CardSystem] 덱 세팅 완료: {pileState.DrawPileCount}장");
        }

        public CardPileRuntimeState CaptureRuntimeState()
        {
            return pileState.ExportRuntimeState();
        }

        public bool RestoreRuntimeState(CardPileRuntimeState state)
        {
            if (state == null || CardCatalog.Instance == null)
            {
                return false;
            }

            pileState.ImportRuntimeState(state, id => CardCatalog.Instance.GetByID(id));
            RefreshPileCounts();
            RefreshVisibleCardViews();
            Debug.Log("[_CardSystem] 저장된 문명 덱 상태를 복원했습니다.");
            return true;
        }

        public IEnumerator OfferRandomCatalogCardToDeck(int amount)
        {
            yield return selectionPerformer.OfferRandomCatalogCardToDeck(amount);
        }

        public IEnumerator OfferRewardBundlesToDecks(int bundleCount)
        {
            yield return selectionPerformer.OfferRewardBundlesToDecks(bundleCount);
        }

        public IEnumerator OfferMixedRewardToDecks(int civilizationAmount, int battleAmount)
        {
            yield return selectionPerformer.OfferMixedRewardToDecks(civilizationAmount, battleAmount);
        }


        /// <summary>
        /// 만약 이곳에 규칙에 맞지 않는 액션이 생긴다면 Perform새로운 스크립트를 만들고
        /// 이곳에 분기와 위임 코드를 추가하면 됩니다.
        /// </summary>
        public IEnumerator Perform(GameAction action)
        {
            // 카드 사용, 드로우, 버리기, 소멸, 손 복귀 등 카드 흐름 처리
            if (playPerformer.CanHandle(action))
            {
                yield return playPerformer.Perform(action);
                yield break;
            }

            // 골드, 식량, 연구, 인구, 코스트 변경 등 수치/자원 처리
            if (resourcePerformer.CanHandle(action))
            {
                yield return resourcePerformer.Perform(action);
                yield break;
            }

            // 카드 타입 배율, 카드 타입 비활성 같은 modifier 처리
            if (modifierPerformer.CanHandle(action))
            {
                yield return modifierPerformer.Perform(action);
                yield break;
            }

            //  선택형 카드 및 선택 UI 처리
            if (action is ChooseOneGA chooseOneGA)
            {
                yield return selectionPerformer.ChooseCardPerformer(chooseOneGA.Amount);
                yield break;
            }

            // 설치 카드 및 건물 배치 처리
            if (action is PlayBuildingGA playBuildingGA)
            {
                yield return buildingPerformer.Perform(playBuildingGA);
                yield break;
            }
    
        }

        public bool TryQueuePlacementCard(Card sourceCard, Vector3Int targetPos, bool isTargetingMode)
        {
            return buildingPerformer.TryQueuePlacementCard(sourceCard, targetPos, isTargetingMode);
        }

        /// <summary>
        /// 덱에 있는 카드들을 섞어서 보여줍니다.
        /// </summary>
        public void ShowDeck()
        {
            if (pileState.DrawPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(pileState.GetShuffledDrawPileCopy(), "덱 확인");
        }


        /// <summary>
        /// 버려진 카드들을 섞어서 보여줍니다.
        /// </summary>
        public void ShowGraveyard()
        {
            if (pileState.DiscardPileCount == 0 || CardListUI.Instance == null)
            {
                return;
            }

            CardListUI.Instance.Show(pileState.GetShuffledDiscardPileCopy(), "무덤 확인");
        }


        public void RefreshVisibleCardViews()
        {
            viewRefreshService.RefreshVisibleCardViews();
        }

        private void RefreshPileCounts()
        {
            if (deackCount != null)
            {
                deackCount.text = $"{pileState.DrawPileCount}장";
            }

            if (discardCount != null)
            {
                discardCount.text = $"{pileState.DiscardPileCount}장";
            }
        }

    }
}
