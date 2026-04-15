namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public class BattleTacticalPerformer
    {
        private const bool EnableMoveDebug = false;

        public bool CanHandle(GameAction action)
        {
            return action is BattleAttackGA || action is BattleMoveGA;
        }

        public IEnumerator Perform(GameAction action)
        {
            if (action is BattleAttackGA attackGA)
            {
                ResolveAttack(attackGA);
                yield return null;
            }
            else if (action is BattleMoveGA moveGA)
            {
                yield return ResolveMove(moveGA);
            }
        }

        private IEnumerator ResolveMove(BattleMoveGA moveGA)
        {
            if (moveGA.Unit == null)
            {
                Debug.LogWarning("[BattleCardSystem] 이동할 유닛이 없습니다.");
                yield break;
            }

            if (BattleBoardSystem.Instance == null)
            {
                Debug.LogWarning("[BattleCardSystem] BattleBoardSystem이 없어 이동을 처리할 수 없습니다.");
                yield break;
            }

            //if (EnableMoveDebug)
            //{
            //    Debug.Log(
            //        $"[BattleMoveDebug] ResolveMove start unit={moveGA.Unit.name}, currentWorld={moveGA.Unit.transform.position}, currentGrid={moveGA.Unit.GridPosition}, target={moveGA.TargetPosition}, finalMove={moveGA.FinalMoveAmount}, pathCount={(moveGA.PlannedPath != null ? moveGA.PlannedPath.Count : 0)}, path={BuildPathDebugText(moveGA.PlannedPath)}");
            //}

            yield return AnimateMove(moveGA);

            bool moved = BattleBoardSystem.Instance.TryMoveUnit(
                moveGA.Unit,
                moveGA.TargetPosition,
                moveGA.FinalMoveAmount,
                syncTransform: false,
                plannedPath: moveGA.PlannedPath);
            moveGA.WasMoved = moved;

            if (!moved)
            {
                // 이동이 막혔는데 후속 공격까지 실행되면
                // 하이브리드 카드가 의도와 다르게 동작하므로 연쇄 반응을 정리합니다.
                moveGA.PerformReactions.Clear();
                moveGA.PostReactions.Clear();
            }

            //if (EnableMoveDebug)
            //{
            //    Debug.Log(
            //        $"[BattleMoveDebug] ResolveMove end unit={moveGA.Unit.name}, moved={moved}, finalWorld={moveGA.Unit.transform.position}, finalGrid={moveGA.Unit.GridPosition}");
            //}
        }

        private static IEnumerator AnimateMove(BattleMoveGA moveGA)
        {
            if (moveGA?.Unit == null || moveGA.PlannedPath == null || moveGA.PlannedPath.Count == 0)
            {
                //if (EnableMoveDebug)
                //{
                //    Debug.LogWarning(
                //        $"[BattleMoveDebug] AnimateMove skipped unit={(moveGA?.Unit != null ? moveGA.Unit.name : "null")}, pathCount={(moveGA?.PlannedPath != null ? moveGA.PlannedPath.Count : 0)}");
                //}

                yield break;
            }

            Transform unitTransform = moveGA.Unit.transform;
            float moveSpeed = 3.75f;
            Vector3 currentPosition = unitTransform.position;

            for (int i = 0; i < moveGA.PlannedPath.Count; i++)
            {
                Vector2Int pathCell = moveGA.PlannedPath[i];
                Vector3 targetWorld = new(pathCell.x, pathCell.y, unitTransform.position.z);

                //if (EnableMoveDebug)
                //{
                //    Debug.Log($"[BattleMoveDebug] AnimateMove waypoint index={i}, cell={pathCell}, targetWorld={targetWorld}");
                //}

                while (Vector3.Distance(currentPosition, targetWorld) > 0.01f)
                {
                    currentPosition = Vector3.MoveTowards(
                        currentPosition,
                        targetWorld,
                        moveSpeed * Time.deltaTime);
                    unitTransform.position = currentPosition;
                    yield return null;
                }

                currentPosition = targetWorld;
                unitTransform.position = currentPosition;
                yield return null;
            }
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

        private void ResolveAttack(BattleAttackGA attackGA)
        {
            if (attackGA.Attacker == null)
            {
                Debug.LogWarning("[BattleCardSystem] 공격 유닛이 없습니다.");
                return;
            }

            if (BattleBoardSystem.Instance == null)
            {
                Debug.LogWarning("[BattleCardSystem] BattleBoardSystem이 없어 공격을 처리할 수 없습니다.");
                return;
            }

            var targets = BattleBoardSystem.Instance.GetUnitsInAttackArea(
                attackGA.Attacker,
                attackGA.TargetPosition,
                attackGA);
            Dictionary<BattleUnit, int> targetHealthBeforeAttack = CaptureTargetHealth(targets);

            if (TryApplyBattleEffects(attackGA, targets))
            {
                LogAttackResults(attackGA, targets, targetHealthBeforeAttack, null, usedEffects: true);
                return;
            }

            foreach (var target in targets)
            {
                int totalDamage = Mathf.Max(0, attackGA.Damage + attackGA.Attacker.CurrentAttackPower);
                target.TakeDamage(totalDamage);
            }

            LogAttackResults(
                attackGA,
                targets,
                targetHealthBeforeAttack,
                Mathf.Max(0, attackGA.Damage + attackGA.Attacker.CurrentAttackPower),
                usedEffects: false);
        }

        private static Dictionary<BattleUnit, int> CaptureTargetHealth(IReadOnlyList<BattleUnit> targets)
        {
            Dictionary<BattleUnit, int> result = new();
            if (targets == null)
            {
                return result;
            }

            foreach (BattleUnit target in targets)
            {
                if (target != null && !result.ContainsKey(target))
                {
                    result[target] = target.CurrentHealth;
                }
            }

            return result;
        }

        private static void LogAttackResults(
            BattleAttackGA attackGA,
            IReadOnlyList<BattleUnit> targets,
            IReadOnlyDictionary<BattleUnit, int> targetHealthBeforeAttack,
            int? baseDamageApplied,
            bool usedEffects)
        {
            if (targets == null || targets.Count == 0)
            {
                Debug.Log(
                    $"[BattleAttackDebug] {(attackGA?.Attacker != null ? attackGA.Attacker.name : "Unknown")}의 공격이 빗나감");
                return;
            }

            foreach (BattleUnit target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                int beforeHealth = 0;
                if (targetHealthBeforeAttack != null && targetHealthBeforeAttack.TryGetValue(target, out int capturedHealth))
                {
                    beforeHealth = capturedHealth;
                }

                int afterHealth = target.CurrentHealth;
                int actualDamage = Mathf.Max(0, beforeHealth - afterHealth);
                string cardTitle = attackGA?.SourceCard != null ? attackGA.SourceCard.Title : "Unknown";
                string effectLabel = usedEffects ? "효과" : "공격";

                Debug.Log(
                    $"[BattleAttackDebug] {effectLabel}: {(attackGA?.Attacker != null ? attackGA.Attacker.name : "Unknown")} -> {target.name}, damage={actualDamage}, hp={afterHealth}/{target.MaxHealth}, alive={target.IsAlive}, card={cardTitle}");
            }
        }

        private static bool TryApplyBattleEffects(BattleAttackGA attackGA, IReadOnlyList<BattleUnit> resolvedTargets)
        {
            if (attackGA?.SourceCard?.RuntimeEffects == null)
            {
                return false;
            }

            bool appliedAnyEffect = false;
            BattleEffectContext context = new(
                attackGA.SourceCard,
                attackGA.Attacker,
                attackGA.PrimaryTarget,
                attackGA.TargetPosition,
                null,
                BattleBoardSystem.Instance,
                BattleCardSystem.Instance);

            foreach (var effect in attackGA.SourceCard.RuntimeEffects)
            {
                if (effect is not BattleEffect battleEffect)
                {
                    continue;
                }

                if (battleEffect is BattleMoveEffect or BattleAttackEffect)
                {
                    // 이동/공격 자체는 GA 체인에서 이미 처리되므로,
                    // 여기서는 피해/상태 같은 부가 이펙트만 적용합니다.
                    continue;
                }

                battleEffect.Apply(context, resolvedTargets);
                appliedAnyEffect = true;
            }

            return appliedAnyEffect;
        }
    }
}

