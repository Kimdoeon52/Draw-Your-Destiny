namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /*
     * BattleUIController
     *
     * ??釉?
     * - BattleManager/BattleCardSystem???怨밴묶???⑤벊??燁삳?諭?UI???怨뚭퍙??몃빍??
     * - ??뺤쨮?怨뺣쭆 ?袁る떮 燁삳?諭띄몴?疫꿸퀣??CardView ?袁ⓥ봺?諭?앮에???밴쉐??HandView??獄쏄퀣???몃빍??
     * - ????곕짗??HUD??揶쏄퉮???랁? 燁삳?諭???뺚댘 ????쇱젫 ?袁る떮 燁삳?諭????쒏틦?? ?怨뚭퍙??몃빍??
     */
    public class BattleUIController : MonoBehaviour
    {
        private enum CardTargetingPhase
        {
            None,
            SelectUnit,
            SelectMoveTarget,
            SelectAttackTarget,
        }

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

        private const bool EnableMoveDebug = false;

        private bool isResolvingEndTurnDiscard;
        private CardTargetingPhase targetingPhase = CardTargetingPhase.None;
        private BattleCard pendingBattleCard;
        private BattleCardTargetingMode pendingTargetingMode = BattleCardTargetingMode.Auto;
        private CardView pendingCardView;
        private BattleUnit pendingUserUnit;
        private List<BattleUnit> selectableUnits = new();
        private HashSet<Vector2Int> selectableMoveCells = new();
        private HashSet<Vector2Int> selectableAttackCells = new();
        private readonly List<Vector2Int> drawnMovePath = new();
        private readonly List<Vector2Int> confirmedMovePath = new();
        private readonly List<Vector2Int> selectedAttackTargetPositions = new();
        private BattleUnit confirmedAttackTargetUnit;
        private bool hasConfirmedAttackTarget;
        private Vector2Int confirmedAttackTargetGrid;
        private int currentMoveBudget;
        private bool hasLastDragCell;
        private Vector2Int lastDraggedMoveCell;
        private bool hasLastMoveHoverCell;
        private Vector2Int lastHoveredMoveCell;
        private bool hasLastAttackHoverCell;
        private bool wasLastAttackHoverValid;
        private Vector2Int lastHoveredAttackCell;

        private void Awake()
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

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(HandleEndTurnClicked);
            }

            if (mulliganConfirmButton != null)
            {
                mulliganConfirmButton.onClick.RemoveAllListeners();
                mulliganConfirmButton.onClick.AddListener(HandleConfirmMulliganClicked);
            }
        }

        private void OnEnable()
        {
            if (battleManager != null)
            {
                battleManager.OnPhaseChanged += HandlePhaseChanged;
                battleManager.OnTurnStarted += HandleTurnStarted;
                battleManager.OnHandStateChanged += RefreshHandView;
                battleManager.OnBattleFinished += HandleBattleFinished;
            }
        }

        private void Start()
        {
            RefreshHud();
            RefreshHandView();
        }

        private void Update()
        {
            if (targetingPhase == CardTargetingPhase.None)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (targetingPhase == CardTargetingPhase.SelectAttackTarget
                    && selectedAttackTargetPositions.Count > 0)
                {
                    selectedAttackTargetPositions.RemoveAt(selectedAttackTargetPositions.Count - 1);
                    confirmedAttackTargetGrid = selectedAttackTargetPositions.Count > 0
                        ? selectedAttackTargetPositions[selectedAttackTargetPositions.Count - 1]
                        : Vector2Int.zero;
                    hasConfirmedAttackTarget = selectedAttackTargetPositions.Count > 0;
                    confirmedAttackTargetUnit = hasConfirmedAttackTarget
                        ? BattleBoardSystem.Instance?.GetUnitAt(confirmedAttackTargetGrid)
                        : null;
                    RefreshAttackPreview(hasLastAttackHoverCell && wasLastAttackHoverValid ? lastHoveredAttackCell : (Vector2Int?)null);
                    return;
                }

                CancelCardTargeting();
                return;
            }

            if (targetingPhase == CardTargetingPhase.SelectMoveTarget
                && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                ConfirmMovePath();
                return;
            }

            if (targetingPhase == CardTargetingPhase.SelectMoveTarget)
            {
                HandleMoveTargetHover(Input.mousePosition);
            }

            if (targetingPhase == CardTargetingPhase.SelectMoveTarget && Input.GetMouseButton(0))
            {
                HandleMovePathDrag(Input.mousePosition);
            }

            if (targetingPhase == CardTargetingPhase.SelectAttackTarget)
            {
                HandleAttackTargetHover(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0))
            {
                hasLastDragCell = false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleBoardTargetingClick(Input.mousePosition);
            }
        }

        private void OnDisable()
        {
            if (battleManager != null)
            {
                battleManager.OnPhaseChanged -= HandlePhaseChanged;
                battleManager.OnTurnStarted -= HandleTurnStarted;
                battleManager.OnHandStateChanged -= RefreshHandView;
                battleManager.OnBattleFinished -= HandleBattleFinished;
            }
        }

        public void RefreshBattlePresentation()
        {
            RefreshHud();
            RefreshHandView();
        }

        public void ClearSharedHandView()
        {
            ClearTargetingState(false);
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
                Debug.LogWarning($"[BattleUI] 燁삳?諭띄몴??????????용뮉 ?怨밴묶??낅빍?? phase={battleManager.CurrentPhase}, ended={battleManager.IsBattleEnded}");
                return false;
            }

            BeginCardTargeting(battleCard, cardView);
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

        private void HandlePhaseChanged(BattlePhase _)
        {
            if (battleManager == null || battleManager.CurrentPhase != BattlePhase.PlayerTurn)
            {
                ClearTargetingState(true);
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
            ClearTargetingState(false);
            CardViewHoverSystem.Instance?.Hide();
            RefreshHud();
            Debug.Log($"[BattleUI] ?袁る떮 ?ル굝利? victory={result.IsVictory}, turn={result.TurnCount}");
        }

        private void HandleEndTurnClicked()
        {
            if (battleManager == null)
            {
                return;
            }

            if (targetingPhase != CardTargetingPhase.None || CardView.AnyCardPickedUp)
            {
                Debug.Log("[BattleUI] 燁삳?諭???????野껊슦??餓λ쵐肉????곸뱽 ?ル굝利??????곷뮸??덈뼄.");
                return;
            }

            if (battleManager.CurrentPhase == BattlePhase.PlayerTurn)
            {
                if (!isResolvingEndTurnDiscard)
                {
                    StartCoroutine(AnimatePlayerEndTurnDiscardThenEndTurn());
                }
            }
            else if (battleManager.CurrentPhase == BattlePhase.EnemyTurn)
            {
                battleManager.EndEnemyTurn();
            }
        }

        private void HandleConfirmMulliganClicked()
        {
            battleManager?.ConfirmMulligan(false);
        }

        private void RefreshHud()
        {
            if (battleManager != null)
            {
                if (turnText != null)
                {
                    turnText.text = $"Turn {battleManager.BattleTurn}";
                }

                if (phaseText != null)
                {
                    phaseText.text = battleManager.CurrentPhase.ToString();
                }
            }

            if (battleCardSystem != null && actionPointsText != null)
            {
                actionPointsText.text = $"AP {battleCardSystem.CurrentActionPoints}";
            }

            if (endTurnButton != null && battleManager != null)
            {
                endTurnButton.interactable = !isResolvingEndTurnDiscard
                    && targetingPhase == CardTargetingPhase.None
                    && !CardView.AnyCardPickedUp
                    && !battleManager.IsBattleEnded
                    && (battleManager.CurrentPhase == BattlePhase.PlayerTurn || battleManager.CurrentPhase == BattlePhase.EnemyTurn);
            }

            if (mulliganConfirmButton != null && battleManager != null)
            {
                mulliganConfirmButton.gameObject.SetActive(battleManager.IsMulliganPhase);
            }
        }

        private void RefreshHandView()
        {
            ClearTargetingState(false);
            CardViewHoverSystem.Instance?.Hide();

            if (handView == null || cardViewCreator == null || battleCardSystem == null)
            {
                Debug.LogWarning($"[BattleUI] RefreshHandView 餓λ쵎?? handView={(handView != null)}, cardViewCreator={(cardViewCreator != null)}, battleCardSystem={(battleCardSystem != null)}");
                return;
            }

            StopAllCoroutines();
            StartCoroutine(RebuildHandRoutine());
        }

        private IEnumerator RebuildHandRoutine()
        {
            handView.ClearAllCardsImmediate();

            int createdCount = 0;
            foreach (var battleCard in battleCardSystem.PileState.Hand)
            {
                if (battleCard == null)
                {
                    continue;
                }

                Card previewCard = BattleCardViewAdapter.CreatePreviewCard(battleCard);
                if (previewCard == null)
                {
                    continue;
                }

                CardView cardView = cardViewCreator.CreateCardView(previewCard, Vector3.zero, Quaternion.identity);
                if (cardView == null)
                {
                    continue;
                }

                BattleCardPlayHandler playHandler = cardView.GetComponent<BattleCardPlayHandler>();
                if (playHandler == null)
                {
                    playHandler = cardView.gameObject.AddComponent<BattleCardPlayHandler>();
                }

                playHandler.Bind(battleCard, this);
                yield return handView.AddCard(cardView);
                createdCount++;
            }

            RefreshHud();
        }

        private BattleUnit FindFirstAlivePlayerUnit()
        {
            BattleUnit[] allUnits = FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);
            foreach (var unit in allUnits)
            {
                if (unit != null && unit.Team == BattleTeam.Player && unit.IsAlive)
                {
                    return unit;
                }
            }

            return null;
        }

        private void BeginCardTargeting(BattleCard battleCard, CardView cardView)
        {
            if (battleCard == null)
            {
                return;
            }

            ClearTargetingState(false);

            pendingBattleCard = battleCard;
            pendingTargetingMode = BattleCardTargetingUtility.ResolveTargetingMode(battleCard);
            pendingCardView = cardView;
            pendingUserUnit = null;
            selectableUnits = FindUsablePlayerUnits(battleCard);
            selectableMoveCells.Clear();
            selectableAttackCells.Clear();
            drawnMovePath.Clear();
            confirmedMovePath.Clear();
            selectedAttackTargetPositions.Clear();
            confirmedAttackTargetUnit = null;
            hasConfirmedAttackTarget = false;
            confirmedAttackTargetGrid = Vector2Int.zero;
            currentMoveBudget = 0;
            targetingPhase = CardTargetingPhase.SelectUnit;
            gridPreviewSystem?.Clear();
            gridPreviewSystem?.ShowUnitBorders(selectableUnits);

            CardViewHoverSystem.Instance?.Hide();
            pendingCardView?.BeginExternalSelection();
        }

        private void HandleBoardTargetingClick(Vector2 screenPosition)
        {
            if (battleManager == null || battleManager.CurrentPhase != BattlePhase.PlayerTurn || battleManager.IsBattleEnded)
            {
                CancelCardTargeting();
                return;
            }

            Vector2Int clickedGrid = ResolveTargetGridPosition(screenPosition);
            BattleUnit clickedUnit = BattleBoardSystem.Instance != null
                ? BattleBoardSystem.Instance.GetUnitAt(clickedGrid)
                : null;

            if (targetingPhase == CardTargetingPhase.SelectUnit)
            {
                TrySelectUserUnit(clickedUnit);
                return;
            }

            if (targetingPhase == CardTargetingPhase.SelectMoveTarget)
            {
                TrySelectMoveTargetByClick(clickedGrid);
                return;
            }

            if (targetingPhase == CardTargetingPhase.SelectAttackTarget)
            {
                TrySelectAttackTarget(clickedGrid, clickedUnit);
            }
        }

        private void TrySelectUserUnit(BattleUnit clickedUnit)
        {
            if (clickedUnit == null || clickedUnit.Team != BattleTeam.Player || !clickedUnit.IsAlive)
            {
                Debug.Log("[BattleUI] 燁삳?諭띄몴???????袁㏓럵 ?醫딅뻺???????뤾쉭??");
                return;
            }

            if (selectableUnits.Count > 0 && !selectableUnits.Contains(clickedUnit))
            {
                Debug.Log("[BattleUI] ?袁⑹삺 燁삳?諭뜻에??????????덈뮉 ?袁㏓럵 ?醫딅뻺???醫뤾문??뤾쉭??");
                return;
            }

            if (BattleBoardSystem.Instance == null || pendingBattleCard == null)
            {
                CancelCardTargeting();
                return;
            }

            pendingUserUnit = clickedUnit;

            if (pendingTargetingMode == BattleCardTargetingMode.MoveOnly
                || pendingTargetingMode == BattleCardTargetingMode.MoveThenAttack)
            {
                currentMoveBudget = ResolveMoveBudget(pendingBattleCard, clickedUnit);
                selectableMoveCells = BattleBoardSystem.Instance.GetSelectableMoveCells(clickedUnit, currentMoveBudget);
                drawnMovePath.Clear();
                confirmedMovePath.Clear();
                selectableAttackCells.Clear();
                hasLastDragCell = false;
                hasLastMoveHoverCell = false;
                hasLastAttackHoverCell = false;
                targetingPhase = CardTargetingPhase.SelectMoveTarget;
                gridPreviewSystem?.ShowMoveCells(selectableMoveCells);
                gridPreviewSystem?.ShowHoverCellBorder(null);
                gridPreviewSystem?.ShowAttackImpactCells(null);
                gridPreviewSystem?.ShowAttackCells(null);
                gridPreviewSystem?.ShowImpactUnitBorders(null);
                gridPreviewSystem?.ShowUnitHighlights(new[] { clickedUnit });
                return;
            }

            if (pendingTargetingMode == BattleCardTargetingMode.AttackOnly
                || pendingTargetingMode == BattleCardTargetingMode.AttackThenMove)
            {
                selectableAttackCells = ResolveAttackSelectionCells(
                    BattleBoardSystem.Instance,
                    clickedUnit,
                    clickedUnit.GridPosition,
                    pendingBattleCard);
                currentMoveBudget = pendingTargetingMode == BattleCardTargetingMode.AttackThenMove
                    ? ResolveMoveBudget(pendingBattleCard, clickedUnit)
                    : 0;
                confirmedMovePath.Clear();
                selectedAttackTargetPositions.Clear();
                confirmedAttackTargetUnit = null;
                hasConfirmedAttackTarget = false;
                hasLastMoveHoverCell = false;
                hasLastAttackHoverCell = false;
                targetingPhase = CardTargetingPhase.SelectAttackTarget;
                RefreshAttackPreview(null);
                return;
            }

            PlayPendingCard(clickedUnit.GridPosition, clickedUnit, null, null);
        }

        private void HandleMoveTargetHover(Vector2 screenPosition)
        {
            if (pendingBattleCard == null || pendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                return;
            }

            Vector2Int hoveredGrid = ResolveTargetGridPosition(screenPosition);
            if (hasLastMoveHoverCell && hoveredGrid == lastHoveredMoveCell)
            {
                return;
            }

            hasLastMoveHoverCell = true;
            lastHoveredMoveCell = hoveredGrid;

            if (!selectableMoveCells.Contains(hoveredGrid))
            {
                gridPreviewSystem?.ShowHoverCellBorder(null);
                return;
            }

            gridPreviewSystem?.ShowHoverCellBorder(hoveredGrid);
        }

        private void TrySelectMoveTargetByClick(Vector2Int clickedGrid)
        {
            if (pendingBattleCard == null || pendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                CancelCardTargeting();
                return;
            }

            if (pendingTargetingMode == BattleCardTargetingMode.MoveThenAttack
                && clickedGrid == pendingUserUnit.GridPosition)
            {
                ConfirmMovePath();
                return;
            }

            if (pendingTargetingMode == BattleCardTargetingMode.AttackThenMove
                && hasConfirmedAttackTarget
                && clickedGrid == pendingUserUnit.GridPosition)
            {
                ConfirmMovePath();
                return;
            }

            if (drawnMovePath.Count > 0 && clickedGrid == drawnMovePath[drawnMovePath.Count - 1])
            {
                ConfirmMovePath();
                return;
            }

            int existingIndex = drawnMovePath.IndexOf(clickedGrid);
            if (existingIndex >= 0)
            {
                drawnMovePath.RemoveRange(existingIndex + 1, drawnMovePath.Count - existingIndex - 1);
                RefreshMovePreview();
                return;
            }

            if (!selectableMoveCells.Contains(clickedGrid))
            {
                Debug.Log("[BattleUI] ??猷?揶쎛?館釉?燁삳챷???????뤾쉭??");
                return;
            }

            if (BattleBoardSystem.Instance.TryBuildMovePath(
                    pendingUserUnit,
                    pendingUserUnit.GridPosition,
                    clickedGrid,
                    currentMoveBudget,
                    out List<Vector2Int> autoPath))
            {
                drawnMovePath.Clear();
                drawnMovePath.AddRange(autoPath);
                RefreshMovePreview();
                return;
            }

            Debug.Log("[BattleUI] ????燁삳㈇?댐쭪???野껋럥以덄몴?筌띾슢諭?????곷뮸??덈뼄.");
        }

        private void HandleMovePathDrag(Vector2 screenPosition)
        {
            if (pendingBattleCard == null || pendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                return;
            }

            Vector2Int hoveredGrid = ResolveTargetGridPosition(screenPosition);
            if (hasLastDragCell && hoveredGrid == lastDraggedMoveCell)
            {
                return;
            }

            hasLastDragCell = true;
            lastDraggedMoveCell = hoveredGrid;

            if (!selectableMoveCells.Contains(hoveredGrid))
            {
                return;
            }

            if (drawnMovePath.Count > 0 && hoveredGrid == drawnMovePath[drawnMovePath.Count - 1])
            {
                return;
            }

            int existingIndex = drawnMovePath.IndexOf(hoveredGrid);
            if (existingIndex >= 0)
            {
                drawnMovePath.RemoveRange(existingIndex + 1, drawnMovePath.Count - existingIndex - 1);
                RefreshMovePreview();
                return;
            }

            if (TryExtendDrawnMovePath(hoveredGrid))
            {
                RefreshMovePreview();
            }
        }

        private void ConfirmMovePath()
        {
            if (pendingTargetingMode == BattleCardTargetingMode.MoveThenAttack)
            {
                if (BattleBoardSystem.Instance == null || pendingUserUnit == null || pendingBattleCard == null)
                {
                    CancelCardTargeting();
                    return;
                }

                Vector2Int finalCell = drawnMovePath.Count > 0
                    ? drawnMovePath[drawnMovePath.Count - 1]
                    : pendingUserUnit.GridPosition;

                HashSet<Vector2Int> attackCells = ResolveAttackSelectionCells(
                    BattleBoardSystem.Instance,
                    pendingUserUnit,
                    finalCell,
                    pendingBattleCard);

                // SRPG筌ｌ꼶??"??猷????멸땋 ??쇱벉 域??袁⑺뒄?癒?퐣 ??쇱젫嚥????뵭 ????덈뮉筌왖"???믪눘? ?類ㅼ뵥??랁?
                // 揶쎛?館釉????춸 ?⑤벀爰??醫뤾문 ??ｍ롦에???랁돥??덈뼄.
                if (attackCells.Count == 0)
                {
                    if (drawnMovePath.Count > 0)
                    {
                        // ??猷????⑤벀爰?燁삳?諭??곕즲 ???숁쾮??퓠 ?怨몄뵠 ??곸몵筌?
                        // ?⑤벀爰???ｍ롧몴???몄셽??랁???猷욑쭕???묐뻬??????뉗쓺 ??볥빍??
                        PlayPendingCard(finalCell, null, null, drawnMovePath, skipFollowUpAttack: true);
                        return;
                    }

                    Debug.Log("[BattleUI] ?袁⑹삺 ?袁⑺뒄?癒?퐣???⑤벀爰?揶쎛?館釉??怨몄뵠 ??곷뮸??덈뼄. ??猷?野껋럥以덄몴?域밸챶?곩쳞怨뺢돌 ??삘뀲 ?醫딅뻺???醫뤾문??뤾쉭??");
                    RefreshMovePreview();
                    return;
                }

                confirmedMovePath.Clear();
                if (drawnMovePath.Count > 0)
                {
                    confirmedMovePath.AddRange(drawnMovePath);
                }

                selectableAttackCells = attackCells;
                hasLastAttackHoverCell = false;
                targetingPhase = CardTargetingPhase.SelectAttackTarget;
                RefreshAttackPreview(null);
                return;
            }

            if (pendingTargetingMode == BattleCardTargetingMode.AttackThenMove)
            {
                if (!hasConfirmedAttackTarget)
                {
                    Debug.Log("[BattleUI] ?믪눘? ?⑤벀爰????怨몄뱽 ?醫뤾문??뤾쉭??");
                    return;
                }

                PlayPendingCard(
                    confirmedAttackTargetGrid,
                    confirmedAttackTargetUnit,
                    selectedAttackTargetPositions,
                    drawnMovePath.Count > 0 ? drawnMovePath : null,
                    skipPostAttackMove: drawnMovePath.Count == 0);
                return;
            }

            if (drawnMovePath.Count == 0)
            {
                Debug.Log("[BattleUI] ?믪눘? ??猷?野껋럥以덄몴?域밸챶??틠?깃쉭??");
                return;
            }

            Vector2Int finalMoveCell = drawnMovePath[drawnMovePath.Count - 1];
            PlayPendingCard(finalMoveCell, null, null, drawnMovePath);
        }

        private static int ResolveMoveBudget(BattleCard battleCard, BattleUnit userUnit)
        {
            if (battleCard == null || userUnit == null)
            {
                return 0;
            }

            BattleMoveEffect moveEffect = BattleEffectResolver.GetMoveEffect(battleCard);
            if (moveEffect != null)
            {
                if (moveEffect.IncludeSourceUnitSpeed)
                {
                    return Mathf.Max(0, moveEffect.Amount + userUnit.CurrentSpeed);
                }

                return Mathf.Max(0, moveEffect.Amount);
            }

            return 0;
        }

        private static List<BattleUnit> FindUsablePlayerUnits(BattleCard battleCard)
        {
            List<BattleUnit> result = new();
            BattleBoardSystem boardSystem = BattleBoardSystem.Instance;
            BattleUnit[] allUnits = FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);

            foreach (BattleUnit unit in allUnits)
            {
                if (unit == null || unit.Team != BattleTeam.Player || !unit.IsAlive)
                {
                    continue;
                }

                BattleCardTargetingMode targetingMode = BattleCardTargetingUtility.ResolveTargetingMode(battleCard);
                if ((targetingMode == BattleCardTargetingMode.MoveOnly || targetingMode == BattleCardTargetingMode.MoveThenAttack) && boardSystem != null)
                {
                    int moveBudget = ResolveMoveBudget(battleCard, unit);
                    HashSet<Vector2Int> moveCells = boardSystem.GetSelectableMoveCells(unit, moveBudget);

                    if (targetingMode == BattleCardTargetingMode.MoveOnly && moveCells.Count == 0)
                    {
                        continue;
                    }

                    if (targetingMode == BattleCardTargetingMode.MoveThenAttack)
                    {
                        bool canAttackFromCurrent = ResolveAttackSelectionCells(
                            boardSystem,
                            unit,
                            unit.GridPosition,
                            battleCard).Count > 0;

                        if (!canAttackFromCurrent && moveCells.Count == 0)
                        {
                            continue;
                        }
                    }
                }

                if ((targetingMode == BattleCardTargetingMode.AttackOnly
                    || targetingMode == BattleCardTargetingMode.AttackThenMove) && boardSystem != null)
                {
                    HashSet<Vector2Int> attackCells = ResolveAttackSelectionCells(
                        boardSystem,
                        unit,
                        unit.GridPosition,
                        battleCard);

                    if (attackCells.Count == 0)
                    {
                        continue;
                    }
                }

                result.Add(unit);
            }

            return result;
        }

        private static Vector2Int ResolveTargetGridPosition(Vector2 screenPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return Vector2Int.zero;
            }

            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.y));
        }

        private int CalculateDrawnPathCost()
        {
            if (BattleBoardSystem.Instance == null || drawnMovePath.Count == 0)
            {
                return 0;
            }

            int cost = 0;
            for (int i = 0; i < drawnMovePath.Count; i++)
            {
                cost += BattleBoardSystem.Instance.GetStepCost(drawnMovePath[i]);
            }

            return cost;
        }

        private bool TryExtendDrawnMovePath(Vector2Int hoveredGrid)
        {
            if (BattleBoardSystem.Instance == null || pendingUserUnit == null)
            {
                return false;
            }

            Vector2Int segmentStart = drawnMovePath.Count > 0
                ? drawnMovePath[drawnMovePath.Count - 1]
                : pendingUserUnit.GridPosition;

            int remainingBudget = Mathf.Max(0, currentMoveBudget - CalculateDrawnPathCost());
            if (remainingBudget <= 0)
            {
                return false;
            }

            if (!BattleBoardSystem.Instance.TryBuildMovePath(
                    pendingUserUnit,
                    segmentStart,
                    hoveredGrid,
                    remainingBudget,
                    out List<Vector2Int> pathSegment))
            {
                return false;
            }

            if (pathSegment == null || pathSegment.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < pathSegment.Count; i++)
            {
                Vector2Int cell = pathSegment[i];
                if (drawnMovePath.Count > 0 && drawnMovePath[drawnMovePath.Count - 1] == cell)
                {
                    continue;
                }

                drawnMovePath.Add(cell);
            }

            return true;
        }

        private void RefreshMovePreview()
        {
            gridPreviewSystem?.ShowMoveCells(selectableMoveCells);
            gridPreviewSystem?.ShowHoverCellBorder(
                hasLastMoveHoverCell && selectableMoveCells.Contains(lastHoveredMoveCell)
                    ? lastHoveredMoveCell
                    : (Vector2Int?)null);
            gridPreviewSystem?.ShowUnitHighlights(new[] { pendingUserUnit });
            gridPreviewSystem?.ShowImpactUnitBorders(null);
            gridPreviewSystem?.ShowAttackSelectionOrder(selectedAttackTargetPositions);

            if (drawnMovePath.Count > 0)
            {
                gridPreviewSystem?.ShowPathCells(drawnMovePath);
            }
            else
            {
                gridPreviewSystem?.ShowPathCells(null);
            }

            if (pendingTargetingMode == BattleCardTargetingMode.MoveThenAttack
                && pendingBattleCard != null
                && pendingUserUnit != null
                && BattleBoardSystem.Instance != null)
            {
                Vector2Int previewOrigin = drawnMovePath.Count > 0
                    ? drawnMovePath[drawnMovePath.Count - 1]
                    : pendingUserUnit.GridPosition;
                HashSet<Vector2Int> previewAttackCells = ResolveAttackSelectionCells(
                    BattleBoardSystem.Instance,
                    pendingUserUnit,
                    previewOrigin,
                    pendingBattleCard);
                gridPreviewSystem?.ShowAttackCells(previewAttackCells);
                gridPreviewSystem?.ShowAttackImpactCells(null);
                return;
            }

            gridPreviewSystem?.ShowAttackImpactCells(null);
            gridPreviewSystem?.ShowAttackCells(null);
        }

        private void HandleAttackTargetHover(Vector2 screenPosition)
        {
            if (pendingBattleCard == null || pendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                return;
            }

            Vector2Int hoveredGrid = ResolveTargetGridPosition(screenPosition);
            BattleUnit hoveredUnit = BattleBoardSystem.Instance.GetUnitAt(hoveredGrid);
            bool isValidHover = IsValidAttackHover(hoveredGrid, hoveredUnit);
            if (hasLastAttackHoverCell
                && hoveredGrid == lastHoveredAttackCell
                && wasLastAttackHoverValid == isValidHover)
            {
                return;
            }

            hasLastAttackHoverCell = true;
            lastHoveredAttackCell = hoveredGrid;
            wasLastAttackHoverValid = isValidHover;

            if (!isValidHover)
            {
                RefreshAttackPreview(null);
                return;
            }

            RefreshAttackPreview(hoveredGrid);
        }

        private void TrySelectAttackTarget(Vector2Int clickedGrid, BattleUnit clickedUnit)
        {
            if (pendingBattleCard == null || pendingUserUnit == null)
            {
                CancelCardTargeting();
                return;
            }

            bool isGroundTargetAttack = IsGroundTargetAttack(pendingBattleCard);
            if (!IsValidAttackHover(clickedGrid, clickedUnit))
            {
                Debug.Log(isGroundTargetAttack
                    ? "[BattleUI] 공격 가능한 칸을 선택해야 합니다."
                    : "[BattleUI] 공격 가능한 적이 있는 칸을 선택해야 합니다.");
                return;
            }

            List<BattleUnit> previewTargets = ResolvePreviewAttackTargets(clickedGrid);
            BattleUnit resolvedTarget = null;
            if (!isGroundTargetAttack && previewTargets.Count > 0)
            {
                if (clickedUnit != null && clickedUnit.Team == BattleTeam.Enemy && clickedUnit.IsAlive)
                {
                    resolvedTarget = clickedUnit;
                }
                else
                {
                    resolvedTarget = previewTargets[0];
                }
            }

            selectedAttackTargetPositions.Add(clickedGrid);
            confirmedAttackTargetGrid = clickedGrid;
            confirmedAttackTargetUnit = isGroundTargetAttack ? null : resolvedTarget;
            hasConfirmedAttackTarget = true;

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(pendingBattleCard);
            int requiredSelectionCount = attackEffect != null ? attackEffect.SelectionCount : 1;
            if (selectedAttackTargetPositions.Count < requiredSelectionCount)
            {
                RefreshAttackPreview(clickedGrid);
                return;
            }

            if (pendingTargetingMode != BattleCardTargetingMode.AttackThenMove)
            {
                PlayPendingCard(
                    clickedGrid,
                    isGroundTargetAttack ? null : resolvedTarget,
                    selectedAttackTargetPositions,
                    confirmedMovePath.Count > 0 ? confirmedMovePath : null);
                return;
            }

            if (BattleBoardSystem.Instance == null)
            {
                CancelCardTargeting();
                return;
            }

            drawnMovePath.Clear();
            confirmedMovePath.Clear();
            hasLastDragCell = false;
            selectableMoveCells = BattleBoardSystem.Instance.GetSelectableMoveCells(pendingUserUnit, currentMoveBudget);

            if (selectableMoveCells.Count == 0)
            {
                PlayPendingCard(
                    confirmedAttackTargetGrid,
                    confirmedAttackTargetUnit,
                    selectedAttackTargetPositions,
                    null,
                    skipPostAttackMove: true);
                return;
            }

            targetingPhase = CardTargetingPhase.SelectMoveTarget;
            RefreshMovePreview();
        }

        private void PlayPendingCard(
            Vector2Int targetGrid,
            BattleUnit targetUnit,
            IReadOnlyList<Vector2Int> attackTargetPositions,
            IReadOnlyList<Vector2Int> plannedPath,
            bool skipFollowUpAttack = false,
            bool skipPostAttackMove = false)
        {
            if (pendingBattleCard == null || pendingUserUnit == null)
            {
                CancelCardTargeting();
                return;
            }

            BattleCard cardToPlay = pendingBattleCard;
            BattleUnit userUnit = pendingUserUnit;
            CardView playedCardView = pendingCardView;
            List<Vector2Int> attackTargetSnapshot = attackTargetPositions != null
                ? new List<Vector2Int>(attackTargetPositions)
                : (selectedAttackTargetPositions.Count > 0 ? new List<Vector2Int>(selectedAttackTargetPositions) : null);
            List<Vector2Int> plannedPathSnapshot = plannedPath != null
                ? new List<Vector2Int>(plannedPath)
                : (confirmedMovePath.Count > 0 ? new List<Vector2Int>(confirmedMovePath) : null);

            CardViewHoverSystem.Instance?.Hide();
            if (playedCardView != null)
            {
                handView?.RemoveCard(playedCardView.Card);
            }

            ClearTargetingState(false);

            battleCardSystem.PlayCard(
                cardToPlay,
                userUnit,
                targetGrid,
                attackTargetSnapshot,
                plannedPathSnapshot,
                targetUnit,
                skipFollowUpAttack,
                skipPostAttackMove,
                () =>
                {
                    StartCoroutine(HandlePlayedCardResolved(playedCardView));
                });
        }

        private void CancelCardTargeting()
        {
            ClearTargetingState(true);
        }

        private void ClearTargetingState(bool returnCardToHand)
        {
            CardViewHoverSystem.Instance?.Hide();

            if (returnCardToHand && pendingCardView != null)
            {
                pendingCardView.CancelExternalSelection();
            }

            targetingPhase = CardTargetingPhase.None;
            pendingBattleCard = null;
            pendingTargetingMode = BattleCardTargetingMode.Auto;
            pendingCardView = null;
            pendingUserUnit = null;
            selectableUnits.Clear();
            selectableMoveCells.Clear();
            selectableAttackCells.Clear();
            drawnMovePath.Clear();
            confirmedMovePath.Clear();
            selectedAttackTargetPositions.Clear();
            confirmedAttackTargetUnit = null;
            hasConfirmedAttackTarget = false;
            confirmedAttackTargetGrid = Vector2Int.zero;
            currentMoveBudget = 0;
            hasLastDragCell = false;
            hasLastMoveHoverCell = false;
            hasLastAttackHoverCell = false;
            wasLastAttackHoverValid = false;
            gridPreviewSystem?.Clear();
        }

        private IEnumerator HandlePlayedCardResolved(CardView playedCardView)
        {
            CardViewHoverSystem.Instance?.Hide();

            if (playedCardView != null)
            {
                if (discardPilePoint != null)
                {
                    yield return CardViewAnimationUtility.AnimateDiscard(playedCardView, discardPilePoint);
                }
                else
                {
                    Destroy(playedCardView.gameObject);
                }
            }

            battleManager.CheckBattleEnd();
            RefreshHandView();
            RefreshHud();
        }

        private void RefreshAttackPreview(Vector2Int? hoveredGrid)
        {
            gridPreviewSystem?.ShowMoveCells(null);
            gridPreviewSystem?.ShowUnitHighlights(new[] { pendingUserUnit });
            gridPreviewSystem?.ShowAttackCells(selectableAttackCells);
            gridPreviewSystem?.ShowAttackSelectionOrder(selectedAttackTargetPositions);
            gridPreviewSystem?.ShowHoverCellBorder(null);

            if (confirmedMovePath.Count > 0)
            {
                gridPreviewSystem?.ShowPathCells(confirmedMovePath);
            }
            else
            {
                gridPreviewSystem?.ShowPathCells(null);
            }

            if (hoveredGrid.HasValue)
            {
                BattleUnit hoveredUnit = BattleBoardSystem.Instance != null
                    ? BattleBoardSystem.Instance.GetUnitAt(hoveredGrid.Value)
                    : null;

                if (IsValidAttackHover(hoveredGrid.Value, hoveredUnit))
                {
                    gridPreviewSystem?.ShowAttackImpactCells(ResolvePreviewAttackCells(hoveredGrid.Value));
                    gridPreviewSystem?.ShowImpactUnitBorders(ResolvePreviewImpactTargets(hoveredGrid.Value));
                    return;
                }
            }

            gridPreviewSystem?.ShowAttackImpactCells(null);
            gridPreviewSystem?.ShowImpactUnitBorders(null);
        }

        private bool IsValidAttackHover(Vector2Int hoveredGrid, BattleUnit hoveredUnit)
        {
            return selectableAttackCells.Contains(hoveredGrid);
        }

        private static bool IsGroundTargetAttack(BattleCard battleCard)
        {
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return false;
            }

            return attackEffect.CustomTargetingPattern != null
                || attackEffect.TargetingPattern == BattleAttackPattern.Area
                || attackEffect.TargetingPattern == BattleAttackPattern.Line
                || attackEffect.TargetingPattern == BattleAttackPattern.Adjacent4
                || attackEffect.CustomImpactPattern != null
                || attackEffect.ImpactPattern == BattleAttackPattern.Area
                || attackEffect.ImpactPattern == BattleAttackPattern.Line
                || attackEffect.ImpactPattern == BattleAttackPattern.Adjacent4;
        }

        private static bool IsAreaAttack(BattleCard battleCard)
        {
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return false;
            }

            if (attackEffect.HitsAllTargetsInRange || attackEffect.ImpactPattern == BattleAttackPattern.Area)
            {
                return true;
            }

            return attackEffect.CustomImpactPattern != null && attackEffect.CustomImpactPattern.Cells.Count > 1;
        }

        private HashSet<Vector2Int> ResolvePreviewAttackCells(Vector2Int targetGrid)
        {
            HashSet<Vector2Int> result = new();
            if (BattleBoardSystem.Instance == null || pendingBattleCard == null || pendingUserUnit == null)
            {
                return result;
            }

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(pendingBattleCard);
            if (attackEffect == null)
            {
                result.Add(targetGrid);
                return result;
            }

            Vector2Int attackOrigin = confirmedMovePath.Count > 0
                ? confirmedMovePath[confirmedMovePath.Count - 1]
                : pendingUserUnit.GridPosition;

            if (attackEffect.CustomImpactPattern != null)
            {
                return BattleBoardSystem.Instance.ResolvePatternCells(
                    attackOrigin,
                    targetGrid,
                    attackEffect.CustomImpactPattern);
            }

            switch (attackEffect.ImpactPattern)
            {
                case BattleAttackPattern.Area:
                    AddDiamondCells(targetGrid, attackEffect.ImpactRange, result);
                    break;

                case BattleAttackPattern.Line:
                    AddLineCellsTowardsTarget(attackOrigin, targetGrid, attackEffect.ImpactRange, result);
                    break;

                case BattleAttackPattern.Adjacent4:
                    AddDiamondCells(targetGrid, 1, result);
                    break;

                case BattleAttackPattern.None:
                default:
                    result.Add(targetGrid);
                    break;
            }

            return result;
        }

        private List<BattleUnit> ResolvePreviewImpactTargets(Vector2Int targetGrid)
        {
            List<BattleUnit> result = new();
            if (BattleBoardSystem.Instance == null || pendingUserUnit == null)
            {
                return result;
            }

            HashSet<Vector2Int> impactCells = ResolvePreviewAttackCells(targetGrid);
            foreach (Vector2Int cell in impactCells)
            {
                BattleUnit unit = BattleBoardSystem.Instance.GetUnitAt(cell);
                if (unit == null
                    || !unit.IsAlive
                    || unit.Team == pendingUserUnit.Team
                    || result.Contains(unit))
                {
                    continue;
                }

                result.Add(unit);
            }

            return result;
        }

        private static void AddDiamondCells(Vector2Int center, int range, HashSet<Vector2Int> destination)
        {
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    Vector2Int offset = new(x, y);
                    if (Mathf.Abs(offset.x) + Mathf.Abs(offset.y) <= range)
                    {
                        destination.Add(center + offset);
                    }
                }
            }
        }

        private static void AddLineCellsTowardsTarget(
            Vector2Int origin,
            Vector2Int target,
            int range,
            HashSet<Vector2Int> destination)
        {
            Vector2Int delta = target - origin;
            Vector2Int direction;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                direction = delta.x >= 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                direction = delta.y >= 0 ? Vector2Int.up : Vector2Int.down;
            }

            for (int i = 1; i <= Mathf.Max(1, range); i++)
            {
                destination.Add(origin + (direction * i));
            }
        }

        private static HashSet<Vector2Int> ResolveAttackSelectionCells(
            BattleBoardSystem boardSystem,
            BattleUnit attacker,
            Vector2Int attackOrigin,
            BattleCard battleCard)
        {
            HashSet<Vector2Int> result = new();
            if (boardSystem == null || attacker == null || battleCard == null)
            {
                return result;
            }

            return boardSystem.GetSelectableAttackCells(attacker, attackOrigin, battleCard);
        }

        private static List<BattleUnit> ResolvePreviewAttackTargets(
            BattleBoardSystem boardSystem,
            BattleUnit attacker,
            Vector2Int attackOrigin,
            BattleCard battleCard,
            Vector2Int targetGrid)
        {
            List<BattleUnit> result = new();
            if (boardSystem == null || attacker == null || battleCard == null)
            {
                return result;
            }

            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return result;
            }

            BattleAttackGA previewAttack = new(
                battleCard,
                attacker,
                null,
                targetGrid,
                0,
                attackEffect.ImpactRange,
                attackEffect.TargetCount,
                attackEffect.HitsAllTargetsInRange,
                attackEffect.ImpactPattern,
                attackEffect.CustomImpactPattern);

            result.AddRange(boardSystem.GetUnitsInAttackArea(
                attacker,
                attackOrigin,
                targetGrid,
                previewAttack));

            return result;
        }

        private List<BattleUnit> ResolvePreviewAttackTargets(Vector2Int targetGrid)
        {
            if (BattleBoardSystem.Instance == null || pendingBattleCard == null || pendingUserUnit == null)
            {
                return new List<BattleUnit>();
            }

            Vector2Int attackOrigin = confirmedMovePath.Count > 0
                ? confirmedMovePath[confirmedMovePath.Count - 1]
                : pendingUserUnit.GridPosition;

            return ResolvePreviewAttackTargets(
                BattleBoardSystem.Instance,
                pendingUserUnit,
                attackOrigin,
                pendingBattleCard,
                targetGrid);
        }

        private static void LogSelectableUnits(string caller, BattleCard battleCard, IReadOnlyList<BattleUnit> units)
        {
            System.Text.StringBuilder builder = new();
            if (units != null)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    BattleUnit unit = units[i];
                    if (unit == null)
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(" | ");
                    }

                    builder.Append($"{unit.name}:grid={unit.GridPosition},world={unit.transform.position},alive={unit.IsAlive},team={unit.Team}");
                }
            }

            //if (EnableMoveDebug)
            //{
            //    Debug.Log(
            //        $"[BattleUI] {caller}: card={(battleCard != null ? battleCard.Title : "null")}, selectableCount={(units != null ? units.Count : 0)}, units={builder}");
            //}
        }

        private static void LogCells(string caller, BattleUnit unit, IEnumerable<Vector2Int> cells)
        {
            System.Text.StringBuilder builder = new();
            int count = 0;

            if (cells != null)
            {
                foreach (Vector2Int cell in cells)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(cell);
                    count++;
                }
            }

            //if (EnableMoveDebug)
            //{
            //    Debug.Log(
            //        $"[BattleUI] {caller}: unit={(unit != null ? unit.name : "null")}, grid={(unit != null ? unit.GridPosition.ToString() : "null")}, world={(unit != null ? unit.transform.position.ToString() : "null")}, cellCount={count}, cells=[{builder}]");
            //}
        }

        private void LogPathState()
        {
            System.Text.StringBuilder builder = new();
            for (int i = 0; i < drawnMovePath.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(drawnMovePath[i]);
            }

            //if (EnableMoveDebug)
            //{
            //    Debug.Log(
            //        $"[BattleUI] MovePath ?怨밴묶: unit={(pendingUserUnit != null ? pendingUserUnit.name : "null")}, currentBudget={currentMoveBudget}, currentCost={CalculateDrawnPathCost()}, pathCount={drawnMovePath.Count}, path={builder}");
            //}
        }

        private static string BuildPathDebugText(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                return "(empty)";
            }

            System.Text.StringBuilder builder = new();
            for (int i = 0; i < path.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(path[i]);
            }

            return builder.ToString();
        }

        private static string AnalyzePathDebug(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                return "empty";
            }

            int duplicateCount = 0;
            int brokenSegments = 0;
            HashSet<Vector2Int> seen = new();

            for (int i = 0; i < path.Count; i++)
            {
                if (!seen.Add(path[i]))
                {
                    duplicateCount++;
                }

                if (i == 0)
                {
                    continue;
                }

                Vector2Int previous = path[i - 1];
                Vector2Int current = path[i];
                int manhattan = Mathf.Abs(previous.x - current.x) + Mathf.Abs(previous.y - current.y);
                if (manhattan != 1)
                {
                    brokenSegments++;
                }
            }

            return $"duplicates={duplicateCount}, brokenSegments={brokenSegments}";
        }

        private IEnumerator AnimatePlayerEndTurnDiscardThenEndTurn()
        {
            if (battleManager == null || battleManager.CurrentPhase != BattlePhase.PlayerTurn)
            {
                yield break;
            }

            if (discardPilePoint == null || handView == null || handView.Cards.Count == 0)
            {
                battleManager.EndPlayerTurn();
                yield break;
            }

            isResolvingEndTurnDiscard = true;
            RefreshHud();

            CardView[] cardsToDiscard = new CardView[handView.Cards.Count];
            for (int i = 0; i < handView.Cards.Count; i++)
            {
                cardsToDiscard[i] = handView.Cards[i];
            }

            foreach (var cardView in cardsToDiscard)
            {
                if (cardView == null)
                {
                    continue;
                }

                handView.RemoveCard(cardView.Card);
                StartCoroutine(CardViewAnimationUtility.AnimateDiscard(cardView, discardPilePoint));
                yield return new WaitForSeconds(0.05f);
            }

            battleManager.EndPlayerTurn();
            isResolvingEndTurnDiscard = false;
            RefreshHud();
        }
    }
}


