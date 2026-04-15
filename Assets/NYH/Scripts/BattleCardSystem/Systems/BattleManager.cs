namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
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

    /*
     * BattleManager
     *
     * Coordinates the battle flow for the scene.
     * Handles setup, mulligan, turn changes, and battle results.
     *
     * Current scope:
     * - Board state and turn sequencing
     * - Enemy AI and richer UI hooks can be layered on later
     */
    public class BattleManager : MonoBehaviour
    {
        [Header("Battle References")]
        [SerializeField] private BattleCardSystem battleCardSystem;

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

        private bool EnsureBattleCardSystem(string caller)
        {
            if (battleCardSystem == null)
            {
                battleCardSystem = BattleCardSystem.Instance;
                Debug.Log($"[BattleManager] BattleCardSystem ????? caller={caller}, success={(battleCardSystem != null)}");
            }

            if (battleCardSystem == null)
            {
                Debug.LogWarning($"[BattleManager] BattleCardSystem??筌≪뼚? 筌륁궢六??щ빍?? caller={caller}");
                return false;
            }

            return true;
        }

        private void Awake()
        {
            if (battleCardSystem == null)
            {
                battleCardSystem = BattleCardSystem.Instance;
            }

            Debug.Log($"[BattleManager] Awake ?袁⑥┷: scene={gameObject.scene.name}, hasBattleCardSystem={(battleCardSystem != null)}");
        }

        private void Start()
        {
            EnsureBattleCardSystem("Start");
            Debug.Log($"[BattleManager] Start: autoStartOnSceneLoad={autoStartOnSceneLoad}, currentPhase={CurrentPhase}, isBattleEnded={IsBattleEnded}");
            if (autoStartOnSceneLoad && CurrentPhase == BattlePhase.None && !IsBattleEnded)
            {
                StartBattle();
            }
        }

        public void SetupBattle(BattleStartContext context = null)
        {
            if (!EnsureBattleCardSystem("SetupBattle"))
            {
                return;
            }

            BattleStartContext resolvedContext = context ?? defaultStartContext ?? new BattleStartContext();
            Debug.Log($"[BattleManager] SetupBattle ??뽰삂: startWithMulligan={resolvedContext.StartWithMulligan}, hasBattleDeckCollection={(BattleDeckCollection.Instance != null)}");
            if (BattleDeckCollection.Instance != null)
            {
                Debug.Log($"[BattleManager] 獄쏄퀬? ???怨밴묶: baseDeck={BattleDeckCollection.Instance.BaseBattleDeck.Count}, earned={BattleDeckCollection.Instance.EarnedBattleCards.Count}");
            }

            ResetBattleState();
            RebuildUnitLists();

            SetPhase(BattlePhase.Setup);

            if (battleCardSystem != null)
            {
                battleCardSystem.SetupFromInspector();
                battleCardSystem.SetupActionPoints(0);
            }

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

            Debug.Log("[BattleManager] 筌렺?귐덇탷 ??뽰삂");
            IsMulliganPhase = true;
            SetPhase(BattlePhase.Mulligan);
            DrawOpeningHand();
        }

        public void ConfirmMulligan(bool redraw = false)
        {
            if (!IsMulliganPhase || IsBattleEnded)
            {
                return;
            }

            if (redraw && battleCardSystem != null)
            {
                battleCardSystem.MulliganOpeningHand(GetAlivePlayerUnitTypeCount());
                NotifyHandStateChanged();
            }

            IsMulliganPhase = false;
            StartPlayerTurn();
        }

        public void StartPlayerTurn()
        {
            if (!EnsureBattleCardSystem("StartPlayerTurn"))
            {
                return;
            }

            if (IsBattleEnded)
            {
                return;
            }

            RebuildUnitLists();
            Debug.Log($"[BattleManager] StartPlayerTurn 筌욊낯???醫딅뻺 ?怨밴묶: " +
                $"alivePlayerUnitTypes={GetAlivePlayerUnitTypeCount()}, playerUnits={playerUnits.Count}, enemyUnits={enemyUnits.Count}");
            if (CheckBattleEnd())
            {
                return;
            }

            BattleTurn++;
            CurrentTurnTeam = BattleTeam.Player;
            SetPhase(BattlePhase.PlayerTurn);

            battleCardSystem?.GainTurnActionPoints(BattleTurn);
            Debug.Log($"[BattleManager] ???쟿??곷선 ????뽰삂: turn={BattleTurn}," +
                $" actionPoints={battleCardSystem?.CurrentActionPoints ?? 0}, hasOpeningHandPrepared={hasOpeningHandPrepared}");

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

            battleCardSystem?.EndTurnDiscardHand();
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

            // Enemy AI is not implemented yet, so control returns externally.
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
            var drawnCards = battleCardSystem.DrawOpeningHand(drawCount);
            Debug.Log($"[BattleManager] ??쎈늄???紐껊굡 ??뺤쨮?? unitTypes={drawCount}, drawn={drawnCards.Count}, hand={battleCardSystem.PileState.HandCount}, drawPile={battleCardSystem.PileState.DrawPileCount}");
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
            var drawnCards = battleCardSystem.DrawTurnCards(drawCount);
            Debug.Log($"[BattleManager] ????뺤쨮?? unitTypes={drawCount}, drawn={drawnCards.Count}, hand={battleCardSystem.PileState.HandCount}, drawPile={battleCardSystem.PileState.DrawPileCount}");
            NotifyHandStateChanged();
        }

        public int GetAlivePlayerUnitTypeCount()
        {
            RebuildUnitLists();

            HashSet<string> aliveUnitTypes = new();
            foreach (var unit in playerUnits)
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
            foreach (var unit in allUnits)
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
            foreach (var unit in units)
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
            foreach (var unit in units)
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

