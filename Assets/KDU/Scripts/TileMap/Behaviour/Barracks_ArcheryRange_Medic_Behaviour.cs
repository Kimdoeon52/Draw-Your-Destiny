using UnityEngine;

// 중급 병영 — 궁수 + 의무병(힐러) 교대 생산
// 체인: Barracks_ArcheryRange_Medic(청동기) → Barracks_ArcheryRange_Medic_Elite(철기)
// 같은 Behaviour 인스턴스가 업그레이드 후에도 유지됨. instance.data만 교체됨.
//
// 생산 규칙:
//   unitsPerCycle = 2. 한 사이클(productionInterval 턴 소요) = 궁수 1 + 힐러 1.
//   자리가 가득 차면 사이클 전체가 waiting 에 쌓이고,
//   유닛 사망으로 자리가 나면 pendingSlot 순서(0=궁수, 1=힐러)대로 1명씩 보충.
//
// 예) cap 10, 만석 상태에서 한 명 사망 → 궁수 스폰 → 또 사망 → 힐러 스폰 → 또 사망 → 궁수 ...
public class Barracks_ArcheryRange_Medic_Behaviour : UnitProducerBehaviour
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
        // slotInCycle: 0 = 궁수, 1 = 힐러 (unitsPerCycle=2 기준)
        if (slotInCycle == 0)
        {
            archerPool.GetHuman(0);
            Debug.Log($"[Barracks_ArcheryRange_Medic] 궁수 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
            return UnitType.Archer;
        }
        else
        {
            healerPool.GetHuman(0);
            Debug.Log($"[Barracks_ArcheryRange_Medic] 힐러 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
            return UnitType.Healer;
        }
    }
}
