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

        private const bool EnableMoveDebug = true;

        private bool isResolvingEndTurnDiscard;
        private CardTargetingPhase targetingPhase = CardTargetingPhase.None;
        private BattleCard pendingBattleCard;
        private CardView pendingCardView;
        private BattleUnit pendingUserUnit;
        private List<BattleUnit> selectableUnits = new();
        private HashSet<Vector2Int> selectableMoveCells = new();
        private HashSet<Vector2Int> selectableAttackCells = new();
        private readonly List<Vector2Int> drawnMovePath = new();
        private int currentMoveBudget;
        private bool hasLastDragCell;
        private Vector2Int lastDraggedMoveCell;

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

            if (targetingPhase == CardTargetingPhase.SelectMoveTarget && Input.GetMouseButton(0))
            {
                HandleMovePathDrag(Input.mousePosition);
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

            Debug.Log($"[BattleUI] 카드 클릭: {battleCard.Title}");
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

            Debug.Log($"[BattleUI] 손패 갱신 시작: handCount={battleCardSystem.PileState.HandCount}");
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

            Debug.Log($"[BattleUI] 손패 갱신 완료: createdViews={createdCount}");
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
            pendingCardView = cardView;
            pendingUserUnit = null;
            selectableUnits = FindUsablePlayerUnits(battleCard);
            selectableMoveCells.Clear();
            selectableAttackCells.Clear();
            drawnMovePath.Clear();
            currentMoveBudget = 0;
            targetingPhase = CardTargetingPhase.SelectUnit;
            gridPreviewSystem?.Clear();
            gridPreviewSystem?.ShowUnitBorders(selectableUnits);

            CardViewHoverSystem.Instance?.Hide();
            pendingCardView?.BeginExternalSelection();
            Debug.Log($"[BattleUI] 카드 타겟팅 시작: card={battleCard.Title}, selectableUnits={selectableUnits.Count}. 먼저 아군 유닛을 선택하세요.");
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

            if (RequiresMoveTargetSelection(pendingBattleCard))
            {
                currentMoveBudget = ResolveMoveBudget(pendingBattleCard, clickedUnit);
                selectableMoveCells = BattleBoardSystem.Instance.GetSelectableMoveCells(clickedUnit, currentMoveBudget);
                drawnMovePath.Clear();
                hasLastDragCell = false;
                targetingPhase = CardTargetingPhase.SelectMoveTarget;
                gridPreviewSystem?.ShowMoveCells(selectableMoveCells);
                gridPreviewSystem?.ShowUnitHighlights(new[] { clickedUnit });
                Debug.Log($"[BattleUI] 유닛 선택 완료: unit={clickedUnit.name}, moveCells={selectableMoveCells.Count}, moveBudget={currentMoveBudget}");
                return;
            }

            if (RequiresAttackTargetSelection(pendingBattleCard))
            {
                selectableAttackCells = BattleBoardSystem.Instance.GetSelectableAttackCells(clickedUnit, pendingBattleCard);
                targetingPhase = CardTargetingPhase.SelectAttackTarget;
                gridPreviewSystem?.ShowAttackCells(selectableAttackCells);
                gridPreviewSystem?.ShowUnitHighlights(new[] { clickedUnit });
                Debug.Log($"[BattleUI] 유닛 선택 완료: unit={clickedUnit.name}, attackCells={selectableAttackCells.Count}");
                return;
            }

            PlayPendingCard(clickedUnit.GridPosition, clickedUnit, null);
        }

        private void TrySelectMoveTargetByClick(Vector2Int clickedGrid)
        {
            if (pendingBattleCard == null || pendingUserUnit == null || BattleBoardSystem.Instance == null)
            {
                CancelCardTargeting();
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
            if (drawnMovePath.Count == 0)
            {
                Debug.Log("[BattleUI] 먼저 이동 경로를 그려주세요.");
                return;
            }

            Vector2Int finalCell = drawnMovePath[drawnMovePath.Count - 1];
            PlayPendingCard(finalCell, null, drawnMovePath);
        }

        private void TrySelectAttackTarget(Vector2Int clickedGrid, BattleUnit clickedUnit)
        {
            if (pendingBattleCard == null || pendingUserUnit == null)
            {
                CancelCardTargeting();
                return;
            }

            if (!selectableAttackCells.Contains(clickedGrid))
            {
                Debug.Log("[BattleUI] 공격 가능한 범위 안의 타일/적을 선택하세요.");
                return;
            }

            bool isAreaAttack = IsAreaAttack(pendingBattleCard);
            if (!isAreaAttack)
            {
                if (clickedUnit == null || clickedUnit.Team != BattleTeam.Enemy || !clickedUnit.IsAlive)
                {
                    Debug.Log("[BattleUI] 단일 공격 카드는 범위 안의 적 유닛을 클릭해야 합니다.");
                    return;
                }
            }

            PlayPendingCard(clickedGrid, isAreaAttack ? null : clickedUnit, null);
        }

        private void PlayPendingCard(Vector2Int targetGrid, BattleUnit targetUnit, IReadOnlyList<Vector2Int> plannedPath)
        {
            if (pendingBattleCard == null || pendingUserUnit == null)
            {
                CancelCardTargeting();
                return;
            }

            BattleCard cardToPlay = pendingBattleCard;
            BattleUnit userUnit = pendingUserUnit;
            CardView playedCardView = pendingCardView;
            List<Vector2Int> plannedPathSnapshot = plannedPath != null ? new List<Vector2Int>(plannedPath) : null;

            CardViewHoverSystem.Instance?.Hide();
            if (playedCardView != null)
            {
                handView?.RemoveCard(playedCardView.Card);
            }

            if (EnableMoveDebug)
            {
                Debug.Log(
                    $"[BattleMoveDebug] PlayPendingCard card={(cardToPlay != null ? cardToPlay.Title : "null")}, unit={(userUnit != null ? userUnit.name : "null")}, target={targetGrid}, pathCount={(plannedPathSnapshot != null ? plannedPathSnapshot.Count : 0)}, path={BuildPathDebugText(plannedPathSnapshot)}");
            }

            ClearTargetingState(false);

            battleCardSystem.PlayCard(
                cardToPlay,
                userUnit,
                targetGrid,
                plannedPathSnapshot,
                targetUnit,
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
            pendingCardView = null;
            pendingUserUnit = null;
            selectableUnits.Clear();
            selectableMoveCells.Clear();
            selectableAttackCells.Clear();
            drawnMovePath.Clear();
            currentMoveBudget = 0;
            hasLastDragCell = false;
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
            return battleCard != null
                && (battleCard.CardType == BattleCardType.Move || HasEffect<BattleMoveEffect>(battleCard));
        }

        private static bool RequiresAttackTargetSelection(BattleCard battleCard)
        {
            return battleCard != null
                && (battleCard.CardType == BattleCardType.Attack || HasEffect<BattleDamageEffect>(battleCard));
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

                if (RequiresMoveTargetSelection(battleCard) && boardSystem != null)
                {
                    int moveBudget = ResolveMoveBudget(battleCard, unit);
                    HashSet<Vector2Int> moveCells = boardSystem.GetSelectableMoveCells(unit, moveBudget);

                    if (moveCells.Count == 0)
                    {
                        continue;
                    }
                }

                if (RequiresAttackTargetSelection(battleCard) && boardSystem != null)
                {
                    HashSet<Vector2Int> attackCells = boardSystem.GetSelectableAttackCells(unit, battleCard);

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
                Debug.Log(
                    $"[BattleUI] 드래그 경로 확장 실패: start={segmentStart}, hovered={hoveredGrid}, remainingBudget={remainingBudget}");
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
            if (EnableMoveDebug)
            {
                Debug.Log(
                    $"[BattleMoveDebug] RefreshMovePreview unit={(pendingUserUnit != null ? pendingUserUnit.name : "null")}, budget={currentMoveBudget}, cost={CalculateDrawnPathCost()}, pathCount={drawnMovePath.Count}, path={BuildPathDebugText(drawnMovePath)}, analysis={AnalyzePathDebug(drawnMovePath)}");
            }

            gridPreviewSystem?.ShowMoveCells(selectableMoveCells);
            gridPreviewSystem?.ShowUnitHighlights(new[] { pendingUserUnit });

            if (drawnMovePath.Count > 0)
            {
                gridPreviewSystem?.ShowPathCells(drawnMovePath);
            }
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

            Debug.Log(
                $"[BattleUI] {caller}: card={(battleCard != null ? battleCard.Title : "null")}, selectableCount={(units != null ? units.Count : 0)}, units={builder}");
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

            Debug.Log(
                $"[BattleUI] {caller}: unit={(unit != null ? unit.name : "null")}, grid={(unit != null ? unit.GridPosition.ToString() : "null")}, world={(unit != null ? unit.transform.position.ToString() : "null")}, cellCount={count}, cells=[{builder}]");
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

            Debug.Log(
                $"[BattleUI] MovePath 상태: unit={(pendingUserUnit != null ? pendingUserUnit.name : "null")}, currentBudget={currentMoveBudget}, currentCost={CalculateDrawnPathCost()}, pathCount={drawnMovePath.Count}, path={builder}");
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
