using UnityEngine;

// 기마병 막사 (StableBarracks) — 기마병 유닛 생산 (철기시대~)
// 기마병은 말 체력 소진 시 근접 유닛으로 전환됨 (유닛 시스템에서 처리)
public class CavalryBarracksBehaviour : UnitProducerBehaviour
{
    protected override UnitType SpawnUnit(int slotInCycle)
    {
        Debug.Log($"[CavalryBarracks] 기마병 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
        return UnitType.HorseWarrior;
    }
}
