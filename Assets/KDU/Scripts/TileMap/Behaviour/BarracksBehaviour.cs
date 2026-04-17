using UnityEngine;

// 병영 — 근접 유닛(돌도끼병) 생산
// 자동 업그레이드 체인: TribePracticeGround(석기) → TrainingCamp(청동기) → Barracks(철기)
// 세 건물 모두 이 Behaviour를 사용. 업그레이드 시 instance.data만 교체되므로 tick/activeCount 유지됨.
public class BarracksBehaviour : UnitProducerBehaviour
{
    RockWarriorPool rockWarriorPool;

    private void Awake()
    {
        rockWarriorPool = GetComponent<RockWarriorPool>();
    }

    protected override void SpawnUnit()
    {
        // TODO: 유닛 시스템 구현 후 UnitManager.Instance.Spawn(UnitType.Soldier, instance); 로 교체
        rockWarriorPool.GetHuman(0);
        Debug.Log($"[Barracks] 병사 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}