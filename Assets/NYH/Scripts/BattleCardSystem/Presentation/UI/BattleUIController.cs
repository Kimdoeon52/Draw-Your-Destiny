namespace NYH.BattleCardSystem
{
    using System.Collections;
    using NYH.CoreCardSystem;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /*
     * BattleUIController
     *
     * 담당:
     * - 씬에 배치된 전투 UI 참조를 모으고, 버튼/이벤트를 각 전용 컨트롤러로 연결합니다.
     * - CardView가 호출하는 기존 public API를 유지해 씬 연결을 보호합니다.
     *
     * 담당하지 않음:
     * - 이동/공격 타겟팅 규칙, 손패 생성, 멀리건 애니메이션, HUD 표시 계산.
     * - 위 책임들은 BattleCardTargetingFlow, BattleHandPresenter, BattleMulliganController, BattleHudPresenter가 처리합니다.
     */
    public class BattleUIController : MonoBehaviour
    {
        [Header("Battle References")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BattleCardSystem battleCardSystem;
        [SerializeField] private BattleGridPreviewSystem gridPreviewSystem;

        [Header("Shared Card UI")]
        [SerializeField] private HandView handView;
        [SerializeField] private CardViewCreator cardViewCreator;
        [SerializeField] private Transform discardPilePoint;

        [Header("HUD")]
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text actionPointsText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Button mulliganConfirmButton;
        [SerializeField] private float mulliganCenterY = 220f;

        private BattleTargetingPreviewPresenter targetingPreviewPresenter;
        private BattleHudPresenter hudPresenter;
        private BattleHandPresenter handPresenter;
        private BattleMulliganController mulliganController;
        private BattleEndTurnDiscardController endTurnDiscardController;
        private BattlePlayedCardResolutionController playedCardResolutionController;
        private BattleAttackTargetingFlow attackTargetingFlow;
        private BattleCardTargetingFlow targetingFlow;
        private bool suppressNextHandRefresh;

        private void Awake()
        {
            ResolveSceneReferences();
            CreateSubControllers();
            BindButtons();
        }

        private void OnEnable()
        {
            if (battleManager == null)
            {
                return;
            }

            battleManager.OnPhaseChanged += HandlePhaseChanged;
            battleManager.OnTurnStarted += HandleTurnStarted;
            battleManager.OnHandStateChanged += RefreshHandView;
            battleManager.OnBattleFinished += HandleBattleFinished;
        }

        private void Start()
        {
            RefreshHud();
            RefreshHandView();
        }

        private void Update()
        {
            targetingFlow?.Tick(new BattleCardTargetingInput(
                Input.mousePosition,
                Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape),
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return),
                Input.GetMouseButton(0),
                Input.GetMouseButtonUp(0),
                Input.GetMouseButtonDown(0)));
        }

        private void OnDisable()
        {
            if (battleManager == null)
            {
                return;
            }

            battleManager.OnPhaseChanged -= HandlePhaseChanged;
            battleManager.OnTurnStarted -= HandleTurnStarted;
            battleManager.OnHandStateChanged -= RefreshHandView;
            battleManager.OnBattleFinished -= HandleBattleFinished;
        }

        public void RefreshBattlePresentation()
        {
            RefreshHud();
            RefreshHandView();
        }

        public void ClearSharedHandView()
        {
            targetingFlow?.Clear(returnCardToHand: false);
            CardViewHoverSystem.Instance?.Hide();

            if (handView != null)
            {
                handView.ClearAllCardsImmediate();
            }

            RefreshHud();
        }

        public void HandleBattleCardClicked(BattleCard battleCard)
        {
            if (battleCard == null)
            {
                return;
            }
        }

        public bool HandleBattleCardReleased(BattleCard battleCard, CardView cardView, Vector2 screenPosition, bool wasDragged)
        {
            if (battleCard == null || battleManager == null || battleCardSystem == null)
            {
                return false;
            }

            if (battleManager.CurrentPhase != BattlePhase.PlayerTurn || battleManager.IsBattleEnded)
            {
                Debug.LogWarning($"[BattleUI] Battle card targeting is only allowed during player turn: phase={battleManager.CurrentPhase}, ended={battleManager.IsBattleEnded}");
                return false;
            }

            targetingFlow?.Begin(battleCard, cardView);
            return true;
        }

        public void HandleShowDeckClicked()
        {
            battleCardSystem?.ShowDeck();
        }

        public void HandleShowDiscardPileClicked()
        {
            battleCardSystem?.ShowDiscardPile();
        }

        private void ResolveSceneReferences()
        {
            if (battleManager == null)
            {
                battleManager = FindFirstObjectByType<BattleManager>();
            }

            if (battleCardSystem == null)
            {
                battleCardSystem = BattleCardSystem.Instance;
            }

            if (handView == null)
            {
                handView = FindFirstObjectByType<HandView>();
            }

            if (cardViewCreator == null)
            {
                cardViewCreator = CardViewCreator.Instance;
            }

            if (gridPreviewSystem == null)
            {
                gridPreviewSystem = FindFirstObjectByType<BattleGridPreviewSystem>();
            }

            if (gridPreviewSystem == null)
            {
                GameObject previewObject = new("BattleGridPreviewSystem");
                gridPreviewSystem = previewObject.AddComponent<BattleGridPreviewSystem>();
            }
        }

        private void CreateSubControllers()
        {
            targetingPreviewPresenter = new BattleTargetingPreviewPresenter(gridPreviewSystem);
            hudPresenter = new BattleHudPresenter(turnText, phaseText, actionPointsText, endTurnButton, mulliganConfirmButton);
            handPresenter = new BattleHandPresenter(handView, cardViewCreator, this, HandleMulliganCardClicked);
            mulliganController = new BattleMulliganController();
            endTurnDiscardController = new BattleEndTurnDiscardController();
            playedCardResolutionController = new BattlePlayedCardResolutionController();
            attackTargetingFlow = new BattleAttackTargetingFlow();
            targetingFlow = new BattleCardTargetingFlow(
                targetingPreviewPresenter,
                attackTargetingFlow,
                CanUseBoardTargeting,
                HandleBattleCardPlayRequested);
        }

        private void BindButtons()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(HandleEndTurnClicked);
            }

            if (mulliganConfirmButton == null)
            {
                return;
            }

            mulliganConfirmButton.onClick.RemoveAllListeners();
            mulliganConfirmButton.onClick.AddListener(HandleConfirmMulliganClicked);
            TMP_Text buttonLabel = mulliganConfirmButton.GetComponentInChildren<TMP_Text>();
            if (buttonLabel != null)
            {
                buttonLabel.text = "넣기";
            }
        }

        private bool CanUseBoardTargeting()
        {
            return battleManager != null
                && battleManager.CurrentPhase == BattlePhase.PlayerTurn
                && !battleManager.IsBattleEnded;
        }

        private void HandlePhaseChanged(BattlePhase _)
        {
            if (battleManager == null || battleManager.CurrentPhase != BattlePhase.PlayerTurn)
            {
                targetingFlow?.Clear(returnCardToHand: true);
            }

            RefreshHud();
        }

        private void HandleTurnStarted(int _, BattleTeam __)
        {
            RefreshHud();
            RefreshHandView();
        }

        private void HandleBattleFinished(BattleResult result)
        {
            targetingFlow?.Clear(returnCardToHand: false);
            gridPreviewSystem?.ResetAllUnitColorsImmediate();
            CardViewHoverSystem.Instance?.Hide();
            RefreshHud();
        }

        private void HandleEndTurnClicked()
        {
            if (battleManager == null)
            {
                return;
            }

            if ((targetingFlow != null && !targetingFlow.IsIdle) || CardView.AnyCardPickedUp)
            {
                return;
            }

            if (battleManager.CurrentPhase == BattlePhase.PlayerTurn)
            {
                endTurnDiscardController ??= new BattleEndTurnDiscardController();
                if (!endTurnDiscardController.IsResolving)
                {
                    StartCoroutine(endTurnDiscardController.DiscardHandThenEndTurn(
                        battleManager,
                        handView,
                        discardPilePoint,
                        handView,
                        RefreshHud));
                }
            }
            else if (battleManager.CurrentPhase == BattlePhase.EnemyTurn)
            {
                battleManager.EndEnemyTurn();
            }
        }

        private void HandleConfirmMulliganClicked()
        {
            if (battleManager == null
                || !battleManager.IsMulliganPhase
                || mulliganController == null
                || !mulliganController.HasSelection
                || mulliganController.IsResolving)
            {
                return;
            }

            StartCoroutine(ResolveMulliganRoutine());
        }

        private IEnumerator ResolveMulliganRoutine()
        {
            yield return mulliganController.ResolveRoutine(
                battleManager,
                handView,
                handPresenter,
                mulliganCenterY,
                RefreshHud,
                () =>
                {
                    suppressNextHandRefresh = true;
                    battleManager.StartPlayerTurnAfterMulligan();
                });
        }

        private void RefreshHud()
        {
            bool isResolvingMulligan = mulliganController != null && mulliganController.IsResolving;
            bool hasMulliganSelection = mulliganController != null && mulliganController.HasSelection;

            hudPresenter?.Refresh(
                battleManager,
                battleCardSystem,
                endTurnDiscardController != null && endTurnDiscardController.IsResolving,
                isResolvingMulligan,
                hasMulliganSelection,
                targetingFlow == null || targetingFlow.IsIdle,
                CardView.AnyCardPickedUp);
        }

        private void RefreshHandView()
        {
            if (suppressNextHandRefresh)
            {
                suppressNextHandRefresh = false;
                RefreshHud();
                return;
            }

            targetingFlow?.Clear(returnCardToHand: false);
            CardViewHoverSystem.Instance?.Hide();

            if (handPresenter == null || !handPresenter.HasRequiredReferences || battleCardSystem == null)
            {
                Debug.LogWarning($"[BattleUI] RefreshHandView skipped: handPresenter={(handPresenter != null)}, battleCardSystem={(battleCardSystem != null)}");
                return;
            }

            mulliganController?.ClearSelection();
            StopAllCoroutines();

            bool isMulliganPhase = battleManager != null && battleManager.IsMulliganPhase;
            StartCoroutine(handPresenter.RebuildHandRoutine(
                battleCardSystem.PileState.Hand,
                isMulliganPhase,
                RefreshHud));
        }

        private void HandleMulliganCardClicked(BattleCard battleCard)
        {
            if (battleManager == null || mulliganController == null)
            {
                return;
            }

            mulliganController.ToggleSelection(
                battleCard,
                battleManager.IsMulliganPhase,
                handPresenter);
            RefreshHud();
        }

        private void HandleBattleCardPlayRequested(BattleCardPlayRequest request)
        {
            if (!request.IsValid || battleCardSystem == null)
            {
                return;
            }

            CardView playedCardView = request.PlayedCardView;
            if (playedCardView != null)
            {
                handView?.RemoveCard(playedCardView.Card);
            }

            battleCardSystem.PlayCard(
                request.Card,
                request.UserUnit,
                request.TargetGrid,
                request.AttackTargetPositions,
                request.PlannedPath,
                request.TargetUnit,
                request.SkipFollowUpAttack,
                request.SkipPostAttackMove,
                () =>
                {
                    StartCoroutine(playedCardResolutionController.Resolve(
                        battleManager,
                        gridPreviewSystem,
                        playedCardView,
                        discardPilePoint,
                        RefreshHandView,
                        RefreshHud));
                });
        }
    }
}
