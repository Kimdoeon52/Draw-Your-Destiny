using Cysharp.Threading.Tasks;
using NYH.BattleCardSystem;
using UnityEngine;
using System.Collections.Generic;

namespace KKH.Script.Enemy
{
    /// <summary>
    /// FSM(상태머신) 기반의 고도화된 전술 AI 전략입니다.
    /// '분석 -> 히트앤런/포지셔닝 -> 최종공격'의 단계를 거쳐 지능적으로 행동합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TacticalStrategy", menuName = "AI/Strategy/Tactical Strategy")]
    public class TacticalStrategySO : AIBehaviorStrategySO
    {
        /// <summary>전술 행동 상태 정의</summary>
        private enum State
        {
            Analyze,        // 현재 상황 분석 및 다음 행동 결정
            HitAndRun,     // 공격 후 안전한 곳으로 도망
            Positionning,   // 공격하기 좋은 위치로 이동
            FinalAttack,    // 이동 후 남은 공격 시도
            EndTurn,        // 행동 종료
        }

        #region ── 레거시 EnemyAIManager 용 실행 로직 ──

        public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
        {
            State currentState = State.Analyze;
            int attackRange = 2; // 기본 사거리

            while (currentState != State.EndTurn)
            {
                Vector3Int unitCell = context.grid.WorldToCell(unit.position);
                Vector3Int playerCell = context.grid.WorldToCell(context.playerUnit.position);

                switch (currentState)
                {
                    case State.Analyze:
                        // 현재 위치와 플레이어 위치를 고려하여 다음 상태 결정
                        currentState = DetermineNextState(unitCell, playerCell, attackRange);
                        break;
                    case State.HitAndRun:
                        // 공격하고 도망가기
                        await HandleHitAndRun(context, unit, unitCell, playerCell);
                        currentState = State.EndTurn;
                        break;
                    case State.Positionning:
                        // 공격 사거리 내 최적의 위치로 이동
                        await HandlePositionning(context, unit, unitCell, playerCell, attackRange);
                        currentState = State.FinalAttack;
                        break;
                    case State.FinalAttack:
                        // 최종 위치에서 인접 플레이어 공격
                        context.TryAttackNearbyPlayer(unit, context.grid.WorldToCell(unit.position));
                        currentState = State.EndTurn;
                        break;
                }

                await UniTask.Yield();
            }
        }

        #endregion

        #region ── 새로운 BattleCardSystem(IBattleAIContext) 용 실행 로직 ──

        public override async UniTask ExecuteBehaviorAsync(IBattleAIContext context, BattleUnit unit)
        {
            if (context == null || unit == null) return;

            State currentState = State.Analyze;
            int attackRange = context.GetAttackRange(unit);

            while (currentState != State.EndTurn)
            {
                BattleUnit player = context.GetNearestPlayerUnit(unit);
                if (player == null) return;

                Vector2Int unitCell = context.GetGridPosition(unit);
                Vector2Int playerCell = context.GetGridPosition(player);

                switch (currentState)
                {
                    case State.Analyze:
                        currentState = DetermineNextState(unitCell, playerCell, attackRange);
                        break;
                    case State.HitAndRun:
                        // 1. 먼저 공격
                        context.TryAttackPlayerInRange(unit, attackRange);
                        // 2. 플레이어의 반대 방향으로 도망
                        Vector2Int fleeTarget = unitCell + new Vector2Int(
                            Mathf.Clamp(unitCell.x - playerCell.x, -1, 1),
                            Mathf.Clamp(unitCell.y - playerCell.y, -1, 1)) * context.GetMoveBudget(unit);
                        var fleePath = context.FindPathTowards(unit, fleeTarget, context.GetMoveBudget(unit));
                        if (fleePath.Count > 0)
                        {
                            await context.MoveUnitAlongPathAsync(unit, fleePath);
                        }
                        currentState = State.EndTurn;
                        break;
                    case State.Positionning:
                        // 1. 공격 가능한 최적의 위치 탐색
                        Vector2Int bestCell = FindBestTacticalCell(context, unit, playerCell, attackRange);
                        // 2. 해당 위치로 이동
                        var path = context.FindPathTowards(unit, bestCell, context.GetMoveBudget(unit));
                        if (path.Count > 0)
                        {
                            await context.MoveUnitAlongPathAsync(unit, path);
                        }
                        currentState = State.FinalAttack;
                        break;
                    case State.FinalAttack:
                        context.TryAttackPlayerInRange(unit, attackRange);
                        currentState = State.EndTurn;
                        break;
                }

                await UniTask.Yield();
            }
        }

