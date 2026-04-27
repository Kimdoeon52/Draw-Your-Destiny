namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 이미 만들어진 이동/공격 GameAction을 실제 보드와 유닛에 적용합니다.
    /// 카드 사용 비용이나 액션 체인 생성은 담당하지 않습니다.
    /// </summary>
    public class BattleTacticalPerformer
    {
        private static readonly bool EnableAttackDebug = false;

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

            bool checkedTrapDuringPath = HasPlannedPath(moveGA);
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
                // 이동이 실패한 뒤 후속 공격이 이어지면 하이브리드 카드가 잘못된 위치 기준으로 실행됩니다.
                moveGA.PerformReactions.Clear();
                moveGA.PostReactions.Clear();
                yield break;
            }

            if (!checkedTrapDuringPath)
            {
                BattleTrapSystem.TryTriggerTrapsAt(moveGA.Unit);
            }
        }

        private static IEnumerator AnimateMove(BattleMoveGA moveGA)
        {
            if (moveGA?.Unit == null || moveGA.PlannedPath == null || moveGA.PlannedPath.Count == 0)
            {
                yield break;
            }

            BattleUnit unit = moveGA.Unit;
            Transform unitTransform = unit.transform;
            float moveSpeed = 3.75f;
            Vector3 currentPosition = BattleUnit.GetWorldPositionForGrid(unit.GridPosition, unitTransform.position.z);
            unitTransform.position = currentPosition;

            for (int i = 0; i < moveGA.PlannedPath.Count; i++)
            {
                Vector2Int pathCell = moveGA.PlannedPath[i];
                Vector3 targetWorld = BattleUnit.GetWorldPositionForGrid(pathCell, unitTransform.position.z);

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
                unit.SetGridPosition(pathCell);
                BattleTrapSystem.TryTriggerTrapsAt(unit);
                if (!unit.IsAlive)
                {
                    yield break;
                }

                yield return null;
            }

            unit.SetGridPosition(moveGA.TargetPosition);
            unit.SnapToGridCenter();
        }

        private static bool HasPlannedPath(BattleMoveGA moveGA)
        {
            return moveGA?.PlannedPath != null && moveGA.PlannedPath.Count > 0;
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

            List<BattleUnit> targets = BattleBoardSystem.Instance.GetUnitsInAttackArea(
                attackGA.Attacker,
                attackGA.TargetPosition,
                attackGA);
            Dictionary<BattleUnit, int> targetHealthBeforeAttack = EnableAttackDebug
                ? CaptureTargetHealth(targets)
                : null;

            if (TryApplyBattleEffects(attackGA, targets))
            {
                LogAttackResultsIfEnabled(attackGA, targets, targetHealthBeforeAttack, usedEffects: true);
                return;
            }

            foreach (BattleUnit target in targets)
            {
                int totalDamage = Mathf.Max(0, attackGA.Damage + attackGA.Attacker.CurrentAttackPower);
                target.TakeDamage(totalDamage);
            }

            LogAttackResultsIfEnabled(attackGA, targets, targetHealthBeforeAttack, usedEffects: false);
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

        private static void LogAttackResultsIfEnabled(
            BattleAttackGA attackGA,
            IReadOnlyList<BattleUnit> targets,
            IReadOnlyDictionary<BattleUnit, int> targetHealthBeforeAttack,
            bool usedEffects)
        {
            if (!EnableAttackDebug)
            {
                return;
            }

            if (targets == null || targets.Count == 0)
            {
                Debug.Log($"[BattleAttackDebug] {(attackGA?.Attacker != null ? attackGA.Attacker.name : "Unknown")}의 공격이 빗나감");
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
                    // 이동/공격 자체는 GameAction 체인에서 이미 처리되므로 피해/상태 같은 부가 이펙트만 적용합니다.
                    continue;
                }

                if (!battleEffect.CanApply(context))
                {
                    continue;
                }

                battleEffect.Apply(context, resolvedTargets);
                appliedAnyEffect = true;
            }

            return appliedAnyEffect;
        }
    }
}
