using UnityEngine;

// 기마병 막사 (StableBarracks) — 기마병 유닛 생산 (철기시대~)
// 기마병은 말 체력 소진 시 근접 유닛으로 전환됨 (유닛 시스템에서 처리)
public class CavalryBarracksBehaviour : UnitProducerBehaviour
{
    protected override void SpawnUnit()
    {
        // TODO: 유닛 시스템 구현 후 UnitManager.Instance.Spawn(UnitType.Cavalry, instance); 로 교체
        Debug.Log($"[CavalryBarracks] 기마병 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}
