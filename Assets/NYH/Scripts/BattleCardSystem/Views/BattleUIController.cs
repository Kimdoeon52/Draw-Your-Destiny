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
        private enum AttackTargetingPhase
        {
            None,
            SelectAttacker,
            SelectTarget,
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

        private bool isResolvingEndTurnDiscard;
        private AttackTargetingPhase attackTargetingPhase = AttackTargetingPhase.None;
        private BattleCard pendingAttackCard;
        private BattleUnit pendingAttackerUnit;
        private HashSet<Vector2Int> selectableAttackCells = new();

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
            if (attackTargetingPhase == AttackTargetingPhase.None)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelAttackTargeting();
                RefreshHandView();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleAttackTargetingClick(Input.mousePosition);
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

        public void HandleBattleCardClicked(BattleCard battleCard)
        {
            if (battleCard == null)
            {
                return;
            }

            Debug.Log($"[BattleUI] 카드 클릭: {battleCard.Title}");
        }

        public bool HandleBattleCardReleased(BattleCard battleCard, Vector2 screenPosition, bool wasDragged)
        {
            if (battleCard == null || battleManager == null || battleCardSystem == null)
            {
                return false;
            }

            if (!wasDragged)
            {
                HandleBattleCardClicked(battleCard);
                return false;
            }

            if (battleManager.CurrentPhase != BattlePhase.PlayerTurn || battleManager.IsBattleEnded)
            {
                Debug.LogWarning($"[BattleUI] 카드를 사용할 수 없는 상태입니다: phase={battleManager.CurrentPhase}, ended={battleManager.IsBattleEnded}");
                return false;
            }

            if (battleCard.CardType == BattleCardType.Attack)
            {
                BeginAttackTargeting(battleCard);
                return false;
            }

            BattleUnit userUnit = FindFirstAlivePlayerUnit();
            if (userUnit == null)
            {
                Debug.LogWarning("[BattleUI] 살아있는 플레이어 유닛이 없어 카드를 사용할 수 없습니다.");
                return false;
            }

            Vector2Int targetPosition = ResolveTargetGridPosition(screenPosition);
            BattleUnit targetUnit = BattleBoardSystem.Instance != null
                ? BattleBoardSystem.Instance.GetUnitAt(targetPosition)
                : null;

            Debug.Log($"[BattleUI] 카드 드롭 사용 시도: card={battleCard.Title}, user={userUnit.name}, targetPos={targetPosition}, targetUnit={(targetUnit != null ? targetUnit.name : "null")}");

            battleCardSystem.PlayCard(
                battleCard,
                userUnit,
                targetPosition,
                targetUnit,
                () =>
                {
                    battleManager.CheckBattleEnd();
                    RefreshHandView();
                    RefreshHud();
                });

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
                CancelAttackTargeting();
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
            CancelAttackTargeting();
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

        private void BeginAttackTargeting(BattleCard battleCard)
        {
            if (battleCard == null)
            {
                return;
            }

            pendingAttackCard = battleCard;
            pendingAttackerUnit = null;
            selectableAttackCells.Clear();
            attackTargetingPhase = AttackTargetingPhase.SelectAttacker;
            gridPreviewSystem?.Clear();
            Debug.Log($"[BattleUI] 공격 카드 타겟팅 시작: card={battleCard.Title}. 먼저 아군 유닛을 선택하세요.");
        }

        private void HandleAttackTargetingClick(Vector2 screenPosition)
        {
            if (battleManager == null || battleManager.CurrentPhase != BattlePhase.PlayerTurn || battleManager.IsBattleEnded)
            {
                CancelAttackTargeting();
                return;
            }

            Vector2Int clickedGrid = ResolveTargetGridPosition(screenPosition);
            BattleUnit clickedUnit = BattleBoardSystem.Instance != null
                ? BattleBoardSystem.Instance.GetUnitAt(clickedGrid)
                : null;

            if (attackTargetingPhase == AttackTargetingPhase.SelectAttacker)
            {
                TrySelectAttacker(clickedUnit);
                return;
            }

            if (attackTargetingPhase == AttackTargetingPhase.SelectTarget)
            {
                TrySelectAttackTarget(clickedGrid, clickedUnit);
            }
        }

        private void TrySelectAttacker(BattleUnit clickedUnit)
        {
            if (clickedUnit == null || clickedUnit.Team != BattleTeam.Player || !clickedUnit.IsAlive)
            {
                Debug.Log("[BattleUI] 공격 주체로 사용할 아군 유닛을 클릭하세요.");
                return;
            }

            if (BattleBoardSystem.Instance == null || pendingAttackCard == null)
            {
                CancelAttackTargeting();
                return;
            }

            pendingAttackerUnit = clickedUnit;
            selectableAttackCells = BattleBoardSystem.Instance.GetSelectableAttackCells(clickedUnit, pendingAttackCard);
            attackTargetingPhase = AttackTargetingPhase.SelectTarget;
            gridPreviewSystem?.ShowCells(selectableAttackCells);

            Debug.Log($"[BattleUI] 공격 유닛 선택 완료: unit={clickedUnit.name}, selectableCells={selectableAttackCells.Count}");
        }

        private void TrySelectAttackTarget(Vector2Int clickedGrid, BattleUnit clickedUnit)
        {
            if (pendingAttackCard == null || pendingAttackerUnit == null)
            {
                CancelAttackTargeting();
                return;
            }

            if (!selectableAttackCells.Contains(clickedGrid))
            {
                Debug.Log("[BattleUI] 공격 가능한 범위 안의 타일/적을 선택하세요.");
                return;
            }

            bool isAreaAttack = IsAreaAttack(pendingAttackCard);
            if (!isAreaAttack)
            {
                if (clickedUnit == null || clickedUnit.Team != BattleTeam.Enemy || !clickedUnit.IsAlive)
                {
                    Debug.Log("[BattleUI] 단일 공격 카드는 범위 안의 적 유닛을 클릭해야 합니다.");
                    return;
                }
            }

            BattleCard cardToPlay = pendingAttackCard;
            BattleUnit attackerToUse = pendingAttackerUnit;
            BattleUnit targetUnit = isAreaAttack ? null : clickedUnit;

            CancelAttackTargeting();

            battleCardSystem.PlayCard(
                cardToPlay,
                attackerToUse,
                clickedGrid,
                targetUnit,
                () =>
                {
                    battleManager.CheckBattleEnd();
                    RefreshHandView();
                    RefreshHud();
                });
        }

        private void CancelAttackTargeting()
        {
            attackTargetingPhase = AttackTargetingPhase.None;
            pendingAttackCard = null;
            pendingAttackerUnit = null;
            selectableAttackCells.Clear();
            gridPreviewSystem?.Clear();
        }

        private static bool IsAreaAttack(BattleCard battleCard)
        {
            if (battleCard == null)
            {
                return false;
            }

            if (battleCard.HitsAllTargetsInRange || battleCard.AttackPattern == BattleAttackPattern.Area)
            {
                return true;
            }

            return battleCard.CustomAttackPattern != null && battleCard.CustomAttackPattern.Cells.Count > 1;
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
