namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /*
     * BattleSessionController
     *
     * 역할:
     * - 씬 전환 대신 같은 씬 안에서 문명 모드와 전투 모드를 전환합니다.
     * - 전투 진입 전 문명 카드 상태를 백업하고, 종료 시 원래 손패/덱 상태를 복원합니다.
     */
    public class BattleSessionController : MonoBehaviour
    {
        public static BattleSessionController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BattleUIController battleUIController;

        [Header("Unit Roster")]
        [Tooltip("병종별 프리팹 매핑 에셋 (없으면 Roster 빌드를 건너뜁니다)")]
        [SerializeField] private BattleUnitPrefabRegistry prefabRegistry;

        [Header("Optional Shared Hand View")]
        [SerializeField] private HandView sharedHandView;

        [Header("Mode Roots")]
        [SerializeField] private GameObject[] civilizationModeObjects;
        [SerializeField] private GameObject[] battleModeObjects;

        [Header("Options")]
        [SerializeField] private bool restoreCivilizationStateOnBattleEnd = true;

        public bool IsBattleActive { get; private set; }

        // SaveManager에서 전투 중 저장 시 전투 진입 전 카드 상태를 가져올 때 사용
        public CardPileRuntimeState GetSavedCivilizationState() => savedCivilizationState;

        private CardPileRuntimeState savedCivilizationState;
        private GameObject deckButtonObject;
        private GameObject discardButtonObject;
        private Text deckButtonText;
        private Text discardButtonText;
        private TMP_Text deckButtonTmpText;
        private TMP_Text discardButtonTmpText;

        private NodeData currentPlayerNode;
        private NodeData currentEnemyNode;

        private void Awake()
        {
            Instance = this;

            if (battleManager == null)
            {
                battleManager = FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);
            }

            if (battleUIController == null)
            {
                battleUIController = FindFirstObjectByType<BattleUIController>(FindObjectsInactive.Include);
            }

            if (sharedHandView == null)
            {
                sharedHandView = FindFirstObjectByType<HandView>(FindObjectsInactive.Include);
            }

            CacheDeckViewButtons();
            RefreshDeckViewButtonLabels();
            SetObjectsActive(battleModeObjects, false);
        }

        private void OnEnable()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleFinished += HandleBattleFinished;
            }
        }

        private void OnDisable()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleFinished -= HandleBattleFinished;
            }
        }

        public void EnterBattle()
        {
            EnterBattle(null, null);
        }

        // NodeData를 지정해 전투 진입 — 월드맵에서 공격 시 호출
        // playerNode: 플레이어 유닛이 있는 노드
        // enemyNode: 공격 대상(적) 노드
        public void EnterBattle(NodeData playerNode, NodeData enemyNode, bool isPlayerAttacker = true)
        {
            if (IsBattleActive)
            {
                Debug.LogWarning("[BattleSession] 이미 전투 모드입니다.");
                return;
            }

            currentPlayerNode = playerNode;
            currentEnemyNode = enemyNode;

            if (CardSystem.Instance == null)
            {
                Debug.LogWarning("[BattleSession] CardSystem이 없어 문명 카드 상태를 백업할 수 없습니다.");
                return;
            }

            if (battleManager == null)
            {
                Debug.LogWarning("[BattleSession] BattleManager가 없어 전투를 시작할 수 없습니다.");
                return;
            }

            savedCivilizationState = CardSystem.Instance.CaptureRuntimeState();
            Debug.Log($"[BattleSession] 문명 상태 백업 완료: {FormatState(savedCivilizationState)}");

            if (sharedHandView != null)
            {
                sharedHandView.ClearAllCardsImmediate();
            }

            SetObjectsActive(civilizationModeObjects, false);
            SetObjectsActive(battleModeObjects, true);

            if (battleUIController != null)
            {
                battleUIController.ClearSharedHandView();
            }

            IsBattleActive = true;
            RefreshDeckViewButtonLabels();

            // 유닛 로스터 구성
            BattleStartContext context = new BattleStartContext
            {
                StartWithMulligan = true,
                IsPlayerAttacker = isPlayerAttacker
            };

            if (prefabRegistry != null)
            {
                if (playerNode != null)
                {
                    context.PlayerRoster = BattleUnitRosterBuilder.Build(
                        playerNode, BattleTeam.Player, 0, prefabRegistry);
                }

                if (enemyNode != null)
                {
                    context.EnemyRoster = BattleUnitRosterBuilder.Build(
                        enemyNode, BattleTeam.Enemy,
                        enemyNode.ownerCivID, prefabRegistry);
                }
            }

            battleManager.SetupBattle(context);

            if (battleUIController != null)
            {
                battleUIController.RefreshBattlePresentation();
            }
        }

        public void ExitBattle()
        {
            if (!IsBattleActive)
            {
                return;
            }

            if (battleUIController != null)
            {
                battleUIController.ClearSharedHandView();
            }
            else if (sharedHandView != null)
            {
                sharedHandView.ClearAllCardsImmediate();
            }

            // [추가] 전투에 사용된 모든 BattleUnit 오브젝트 파괴
            BattleUnit[] allUnits = FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);
            foreach (BattleUnit unit in allUnits)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }

            SetObjectsActive(battleModeObjects, false);
            SetObjectsActive(civilizationModeObjects, true);

            if (restoreCivilizationStateOnBattleEnd && CardSystem.Instance != null && savedCivilizationState != null)
            {
                bool restored = CardSystem.Instance.RestoreRuntimeState(savedCivilizationState);
                Debug.Log($"[BattleSession] 문명 상태 복원 결과: restored={restored}, state={FormatState(savedCivilizationState)}");
            }

            if (battleManager != null)
            {
                battleManager.ResetBattleState();
            }

            savedCivilizationState = null;
            currentPlayerNode = null;
            currentEnemyNode = null;
            IsBattleActive = false;
            RefreshDeckViewButtonLabels();
        }

        private void HandleBattleFinished(BattleResult result)
        {
            // 전투 결과를 월드맵 노드에 반영 (생존 유닛 반환)
            if (currentPlayerNode != null && result.SurvivingPlayerTroops != null)
            {
                currentPlayerNode.units.Clear();
                foreach (var kvp in result.SurvivingPlayerTroops)
                {
                    if (kvp.Value > 0)
                    {
                        currentPlayerNode.units.Add(new NodeUnit { unitType = kvp.Key, count = kvp.Value });
                    }
                }
            }

            if (currentEnemyNode != null && result.SurvivingEnemyTroops != null)
            {
                currentEnemyNode.units.Clear();
                foreach (var kvp in result.SurvivingEnemyTroops)
                {
                    if (kvp.Value > 0)
                    {
                        currentEnemyNode.units.Add(new NodeUnit { unitType = kvp.Key, count = kvp.Value });
                    }
                }

                // 플레이어 승리 시 노드 소유권 빼앗기 (임시 처리, 문명 매니저 연결에 따라 다름)
                if (result.IsVictory)
                {
                    currentEnemyNode.ownerCivID = 0; // 플레이어 소유
                }
            }

            ExitBattle();
        }

        private static void SetObjectsActive(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }

        private void CacheDeckViewButtons()
        {
            deckButtonObject = GameObject.Find("DeckCardViewButton");
            discardButtonObject = GameObject.Find("DisCardViewButton");

            if (deckButtonObject != null)
            {
                deckButtonText = deckButtonObject.GetComponentInChildren<Text>(true);
                deckButtonTmpText = deckButtonObject.GetComponentInChildren<TMP_Text>(true);
            }

            if (discardButtonObject != null)
            {
                discardButtonText = discardButtonObject.GetComponentInChildren<Text>(true);
                discardButtonTmpText = discardButtonObject.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void RefreshDeckViewButtonLabels()
        {
            if (deckButtonObject == null && discardButtonObject == null)
            {
                CacheDeckViewButtons();
            }

            string deckLabel = IsBattleActive ? "전투 덱" : "덱";
            string discardLabel = IsBattleActive ? "전투 버림" : "버림";

            if (deckButtonText != null)
            {
                deckButtonText.text = deckLabel;
            }

            if (deckButtonTmpText != null)
            {
                deckButtonTmpText.text = deckLabel;
            }

            if (discardButtonText != null)
            {
                discardButtonText.text = discardLabel;
            }

            if (discardButtonTmpText != null)
            {
                discardButtonTmpText.text = discardLabel;
            }
        }

        private static string FormatState(CardPileRuntimeState state)
        {
            if (state == null)
            {
                return "state=NULL";
            }

            int draw = state.DrawPile != null ? state.DrawPile.Count : 0;
            int hand = state.Hand != null ? state.Hand.Count : 0;
            int discard = state.DiscardPile != null ? state.DiscardPile.Count : 0;
            int extinction = state.ExtinctionPile != null ? state.ExtinctionPile.Count : 0;
            int total = draw + hand + discard + extinction;
            return $"draw={draw}, hand={hand}, discard={discard}, extinction={extinction}, total={total}";
        }
    }
}
