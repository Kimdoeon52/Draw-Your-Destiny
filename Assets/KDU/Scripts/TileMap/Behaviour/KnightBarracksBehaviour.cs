using UnityEngine;

// 기사단 막사 (KnightBarracks) — 풀플레이트 아머 기사 유닛 생산 (철기시대~)
public class KnightBarracksBehaviour : UnitProducerBehaviour
{
    protected override void SpawnUnit()
    {
        // TODO: 유닛 시스템 구현 후 UnitManager.Instance.Spawn(UnitType.Knight, instance); 로 교체
        Debug.Log($"[KnightBarracks] 기사 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}
