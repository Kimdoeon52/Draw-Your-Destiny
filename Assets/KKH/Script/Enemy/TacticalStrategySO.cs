using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System;

namespace KKH.Script.Enemy
{
    [CreateAssetMenu(fileName = "TacticalStrategy", menuName = "AI/Strategy/Tactical Strategy")]
    public class TacticalStrategySO : AIBehaviorStrategySO
    {
        private enum State
        {
            Analyze,        // 현재 상황 판단
            HitAndRun,      // 공격 후 후퇴
            Positionning,   // 최적의 사거리 확보
            FinalAttack,    // 공격
            EndTurn         // 턴 종료
        }
        public override async UniTask ExecuteBehaviorAsync(EnemyAIManager context, Transform unit)
        {
            State currentState = State.Analyze;
            int attackRange = GetAttackRange(context);

            while (currentState != State.EndTurn)
            {
                Vector3Int unitCell = context.grid.WorldToCell(unit.position);
                Vector3Int playerCell = context.grid.WorldToCell(context.playerUnit.position);
                
                switch(currentState)
                {
                    case State.Analyze:
                        currentState = DetermineNextState(unitCell, playerCell, attackRange);
                        break;
                    case State.HitAndRun:
                        await HandleHitAndRun(context, unit, unitCell, playerCell, attackRange);
                        currentState = State.EndTurn;
                        break;
                    case State.Positionning:
                        await HandlePositionning(context, unit, unitCell, playerCell, attackRange);
                        currentState = State.FinalAttack;                        
                        break;
                    case State.FinalAttack:
                        context.TryAttackNearbyPlayer(unit, context.grid.WorldToCell(unit.position));
                        currentState = State.EndTurn;
                        break;
                }
                await UniTask.Yield();
            }
        }

#region =========상태 처리 로직=========
        private State DetermineNextState(Vector3Int unitCell, Vector3Int playerCell, int attackRange)
        {
            int dist = Mathf.Abs(unitCell.x - playerCell.x) + Mathf.Abs(unitCell.y - playerCell.y);
            bool isSameAxis = (unitCell.x == playerCell.x) || (unitCell.y == playerCell.y);

            return (isSameAxis && dist <= attackRange) ? State.HitAndRun : State.Positionning;
        }
        private async UniTask HandleHitAndRun(EnemyAIManager context, Transform unit, Vector3Int unitCell, Vector3Int playerCell, int attackRange)
        {
            Debug.Log($"[TacticalStrategySO] {unit.name} Hit and Run 실행");
            context.TryAttackNearbyPlayer(unit, unitCell);
            Vector3Int fleeDir = new Vector3Int(Mathf.Clamp(unitCell.x - playerCell.x, -1, 1), Mathf.Clamp(unitCell.y - playerCell.y, -1, 1), 0);
            Vector3Int target = unitCell + fleeDir * 5;
            var path = context.FindPathToPlayer(unitCell, target);

            if (path.Count > 0)
            {
                await context.MoveUnitAlongPathAsync(unit, path);
            }
        }

        private async Task HandlePositionning(EnemyAIManager context, Transform unit, Vector3Int unitCell, Vector3Int playerCell, int attackRange)
        {
            Debug.Log($"[TacticalStrategySO] {unit.name} Positionning 실행");
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
        #endregion
        /// <summary>
        /// EnemyAIManager에 private으로 숨겨진 attackRange 필드값을 Reflection으로 가져옵니다.
        /// (manager.cs 스크립트 수정 불가 제약사항 회피용)
        /// </summary>
        private int GetAttackRange(EnemyAIManager context)
        {
            FieldInfo field = typeof(EnemyAIManager).GetField("attackRange", BindingFlags.NonPublic | BindingFlags.Instance);

            return field != null ? (int)field.GetValue(context) : 2;
        }
    }
}
