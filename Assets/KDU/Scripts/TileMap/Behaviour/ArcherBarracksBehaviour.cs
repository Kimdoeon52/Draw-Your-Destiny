using UnityEngine;

// 사격장 / 정예 사격장 — 궁수 유닛 생산
// 체인: ArcheryRange(청동기) → ArcheryRangeElite(철기)
// 같은 Behaviour 인스턴스가 업그레이드 후에도 유지됨. instance.data만 교체됨.
public class ArcherBarracksBehaviour : UnitProducerBehaviour
{
    protected override void SpawnUnit()
    {
        // TODO: 유닛 시스템 구현 후 UnitManager.Instance.Spawn(UnitType.Archer, instance); 로 교체
        ArcherPool.Instance.GetHuman(0);
        HealerPool.Instance.GetHuman(0);
        Debug.Log($"[ArcherBarracks] 궁수 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}
