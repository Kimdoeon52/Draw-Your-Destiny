namespace NYH.BattleCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;

    public class BattleTacticalPerformer
    {
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
                ResolveMove(moveGA);
                yield return null;
            }
        }

        private void ResolveMove(BattleMoveGA moveGA)
        {
            if (moveGA.Unit == null)
            {
                Debug.LogWarning("[BattleCardSystem] 이동할 유닛이 없습니다.");
                return;
            }

            if (BattleBoardSystem.Instance == null)
            {
                Debug.LogWarning("[BattleCardSystem] BattleBoardSystem이 없어 이동을 처리할 수 없습니다.");
                return;
            }

            bool moved = BattleBoardSystem.Instance.TryMoveUnit(moveGA.Unit, moveGA.TargetPosition, moveGA.FinalMoveAmount);
            moveGA.WasMoved = moved;

            Debug.Log(
                $"[BattleCardSystem] 이동 처리: unit={moveGA.Unit.name}, target={moveGA.TargetPosition}, finalMove={moveGA.FinalMoveAmount}, moved={moved}");
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

            if (TryApplyBattleEffects(attackGA, targets))
            {
                Debug.Log(
                    $"[BattleCardSystem] 이펙트 공격 처리: attacker={attackGA.Attacker.name}, targetCount={targets.Count}, pattern={attackGA.AttackPattern}");
                return;
            }

            foreach (var target in targets)
            {
                int totalDamage = Mathf.Max(0, attackGA.Damage + attackGA.Attacker.CurrentAttackPower);
                target.TakeDamage(totalDamage);
            }

            Debug.Log(
                $"[BattleCardSystem] 기본 공격 처리: attacker={attackGA.Attacker.name}, targetCount={targets.Count}, damage={attackGA.Damage}, pattern={attackGA.AttackPattern}");
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
                BattleBoardSystem.Instance,
                BattleCardSystem.Instance);

            foreach (var effect in attackGA.SourceCard.RuntimeEffects)
            {
                if (effect is not BattleEffect battleEffect)
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
