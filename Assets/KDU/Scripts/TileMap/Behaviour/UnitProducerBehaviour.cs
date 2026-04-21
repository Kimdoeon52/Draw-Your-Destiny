using UnityEngine;

// ============================================================
// UnitProducerBehaviour — 유닛을 생산하는 건물의 공통 추상 베이스
//
// 병영 / 농장 / 상점 등이 상속.
// productionInterval 턴마다 사이클 발동 → 큐에 쌓고 자리 있는 만큼 pendingSlot 순서로 1명씩 스폰.
// 사이클 = unitsPerCycle 명 (단일 유닛 건물 1, 쌍 유닛 건물 2).
// 자리가 없으면 사이클 전체가 waiting 에 누적, 유닛 사망 시 큐에서 순차 보충.
// ============================================================
public abstract class UnitProducerBehaviour : BuildingBehaviour
{
    protected int tick;
    protected int activeCount;
    protected int waiting;        // 아직 다 채우지 못한 사이클 수
    protected int pendingSlot;    // 다음에 스폰할 사이클 내 인덱스 (0..unitsPerCycle-1)

    public int ActiveCount  => activeCount;
    public int WaitingCount => waiting;
    public int Capacity     => instance?.data != null ? instance.data.unitCapacity : 0;

    private bool initialSpawnDone = false;

    public override void OnTurnEnd()
    {
        if (!initialSpawnDone)
        {
            initialSpawnDone = true;

            // 초기 스폰: 5사이클 분을 큐에 쌓고 자리까지만 채움
            waiting += 5;
            TrySpawnFromQueue();
            return;
        }

        tick++;
        int interval = instance.data.productionInterval;
        if (interval <= 0) interval = 3;

        if (tick < interval) return;
        tick = 0;

        // 사이클 1개 발동 → 큐에 쌓고 가능한 만큼 즉시 채움
        waiting++;
        TrySpawnFromQueue();
    }

    // 자리가 날 때마다 pendingSlot 순서대로 1명씩 스폰
    private void TrySpawnFromQueue()
    {
        int per = Mathf.Max(1, instance.data.unitsPerCycle);

        while (waiting > 0 && activeCount < Capacity)
        {
            SpawnUnit(pendingSlot);
            activeCount++;
            pendingSlot++;

            if (pendingSlot >= per)
            {
                pendingSlot = 0;
                waiting--;
            }
        }
    }

    // 해당 건물 소속 유닛이 죽었을 때 외부에서 호출.
    public void NotifyUnitDied()
    {
        if (activeCount > 0) activeCount--;
        TrySpawnFromQueue();
    }

    // 자식 클래스가 실제 유닛 인스턴스를 생성/등록.
    // slotInCycle: 사이클 내 인덱스. 단일 유닛 건물이면 항상 0.
    //              쌍 건물이면 0=앞유닛(예: 궁수/기사), 1=뒷유닛(예: 힐러/기마병).
    protected abstract void SpawnUnit(int slotInCycle);

    public override BuildingRuntimeState SaveState()
    {
        return new BuildingRuntimeState
        {
            tick         = tick,
            activeCount  = activeCount,
            waiting      = waiting,
            pendingSlot  = pendingSlot
        };
    }

    public override void LoadState(BuildingRuntimeState state)
    {
        if (state == null) return;
        tick         = state.tick;
        activeCount  = state.activeCount;
        waiting      = state.waiting;
        pendingSlot  = state.pendingSlot;
    }
}
