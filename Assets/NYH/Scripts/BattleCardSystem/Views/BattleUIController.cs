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
     * 역할:
     * - BattleManager/BattleCardSystem의 상태를 공용 카드 UI에 연결합니다.
     * - 드로우된 전투 카드를 기존 CardView 프리팹으로 생성해 HandView에 배치합니다.
     * - 턴/행동력 HUD를 갱신하고, 카드 드롭 시 실제 전투 카드 사용까지 연결합니다.
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
                Debug.LogWarning($"[BattleUI] 카드를 사용할 수 없는 상태입니다: phase={battleManager.CurrentPhase}, ended={battleManager.IsBattleEnded}");
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
            Debug.Log($"[BattleUI] 전투 종료: victory={result.IsVictory}, turn={result.TurnCount}");
        }

        private void HandleEndTurnClicked()
        {
            if (battleManager == null)
            {
                return;
            }

            if (targetingPhase != CardTargetingPhase.None || CardView.AnyCardPickedUp)
            {
                Debug.Log("[BattleUI] 카드 사용/타게팅 중에는 턴을 종료할 수 없습니다.");
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
                Debug.LogWarning($"[BattleUI] RefreshHandView 중단: handView={(handView != null)}, cardViewCreator={(cardViewCreator != null)}, battleCardSystem={(battleCardSystem != null)}");
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
                Debug.Log("[BattleUI] 카드를 사용할 아군 유닛을 클릭하세요.");
                return;
            }

            if (selectableUnits.Count > 0 && !selectableUnits.Contains(clickedUnit))
            {
                Debug.Log("[BattleUI] 현재 카드로 사용할 수 있는 아군 유닛을 선택하세요.");
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
                confirmedAttackTargetUnit = null;
                hasConfirmedAttackTarget = false;
                hasLastMoveHoverCell = false;
                hasLastAttackHoverCell = false;
                targetingPhase = CardTargetingPhase.SelectAttackTarget;
                RefreshAttackPreview(null);
                return;
            }

            PlayPendingCard(clickedUnit.GridPosition, clickedUnit, null);
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
                Debug.Log("[BattleUI] 이동 가능한 칸을 클릭하세요.");
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

            Debug.Log("[BattleUI] 해당 칸까지의 경로를 만들 수 없습니다.");
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

                // SRPG처럼 "이동을 끝낸 다음 그 위치에서 실제로 때릴 수 있는지"를 먼저 확인하고
                // 가능할 때만 공격 선택 단계로 넘깁니다.
                if (attackCells.Count == 0)
                {
                    if (drawnMovePath.Count > 0)
                    {
                        // 이동 후 공격 카드라도 사정권에 적이 없으면
                        // 공격 단계를 생략하고 이동만 수행할 수 있게 둡니다.
                        PlayPendingCard(finalCell, null, drawnMovePath, skipFollowUpAttack: true);
                        return;
                    }

                    Debug.Log("[BattleUI] 현재 위치에서는 공격 가능한 적이 없습니다. 이동 경로를 그리거나 다른 유닛을 선택하세요.");
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
                    Debug.Log("[BattleUI] 먼저 공격 대상을 선택하세요.");
                    return;
                }

                PlayPendingCard(
                    confirmedAttackTargetGrid,
                    confirmedAttackTargetUnit,
                    drawnMovePath.Count > 0 ? drawnMovePath : null,
                    skipPostAttackMove: drawnMovePath.Count == 0);
                return;
            }

            if (drawnMovePath.Count == 0)
            {
                Debug.Log("[BattleUI] 먼저 이동 경로를 그려주세요.");
                return;
            }

            Vector2Int finalMoveCell = drawnMovePath[drawnMovePath.Count - 1];
            PlayPendingCard(finalMoveCell, null, drawnMovePath);
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
                    ? "[BattleUI] 공격 범위 안의 타일 또는 적 유닛을 선택하세요."
                    : "[BattleUI] 공격할 적 유닛 위에 마우스를 올리고 선택하세요.");
                return;
            }

            List<BattleUnit> previewTargets = ResolvePreviewAttackTargets(clickedGrid);
            if (!isGroundTargetAttack)
            {
                if (previewTargets.Count == 0)
                {
                    Debug.Log("[BattleUI] 단일 공격 카드는 범위 안의 적 유닛을 클릭해야 합니다.");
                    return;
                }
            }

            BattleUnit resolvedTarget = null;
            if (!isGroundTargetAttack)
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

            if (pendingTargetingMode != BattleCardTargetingMode.AttackThenMove)
            {
                PlayPendingCard(
                    clickedGrid,
                    isGroundTargetAttack ? null : resolvedTarget,
                    confirmedMovePath.Count > 0 ? confirmedMovePath : null);
                return;
            }

            if (BattleBoardSystem.Instance == null)
            {
                CancelCardTargeting();
                return;
            }

            confirmedAttackTargetGrid = clickedGrid;
            confirmedAttackTargetUnit = isGroundTargetAttack ? null : resolvedTarget;
            hasConfirmedAttackTarget = true;
            drawnMovePath.Clear();
            confirmedMovePath.Clear();
            hasLastDragCell = false;
            selectableMoveCells = BattleBoardSystem.Instance.GetSelectableMoveCells(pendingUserUnit, currentMoveBudget);

            if (selectableMoveCells.Count == 0)
            {
                PlayPendingCard(
                    confirmedAttackTargetGrid,
                    confirmedAttackTargetUnit,
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
            List<Vector2Int> plannedPathSnapshot = plannedPath != null
                ? new List<Vector2Int>(plannedPath)
                : (confirmedMovePath.Count > 0 ? new List<Vector2Int>(confirmedMovePath) : null);

            CardViewHoverSystem.Instance?.Hide();
            if (playedCardView != null)
            {
                handView?.RemoveCard(playedCardView.Card);
            }

            //if (EnableMoveDebug)
            //{
            //    Debug.Log(
            //        $"[BattleMoveDebug] PlayPendingCard card={(cardToPlay != null ? cardToPlay.Title : "null")}," +
            //        $" unit={(userUnit != null ? userUnit.name : "null")}, target={targetGrid}, pathCount={(plannedPathSnapshot != null ? plannedPathSnapshot.Count : 0)}, path={BuildPathDebugText(plannedPathSnapshot)}");
            //}

            ClearTargetingState(false);

            battleCardSystem.PlayCard(
                cardToPlay,
                userUnit,
                targetGrid,
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

        private static bool RequiresMoveTargetSelection(BattleCard battleCard)
        {
            BattleCardTargetingMode targetingMode = BattleCardTargetingUtility.ResolveTargetingMode(battleCard);
            return targetingMode == BattleCardTargetingMode.MoveOnly
                || targetingMode == BattleCardTargetingMode.MoveThenAttack
                || targetingMode == BattleCardTargetingMode.AttackThenMove;
        }

        private static bool RequiresAttackTargetSelection(BattleCard battleCard)
        {
            BattleCardTargetingMode targetingMode = BattleCardTargetingUtility.ResolveTargetingMode(battleCard);
            return targetingMode == BattleCardTargetingMode.AttackOnly
                || targetingMode == BattleCardTargetingMode.MoveThenAttack
                || targetingMode == BattleCardTargetingMode.AttackThenMove;
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

        private static bool HasEffect<TEffect>(BattleCard battleCard)
            where TEffect : BattleEffect
        {
            if (battleCard?.RuntimeEffects == null)
            {
                return false;
            }

            foreach (var effect in battleCard.RuntimeEffects)
            {
                if (effect is TEffect)
                {
                    return true;
                }
            }

            return false;
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

                        // 이제는 "이동 후 공격" 카드도 공격 대상이 없으면 이동만 가능하므로
                        // 현재 위치에서 공격 가능하거나, 최소한 한 칸이라도 이동 가능하면 선택 가능하게 둡니다.
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

        private static bool IsAreaAttack(BattleCard battleCard)
        {
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return false;
            }

            if (attackEffect.HitsAllTargetsInRange || attackEffect.AttackPattern == BattleAttackPattern.Area)
            {
                return true;
            }

            return attackEffect.CustomAttackPattern != null && attackEffect.CustomAttackPattern.Cells.Count > 1;
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
                //if (EnableMoveDebug)
                //{
                //    Debug.Log(
                //        $"[BattleUI] 드래그 경로 확장 실패: start={segmentStart}, hovered={hoveredGrid}, remainingBudget={remainingBudget}");
                //}
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
            //if (EnableMoveDebug)
            //{
            //    Debug.Log(
            //        $"[BattleMoveDebug] RefreshMovePreview unit={(pendingUserUnit != null ? pendingUserUnit.name : "null")}, budget={currentMoveBudget}, cost={CalculateDrawnPathCost()}, pathCount={drawnMovePath.Count}, path={BuildPathDebugText(drawnMovePath)}, analysis={AnalyzePathDebug(drawnMovePath)}");
            //}

            gridPreviewSystem?.ShowMoveCells(selectableMoveCells);
            gridPreviewSystem?.ShowHoverCellBorder(
                hasLastMoveHoverCell && selectableMoveCells.Contains(lastHoveredMoveCell)
                    ? lastHoveredMoveCell
                    : (Vector2Int?)null);
            gridPreviewSystem?.ShowUnitHighlights(new[] { pendingUserUnit });
            gridPreviewSystem?.ShowImpactUnitBorders(null);

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
                // 경로를 그리는 동안에도 최종 도착 예정 칸 기준으로
                // 다음 공격 가능 셀을 같이 보여줘서 SRPG식 흐름을 유지합니다.
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

        private void RefreshAttackPreview(Vector2Int? hoveredGrid)
        {
            bool isGroundTargetAttack = IsGroundTargetAttack(pendingBattleCard);
            gridPreviewSystem?.ShowMoveCells(null);
            gridPreviewSystem?.ShowUnitHighlights(new[] { pendingUserUnit });
            gridPreviewSystem?.ShowHoverCellBorder(null);
            gridPreviewSystem?.ShowAttackCells(isGroundTargetAttack ? selectableAttackCells : null);

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

            if (!isGroundTargetAttack)
            {
                gridPreviewSystem?.ShowAttackCells(null);
            }

            gridPreviewSystem?.ShowAttackImpactCells(null);
            gridPreviewSystem?.ShowImpactUnitBorders(null);
            gridPreviewSystem?.ShowHoverCellBorder(null);
        }

        private bool IsValidAttackHover(Vector2Int hoveredGrid, BattleUnit hoveredUnit)
        {
            if (!selectableAttackCells.Contains(hoveredGrid))
            {
                return false;
            }

            if (IsGroundTargetAttack(pendingBattleCard))
            {
                return true;
            }

            List<BattleUnit> previewTargets = ResolvePreviewAttackTargets(hoveredGrid);
            if (previewTargets.Count == 0)
            {
                return false;
            }

            return hoveredUnit != null
                && hoveredUnit.IsAlive
                && hoveredUnit.Team == BattleTeam.Enemy
                && previewTargets.Contains(hoveredUnit);
        }

        private static bool IsGroundTargetAttack(BattleCard battleCard)
        {
            BattleAttackEffect attackEffect = BattleEffectResolver.GetAttackEffect(battleCard);
            if (attackEffect == null)
            {
                return false;
            }

            return attackEffect.CustomAttackPattern != null
                || attackEffect.AttackPattern == BattleAttackPattern.Area
                || attackEffect.AttackPattern == BattleAttackPattern.Line
                || attackEffect.AttackPattern == BattleAttackPattern.Adjacent4;
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

            if (attackEffect.CustomAttackPattern != null)
            {
                return BattleBoardSystem.Instance.ResolvePatternCells(
                    attackOrigin,
                    targetGrid,
                    attackEffect.CustomAttackPattern);
            }

            switch (attackEffect.AttackPattern)
            {
                case BattleAttackPattern.Area:
                    AddDiamondCells(targetGrid, attackEffect.Range, result);
                    break;

                case BattleAttackPattern.Line:
                    AddLineCellsTowardsTarget(attackOrigin, targetGrid, attackEffect.Range, result);
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

            HashSet<Vector2Int> candidateCells = boardSystem.GetSelectableAttackCells(attacker, attackOrigin, battleCard);
            if (IsGroundTargetAttack(battleCard))
            {
                return candidateCells;
            }

            foreach (Vector2Int candidateCell in candidateCells)
            {
                // 단순 사거리 셀이 아니라 "이 칸을 고르면 최소 한 명은 맞는가"를 기준으로
                // 실제 선택 가능한 공격 타일만 남깁니다.
                if (ResolvePreviewAttackTargets(boardSystem, attacker, attackOrigin, battleCard, candidateCell).Count > 0)
                {
                    result.Add(candidateCell);
                }
            }

            return result;
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

            // 실제 액션을 만들기 전, 보드 판정 로직을 재사용하기 위한 미리보기용 공격 GA입니다.
            BattleAttackGA previewAttack = new(
                battleCard,
                attacker,
                null,
                targetGrid,
                0,
                attackEffect.Range,
                attackEffect.TargetCount,
                attackEffect.HitsAllTargetsInRange,
                attackEffect.AttackPattern,
                attackEffect.CustomAttackPattern);

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
            //        $"[BattleUI] MovePath 상태: unit={(pendingUserUnit != null ? pendingUserUnit.name : "null")}, currentBudget={currentMoveBudget}, currentCost={CalculateDrawnPathCost()}, pathCount={drawnMovePath.Count}, path={builder}");
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
