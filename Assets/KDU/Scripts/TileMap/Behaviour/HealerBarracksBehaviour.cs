using UnityEngine;

// 의무병 막사 / 정예 의무병 막사 — 힐러(의무병) 유닛 생산
// 체인: MedicBarracks(청동기) → MedicBarracksElite(철기)
// 같은 Behaviour 인스턴스가 업그레이드 후에도 유지됨. instance.data만 교체됨.
public class HealerBarracksBehaviour : UnitProducerBehaviour
{
    protected override UnitType SpawnUnit(int slotInCycle)
    {
        Debug.Log($"[HealerBarracks] 의무병 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
        return UnitType.Healer;
    }
}
