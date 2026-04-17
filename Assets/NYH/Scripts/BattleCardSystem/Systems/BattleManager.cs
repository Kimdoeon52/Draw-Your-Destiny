namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

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
    public class BattleStartContext
    {
        public bool StartWithMulligan = true;
    }

    [System.Serializable]
    public class BattleResult
    {
        public bool IsVictory;
        public int TurnCount;
        public int SurvivingPlayerUnits;
        public int SurvivingEnemyUnits;
    }

    public class BattleManager : MonoBehaviour
    {
        [Header("Battle References")]
        [SerializeField] private BattleCardSystem battleCardSystem;
        [SerializeField] private BattleEnemyAIController enemyAIController;

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
                Debug.LogWarning($"[BattleManager] BattleCardSystem이 없어 진행할 수 없습니다. caller={caller}");
                return false;
            }

            return true;
        }

        public void SetupBattle(BattleStartContext context = null)
        {
            if (!EnsureBattleCardSystem("SetupBattle"))
            {
                return;
            }

            BattleStartContext resolvedContext = context ?? defaultStartContext ?? new BattleStartContext();
            ResetBattleState();
            RebuildUnitLists();
            SetPhase(BattlePhase.Setup);

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

        public void ResetBattleState()
        {
            BattleTurn = 0;
            CurrentTurnTeam = BattleTeam.Player;
            IsBattleEnded = false;
            IsMulliganPhase = false;
            hasOpeningHandPrepared = false;
            LastResult = null;
            SetPhase(BattlePhase.None);
        }

        public void StartBattle()
        {
            if (!EnsureBattleCardSystem("StartBattle"))
            {
                return;
            }

            SetupBattle(defaultStartContext);
        }

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

        public BattleMulliganResult ConfirmMulligan(IReadOnlyList<BattleCard> selectedCards = null)
        {
            if (!IsMulliganPhase || IsBattleEnded || battleCardSystem == null)
            {
                return null;
            }

            IsMulliganPhase = false;
            return battleCardSystem.MulliganSelectedCards(selectedCards);
        }

        public void StartPlayerTurnAfterMulligan()
        {
            if (!IsBattleEnded)
            {
                StartPlayerTurn();
            }
        }

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

        public void DrawOpeningHand()
        {
            if (!EnsureBattleCardSystem("DrawOpeningHand"))
            {
                return;
            }

            int drawCount = GetAlivePlayerUnitTypeCount();
            battleCardSystem.DrawOpeningHand(drawCount);
            hasOpeningHandPrepared = true;
            NotifyHandStateChanged();
        }

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

        public void HandleVictory()
        {
            if (IsBattleEnded)
            {
                return;
            }

            SetPhase(BattlePhase.Victory);
            FinishBattle(BuildResult(true));
        }

        public void HandleDefeat()
        {
            if (IsBattleEnded)
            {
                return;
            }

            SetPhase(BattlePhase.Defeat);
            FinishBattle(BuildResult(false));
        }

        public void FinishBattle(BattleResult result)
        {
            if (IsBattleEnded)
            {
                return;
            }

            IsBattleEnded = true;
            LastResult = result;
            SetPhase(BattlePhase.Finished);
            OnBattleFinished?.Invoke(result);
        }

        private BattleResult BuildResult(bool isVictory)
        {
            RebuildUnitLists();
            return new BattleResult
            {
                IsVictory = isVictory,
                TurnCount = BattleTurn,
                SurvivingPlayerUnits = CountAliveUnits(playerUnits),
                SurvivingEnemyUnits = CountAliveUnits(enemyUnits),
            };
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