        #endregion

        #region ── 상태 결정 및 내부 로직 ──

        /// <summary>
        /// 유닛과 플레이어 사이의 거리와 축을 계산하여 다음 행동 상태를 결정합니다.
        /// </summary>
        private State DetermineNextState(Vector2Int unitCell, Vector2Int playerCell, int attackRange)
        {
            int dist = Mathf.Abs(unitCell.x - playerCell.x) + Mathf.Abs(unitCell.y - playerCell.y);
            bool isSameAxis = unitCell.x == playerCell.x || unitCell.y == playerCell.y;
            // 사거리 내에 있고 같은 축(십자 방향)에 있으면 히트앤런, 아니면 포지셔닝
            return (isSameAxis && dist <= attackRange) ? State.HitAndRun : State.Positionning;
        }

        private State DetermineNextState(Vector3Int unitCell, Vector3Int playerCell, int attackRange)
        {
            int dist = Mathf.Abs(unitCell.x - playerCell.x) + Mathf.Abs(unitCell.y - playerCell.y);
            bool isSameAxis = unitCell.x == playerCell.x || unitCell.y == playerCell.y;
            return (isSameAxis && dist <= attackRange) ? State.HitAndRun : State.Positionning;
        }

        /// <summary>
        /// 레거시: 히트앤런 처리 (공격 후 플레이어 반대 방향으로 도망)
        /// </summary>
        private async UniTask HandleHitAndRun(EnemyAIManager context, Transform unit, Vector3Int unitCell, Vector3Int playerCell)
        {
            context.TryAttackNearbyPlayer(unit, unitCell);
            Vector3Int fleeDir = new(
                Mathf.Clamp(unitCell.x - playerCell.x, -1, 1),
                Mathf.Clamp(unitCell.y - playerCell.y, -1, 1),
                0);
            Vector3Int target = unitCell + fleeDir * 5;
            var path = context.FindPathToPlayer(unitCell, target);
            if (path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        /// <summary>
        /// 레거시: 포지셔닝 처리 (플레이어의 상하좌우 사거리 위치 중 가장 가까운 곳으로 이동)
        /// </summary>
        private async UniTask HandlePositionning(EnemyAIManager context, Transform unit, Vector3Int unitCell, Vector3Int playerCell, int attackRange)
        {
            Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            Vector3Int bestCell = playerCell;
            int minDistance = int.MaxValue;
            foreach (var dir in dirs)
            {
                Vector3Int candidate = playerCell + (dir * attackRange);
                if (context.IsCellWalkable(candidate))
                {
                    int dist = Mathf.Abs(candidate.x - unitCell.x) + Mathf.Abs(candidate.y - unitCell.y);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestCell = candidate;
                    }
                }
            }

            var path = context.FindPathToPlayer(unitCell, bestCell);
            if (path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        /// <summary>
        /// 새로운 배틀 시스템용 최적의 전술적 위치 탐색
        /// </summary>
        private Vector2Int FindBestTacticalCell(IBattleAIContext context, BattleUnit unit, Vector2Int playerCell, int attackRange)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            Vector2Int bestCell = context.GetGridPosition(unit);
            int bestDistance = int.MaxValue;

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int candidate = playerCell + (dir * attackRange);
                if (!context.IsCellWalkable(candidate)) continue;

                int distance = Mathf.Abs(candidate.x - unit.GridPosition.x) + Mathf.Abs(candidate.y - unit.GridPosition.y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCell = candidate;
                }
            }

            return bestCell;
        }

        #endregion
    }
}
