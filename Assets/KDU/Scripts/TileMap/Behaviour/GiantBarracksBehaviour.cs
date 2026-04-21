using Unity.VisualScripting;
using UnityEngine;

// 자이언트 막사 — 자이언트 유닛 생산
// 건물 배치는 철기시대부터 가능하지만,
// 연구 포인트가 200에 도달하기 전까지는 유닛을 생산하지 않는다.
public class GiantBarracksBehaviour : UnitProducerBehaviour
{
    WizzardPool wizardPool;

    private const int RequiredResearch = 200;

    private void Awake()
    {
        wizardPool = GetComponent<WizzardPool>();
    }

    public override void OnTurnEnd()
    {
        // 연구 포인트 200 미만이면 tick을 올리지 않음 (생산 대기)
        if (ResourceManager.Instance == null || ResourceManager.Instance.Research < RequiredResearch)
        {
            Debug.Log($"[GiantBarracks] 연구 포인트 부족 ({ResourceManager.Instance?.Research ?? 0}/{RequiredResearch}) — 생산 대기");
            return;
        }

        base.OnTurnEnd();
    }

    protected override void SpawnUnit(int slotInCycle)
    {
        // TODO: 유닛 시스템 구현 후 UnitManager.Instance.Spawn(UnitType.Giant, instance); 로 교체
        wizardPool.GetHuman(0);
        Debug.Log($"[GiantBarracks] 자이언트 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}
