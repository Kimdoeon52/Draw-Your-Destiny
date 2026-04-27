using UnityEngine;

// 중급 병영 — 궁수 유닛 생산
// 체인: Barracks_ArcheryRange_Medic(청동기) → Barracks_ArcheryRange_Medic_Elite(철기)
// 같은 Behaviour 인스턴스가 업그레이드 후에도 유지됨. instance.data만 교체됨.
public class ArcherBarracksBehaviour : UnitProducerBehaviour
{
    ArcherPool archerPool;
    HealerPool healerPool;

    private void Awake()
    {
        archerPool = GetComponent<ArcherPool>();
        healerPool = GetComponent<HealerPool>();
    }

    protected override UnitType SpawnUnit(int slotInCycle)
    {
        // 레거시: 한 번에 궁수+힐러 동시 스폰. 베이스는 1명만 추적하므로 healer는 별도 추가.
        archerPool.GetHuman(0);
        healerPool.GetHuman(0);
        var wm = WorldMapManager.Instance;
        if (wm != null)
            WorldMapManager.AddUnitToNode(wm.GetNode(wm.CurrentNodeID), UnitType.Healer, 1);
        Debug.Log($"[ArcherBarracks] 궁수+힐러 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
        return UnitType.Archer;
    }
}
