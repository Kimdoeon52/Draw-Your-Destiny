namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /*
     * BattleEnemyAIController
     *
     * 역할:
     * - 적 턴 동안 살아 있는 적 유닛을 순서대로 실행합니다.
     * - 각 유닛의 AI 전략에 필요한 보드 조회/이동/공격 API를 IBattleAIContext로 제공합니다.
     *
     * 담당하지 않는 것:
     * - 구체적인 행동 판단은 AIBehaviorStrategySO가 담당합니다.
     * - 전투 페이즈 전환은 BattleManager가 담당합니다.
     */
    public class BattleEnemyAIController : MonoBehaviour, IBattleAIContext
    {
        [Header("Defaults")]
        [SerializeField] private AIBehaviorStrategySO defaultStrategy;
        [SerializeField] private int defaultMoveBudget = 3;
        [SerializeField] private int defaultAttackRange = 1;
        [SerializeField] private float moveSecondsPerCell = 0.12f;
        [SerializeField] private float unitActionDelaySeconds = 0.2f;

        public event Action OnAITurnFinished;

        private bool isExecutingTurn;

        // 적 턴 전체를 비동기로 실행하고 완료 시 OnAITurnFinished를 호출합니다.
        public async UniTask ExecuteTurnAsync()
        {
            if (isExecutingTurn)
            {
                return;
            }

            isExecutingTurn = true;
            try
            {
                List<BattleUnit> enemyUnits = CollectUnits(BattleTeam.Enemy);
                for (int i = 0; i < enemyUnits.Count; i++)
                {
                    BattleUnit unit = enemyUnits[i];
                    if (unit == null || !unit.IsAlive)
                    {
                        continue;
                    }

                    AIBehaviorStrategySO strategy = ResolveStrategy(unit);
                    if (strategy != null)
                    {
                        await strategy.ExecuteBehaviorAsync(this, unit);
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(unitActionDelaySeconds));
                }
            }
            finally
            {
                isExecutingTurn = false;
                OnAITurnFinished?.Invoke();
            }
        }

        // requester 기준 가장 가까운 플레이어 유닛을 찾습니다.
        public BattleUnit GetNearestPlayerUnit(BattleUnit requester)
        {
            return GetNearestUnit(requester, BattleTeam.Player);
        }

        // requester 기준 가장 가까운 다른 적 유닛을 찾습니다.
        public BattleUnit GetNearestEnemyUnit(BattleUnit requester)
        {
            return GetNearestUnit(requester, BattleTeam.Enemy);
        }

        // AI 전략이 유닛의 현재 그리드 좌표를 읽을 때 사용하는 래퍼입니다.
        public Vector2Int GetGridPosition(BattleUnit unit)
        {
            return unit != null ? unit.GridPosition : Vector2Int.zero;
        }

        // 유닛별 AI 프로필이 있으면 그 값을, 없으면 기본 이동량을 반환합니다.
        public int GetMoveBudget(BattleUnit unit)
        {
            BattleUnitAIProfile profile = unit != null ? unit.GetComponent<BattleUnitAIProfile>() : null;
            return profile != null ? profile.MoveBudget : Mathf.Max(0, defaultMoveBudget);
        }

        // 유닛별 AI 프로필이 있으면 그 값을, 없으면 기본 공격 사거리를 반환합니다.
        public int GetAttackRange(BattleUnit unit)
        {
            BattleUnitAIProfile profile = unit != null ? unit.GetComponent<BattleUnitAIProfile>() : null;
            return profile != null ? profile.AttackRange : Mathf.Max(1, defaultAttackRange);
        }

        // 이동 가능 범위 안에서 목표 셀에 가장 가까워지는 경로를 찾습니다.
        public List<Vector2Int> FindPathTowards(BattleUnit unit, Vector2Int targetCell, int moveBudget)
        {
            List<Vector2Int> empty = new();
            BattleBoardSystem board = BattleBoardSystem.Instance;
            if (board == null || unit == null || !unit.IsAlive || moveBudget <= 0)
            {
                return empty;
            }

            HashSet<Vector2Int> selectableCells = board.GetSelectableMoveCells(unit, moveBudget);
            Vector2Int startCell = unit.GridPosition;
            Vector2Int bestCell = startCell;
            int bestDistance = ManhattanDistance(startCell, targetCell);
            int bestPathLength = int.MaxValue;

            foreach (Vector2Int cell in selectableCells)
            {
                int distance = ManhattanDistance(cell, targetCell);
                if (!board.TryBuildMovePath(unit, startCell, cell, moveBudget, out List<Vector2Int> candidatePath))
                {
                    continue;
                }

                int candidateLength = candidatePath != null ? candidatePath.Count : 0;
                if (distance < bestDistance || (distance == bestDistance && candidateLength < bestPathLength))
                {
                    bestDistance = distance;
                    bestPathLength = candidateLength;
                    bestCell = cell;
                }
            }

            if (bestCell == startCell)
            {
                return empty;
            }

            return board.TryBuildMovePath(unit, startCell, bestCell, moveBudget, out List<Vector2Int> path)
                ? path
                : empty;
        }

        // 계산된 경로를 따라 유닛을 실제로 이동시키고 보드 점유 상태를 갱신합니다.
        public async UniTask MoveUnitAlongPathAsync(BattleUnit unit, IReadOnlyList<Vector2Int> path)
        {
            BattleBoardSystem board = BattleBoardSystem.Instance;
            if (board == null || unit == null || path == null || path.Count == 0)
            {
                return;
            }

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int nextCell = path[i];
                Vector3 startPosition = BattleUnit.GetWorldPositionForGrid(unit.GridPosition, unit.transform.position.z);
                Vector3 endPosition = BattleUnit.GetWorldPositionForGrid(nextCell, unit.transform.position.z);
                int stepCost = board.GetStepCost(nextCell);

                unit.transform.position = startPosition;

                if (!board.TryMoveUnit(unit, nextCell, stepCost, syncTransform: false))
                {
                    return;
                }

                float elapsed = 0f;
                float duration = Mathf.Max(0.01f, moveSecondsPerCell);
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    unit.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                unit.transform.position = endPosition;
                unit.SnapToGridCenter();
            }
        }

        // 사거리 안에 있는 가장 가까운 플레이어 유닛을 기본 공격으로 타격합니다.
        public bool TryAttackPlayerInRange(BattleUnit unit, int attackRange)
        {
            if (unit == null || !unit.IsAlive)
            {
                return false;
            }

            BattleUnit target = FindNearestAttackablePlayer(unit, Mathf.Max(1, attackRange));
            if (target == null)
            {
                return false;
            }

            int damage = Mathf.Max(0, unit.CurrentAttackPower);
            target.TakeDamage(damage);
            return true;
        }

        // AI 전략이 셀 이동 가능 여부를 확인할 때 사용하는 보드 래퍼입니다.
        public bool IsCellWalkable(Vector2Int cell)
        {
            BattleBoardSystem board = BattleBoardSystem.Instance;
            if (board == null)
            {
                return false;
            }

            return board.GetUnitAt(cell) == null;
        }

        private AIBehaviorStrategySO ResolveStrategy(BattleUnit unit)
        {
            BattleUnitAIProfile profile = unit != null ? unit.GetComponent<BattleUnitAIProfile>() : null;
            if (profile != null && profile.Strategy != null)
            {
                return profile.Strategy;
            }

            return defaultStrategy;
        }

        private List<BattleUnit> CollectUnits(BattleTeam team)
        {
            List<BattleUnit> result = new();
            BattleUnit[] allUnits = FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < allUnits.Length; i++)
            {
                BattleUnit unit = allUnits[i];
                if (unit == null || !unit.IsAlive || unit.Team != team)
                {
                    continue;
                }

                result.Add(unit);
            }

            result.Sort((a, b) => a.GridPosition.y != b.GridPosition.y
                ? b.GridPosition.y.CompareTo(a.GridPosition.y)
                : a.GridPosition.x.CompareTo(b.GridPosition.x));
            return result;
        }

        private BattleUnit GetNearestUnit(BattleUnit requester, BattleTeam team)
        {
            List<BattleUnit> units = CollectUnits(team);
            BattleUnit best = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit candidate = units[i];
                if (candidate == null || candidate == requester)
                {
                    continue;
                }

                int distance = ManhattanDistance(requester.GridPosition, candidate.GridPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private BattleUnit FindNearestAttackablePlayer(BattleUnit unit, int attackRange)
        {
            List<BattleUnit> players = CollectUnits(BattleTeam.Player);
            BattleUnit best = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < players.Count; i++)
            {
                BattleUnit candidate = players[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                bool sameAxis = candidate.GridPosition.x == unit.GridPosition.x
                    || candidate.GridPosition.y == unit.GridPosition.y;
                if (!sameAxis)
                {
                    continue;
                }

                int distance = ManhattanDistance(unit.GridPosition, candidate.GridPosition);
                if (distance > attackRange)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}

