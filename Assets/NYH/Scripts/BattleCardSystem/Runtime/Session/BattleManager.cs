namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    // 전투 전체의 큰 상태 흐름을 표현합니다.
    // 세부 카드 실행, 보드 계산, AI 행동은 각각 전용 시스템으로 위임됩니다.
    public enum BattlePhase
    {
        None,
        Setup,
        Mulligan,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat,
        Finished,
    }

    [System.Serializable]
    // 전투 시작 시 외부에서 넘길 수 있는 옵션입니다.
    public class BattleStartContext
    {
        public bool StartWithMulligan = true;

        // 전투에 투입할 유닛 로스터 (null이면 씬에 미리 배치된 유닛 또는 fallback 사용)
        public BattleUnitRoster PlayerRoster;
        public BattleUnitRoster EnemyRoster;
    }

    [System.Serializable]
    // 전투 종료 후 보상/씬 전환 쪽에서 사용할 결과 요약입니다.
    public class BattleResult
    {
        public bool IsVictory;
        public int TurnCount;
        public int SurvivingPlayerUnits;
        public int SurvivingEnemyUnits;
        public Dictionary<UnitType, int> SurvivingPlayerTroops;
        public Dictionary<UnitType, int> SurvivingEnemyTroops;
        public BattleTeam Winner;
    }

    /*
     * BattleManager
     *
     * 역할:
     * - 전투 시작, 멀리건, 플레이어 턴, 적 턴, 승패 판정을 순서대로 조율합니다.
     * - 손패 변화 이벤트와 전투 종료 이벤트를 발행해 UI/세션 컨트롤러가 반응하게 합니다.
     *
     * 담당하지 않는 것:
     * - 카드 비용 지불과 카드 더미 조작은 BattleCardSystem이 담당합니다.
     * - 이동/공격 가능 칸 계산은 BattleBoardSystem과 query service가 담당합니다.
     * - 적 유닛의 실제 판단과 행동은 BattleEnemyAIController가 담당합니다.
     */
    public class BattleManager : MonoBehaviour
    {
        [Header("Battle References")]
        [SerializeField] private BattleCardSystem battleCardSystem;
        [SerializeField] private BattleEnemyAIController enemyAIController;
        [SerializeField] private BattleStartSpawner battleStartSpawner;


        [Header("Battle Setup")]
        [SerializeField] private BattleStartContext defaultStartContext = new();
        [SerializeField] private bool autoStartOnSceneLoad = true;

        public int BattleTurn { get; private set; }
        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.None;
        public BattleTeam CurrentTurnTeam { get; private set; } = BattleTeam.Player;
        public bool IsBattleEnded { get; private set; }
        public bool IsMulliganPhase { get; private set; }
        public BattleResult LastResult { get; private set; }

        public event System.Action<BattlePhase> OnPhaseChanged;
        public event System.Action<int, BattleTeam> OnTurnStarted;
        public event System.Action<BattleResult> OnBattleFinished;
        public event System.Action OnHandStateChanged;

        private readonly List<BattleUnit> playerUnits = new();
        private readonly List<BattleUnit> enemyUnits = new();
        private bool hasOpeningHandPrepared;

        private void Awake()
        {
            if (battleCardSystem == null)
            {
                battleCardSystem = BattleCardSystem.Instance;
            }

            if (enemyAIController == null)
            {
                enemyAIController = FindFirstObjectByType<BattleEnemyAIController>(FindObjectsInactive.Include);
            }

            if (battleStartSpawner == null)
            {
                battleStartSpawner = FindFirstObjectByType<BattleStartSpawner>(FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            if (enemyAIController != null)
            {
                enemyAIController.OnAITurnFinished += EndEnemyTurn;
            }
        }

        private void OnDisable()
        {
            if (enemyAIController != null)
            {
                enemyAIController.OnAITurnFinished -= EndEnemyTurn;
            }
        }

        private void Start()
        {
            EnsureBattleCardSystem("Start");
            if (autoStartOnSceneLoad && CurrentPhase == BattlePhase.None && !IsBattleEnded)
            {
                StartBattle();
            }
        }

        private bool EnsureBattleCardSystem(string caller)
        {
            if (battleCardSystem == null)
            {
                battleCardSystem = BattleCardSystem.Instance;
            }

            if (battleCardSystem == null)
            {
                return false;
            }

            return true;
        }

        // 전투 시작 전 내부 상태와 카드 더미를 준비합니다.
        public void SetupBattle(BattleStartContext context = null)
        {
            if (!EnsureBattleCardSystem("SetupBattle"))
            {
                return;
            }

            BattleStartContext resolvedContext = context ?? defaultStartContext ?? new BattleStartContext();
            ResetBattleState();
            SetPhase(BattlePhase.Setup);

            // 유닛 스폰: Roster가 있으면 Roster 기반, 없으면 fallback/씬 배치 유닛 사용
            if (battleStartSpawner != null)
            {
                battleStartSpawner.SpawnAll(resolvedContext.PlayerRoster, resolvedContext.EnemyRoster);
            }

            RebuildUnitLists();

            battleCardSystem.SetupFromInspector();
            battleCardSystem.SetupActionPoints(0);

            if (resolvedContext.StartWithMulligan)
            {
                StartMulligan();
            }
            else
            {
                DrawOpeningHand();
                StartPlayerTurn();
            }
        }

        // 턴/페이즈/결과/유닛 캐시를 초기 상태로 되돌립니다.
        public void ResetBattleState()
        {
            BattleTurn = 0;
            CurrentTurnTeam = BattleTeam.Player;
            IsBattleEnded = false;
            IsMulliganPhase = false;
            hasOpeningHandPrepared = false;
            LastResult = null;
            BattleTrapSystem.ClearInstalledTraps();
            SetPhase(BattlePhase.None);
        }

        // 설정된 시작 옵션에 따라 멀리건 또는 플레이어 턴으로 전투를 시작합니다.
        public void StartBattle()
        {
            if (!EnsureBattleCardSystem("StartBattle"))
            {
                return;
            }

            SetupBattle(defaultStartContext);
        }

        // 시작 손패를 뽑고 멀리건 페이즈로 전환합니다.
        public void StartMulligan()
        {
            if (IsBattleEnded)
            {
                return;
            }

            IsMulliganPhase = true;
            SetPhase(BattlePhase.Mulligan);
            DrawOpeningHand();
        }

        // 멀리건 선택을 확정하고 되돌린 카드 수만큼 새 카드를 뽑습니다.
        public BattleMulliganResult ConfirmMulligan(IReadOnlyList<BattleCard> selectedCards = null)
        {
            if (!IsMulliganPhase || IsBattleEnded || battleCardSystem == null)
            {
                return null;
            }

            IsMulliganPhase = false;
            return battleCardSystem.MulliganSelectedCards(selectedCards);
        }

        // 멀리건 종료 후 첫 플레이어 턴으로 진입합니다.
        public void StartPlayerTurnAfterMulligan()
        {
            if (!IsBattleEnded)
            {
                StartPlayerTurn();
            }
        }

        // 플레이어 턴을 시작하고 AP/드로우/종료 판정을 처리합니다.
        public void StartPlayerTurn()
        {
            if (!EnsureBattleCardSystem("StartPlayerTurn") || IsBattleEnded)
            {
                return;
            }

            RebuildUnitLists();
            if (CheckBattleEnd())
            {
                return;
            }

            BattleTurn++;
            CurrentTurnTeam = BattleTeam.Player;
            SetPhase(BattlePhase.PlayerTurn);
            battleCardSystem.GainTurnActionPoints(BattleTurn);

            if (hasOpeningHandPrepared)
            {
                hasOpeningHandPrepared = false;
            }
            else if (!IsMulliganPhase)
            {
                DrawTurnCards();
            }

            OnTurnStarted?.Invoke(BattleTurn, CurrentTurnTeam);
        }

        // 플레이어 턴을 끝내고 손패 정리 후 적 턴으로 넘깁니다.
        public void EndPlayerTurn()
        {
            if (!EnsureBattleCardSystem("EndPlayerTurn"))
            {
                return;
            }

            if (IsBattleEnded || CurrentPhase != BattlePhase.PlayerTurn)
            {
                return;
            }

            battleCardSystem.EndTurnDiscardHand();
            NotifyHandStateChanged();

            if (CheckBattleEnd())
            {
                return;
            }

            StartEnemyTurn();
        }

        // 적 턴을 시작하고 EnemyAIController에 행동 실행을 요청합니다.
        public void StartEnemyTurn()
        {
            if (IsBattleEnded)
            {
                return;
            }

            RebuildUnitLists();
            if (CheckBattleEnd())
            {
                return;
            }

            CurrentTurnTeam = BattleTeam.Enemy;
            SetPhase(BattlePhase.EnemyTurn);
            OnTurnStarted?.Invoke(BattleTurn, CurrentTurnTeam);

            if (enemyAIController != null)
            {
                enemyAIController.ExecuteTurnAsync().Forget();
            }
            else
            {
                Debug.LogWarning("[BattleManager] BattleEnemyAIController가 없어 적 턴을 즉시 종료합니다.");
                EndEnemyTurn();
            }
        }

        // 적 AI 실행 완료 콜백에서 플레이어 턴으로 돌아갑니다.
        public void EndEnemyTurn()
        {
            if (IsBattleEnded || CurrentPhase != BattlePhase.EnemyTurn)
            {
                return;
            }

            if (CheckBattleEnd())
            {
                return;
            }

            StartPlayerTurn();
        }

        // 전투 시작 손패 규칙에 맞춰 카드를 뽑고 UI에 알립니다.
        public void DrawOpeningHand()
        {
            if (!EnsureBattleCardSystem("DrawOpeningHand"))
            {
                return;
            }

            int drawCount = GetAlivePlayerUnitTypeCount();
            battleCardSystem.DrawOpeningHand(drawCount + 3); // 시작시만 유닛 타입 수 +3장 뽑기
            hasOpeningHandPrepared = true;
            NotifyHandStateChanged();
        }

        // 턴 시작 드로우 규칙에 맞춰 카드를 뽑고 UI에 알립니다.
        public void DrawTurnCards()
        {
            if (!EnsureBattleCardSystem("DrawTurnCards"))
            {
                return;
            }

            int drawCount = GetAlivePlayerUnitTypeCount();
            battleCardSystem.DrawTurnCards(drawCount);
            NotifyHandStateChanged();
        }

        // 살아 있는 플레이어 유닛의 병종 종류 수를 계산합니다.
        public int GetAlivePlayerUnitTypeCount()
        {
            RebuildUnitLists();

            HashSet<string> aliveUnitTypes = new();
            foreach (BattleUnit unit in playerUnits)
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                string unitTypeKey = string.IsNullOrEmpty(unit.UnitId) ? unit.name : unit.UnitId;
                aliveUnitTypes.Add(unitTypeKey);
            }

            return aliveUnitTypes.Count;
        }

        // 아군/적 생존 상태를 보고 승리 또는 패배 처리 여부를 결정합니다.
        public bool CheckBattleEnd()
        {
            RebuildUnitLists();

            bool playerAlive = HasAliveUnits(playerUnits);
            bool enemyAlive = HasAliveUnits(enemyUnits);
            if (!playerAlive)
            {
                HandleDefeat();
                return true;
            }

            if (!enemyAlive)
            {
                HandleVictory();
                return true;
            }

            return false;
        }

        // 승리 결과를 만들고 전투 종료 이벤트를 발행합니다.
        public void HandleVictory()
        {
            if (IsBattleEnded)
            {
                return;
            }

            SetPhase(BattlePhase.Victory);
            FinishBattle(BuildResult(true));
        }

        // 패배 결과를 만들고 전투 종료 이벤트를 발행합니다.
        public void HandleDefeat()
        {
            if (IsBattleEnded)
            {
                return;
            }

            SetPhase(BattlePhase.Defeat);
            FinishBattle(BuildResult(false));
        }

        // 최종 전투 결과를 저장하고 Finished 페이즈로 전환합니다.
        public void FinishBattle(BattleResult result)
        {
            if (IsBattleEnded)
            {
                return;
            }

            bool hasPlayerUnits = HasAliveUnits(playerUnits);
            result.Winner = hasPlayerUnits ? BattleTeam.Player : BattleTeam.Enemy;
            result.IsVictory = (result.Winner == BattleTeam.Player);

            IsBattleEnded = true;
            LastResult = result;
            BattleTrapSystem.ClearInstalledTraps();
            SetPhase(BattlePhase.Finished);
            OnBattleFinished?.Invoke(result);
        }

        private BattleResult BuildResult(bool isVictory)
        {
            RebuildUnitLists();
            var result = new BattleResult
            {
                IsVictory = isVictory,
                TurnCount = BattleTurn,
                SurvivingPlayerUnits = CountAliveUnits(playerUnits),
                SurvivingEnemyUnits = CountAliveUnits(enemyUnits),
            };
            result.SurvivingPlayerTroops = new Dictionary<UnitType, int>();
            foreach (BattleUnit unit in playerUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (!result.SurvivingPlayerTroops.ContainsKey(unit.UnitType))
                    result.SurvivingPlayerTroops[unit.UnitType] = 0;

                int survivingCount = Mathf.CeilToInt((float)unit.CurrentHealth / unit.BaseHealthPerUnit);
                result.SurvivingPlayerTroops[unit.UnitType] += survivingCount;
            }

            result.SurvivingEnemyTroops = new Dictionary<UnitType, int>();
            foreach (BattleUnit unit in enemyUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (!result.SurvivingEnemyTroops.ContainsKey(unit.UnitType))
                    result.SurvivingEnemyTroops[unit.UnitType] = 0;

                int survivingCount = Mathf.CeilToInt((float)unit.CurrentHealth / unit.BaseHealthPerUnit);
                result.SurvivingEnemyTroops[unit.UnitType] += survivingCount;
            }

            return result;
        }

        private void RebuildUnitLists()
        {
            playerUnits.Clear();
            enemyUnits.Clear();

            BattleUnit[] allUnits = FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);
            foreach (BattleUnit unit in allUnits)
            {
                if (unit == null)
                {
                    continue;
                }

                if (unit.Team == BattleTeam.Player)
                {
                    playerUnits.Add(unit);
                }
                else
                {
                    enemyUnits.Add(unit);
                }
            }
        }

        private void SetPhase(BattlePhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private static bool HasAliveUnits(List<BattleUnit> units)
        {
            foreach (BattleUnit unit in units)
            {
                if (unit != null && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountAliveUnits(List<BattleUnit> units)
        {
            int count = 0;
            foreach (BattleUnit unit in units)
            {
                if (unit != null && unit.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private void NotifyHandStateChanged()
        {
            OnHandStateChanged?.Invoke();
        }
    }
}
