using UnityEngine;

// 기사단 막사 (KnightBarracks) — 풀플레이트 아머 기사 유닛 생산 (철기시대~)
public class KnightBarracksBehaviour : UnitProducerBehaviour
{
    SuperUnitPool superUnitPool;

    private void Awake()
    {
        superUnitPool = GetComponent<SuperUnitPool>();
    }

    protected override UnitType SpawnUnit(int slotInCycle)
    {
        superUnitPool.GetHuman(0);
        Debug.Log($"[KnightBarracks] 기사 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
        return UnitType.Knight;
    }
}
