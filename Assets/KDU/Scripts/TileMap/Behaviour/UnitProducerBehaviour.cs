using UnityEngine;

// ============================================================
// UnitProducerBehaviour — 유닛을 생산하는 건물의 공통 추상 베이스
//
// 병영 / 농장 / 상점 등이 상속.
// productionInterval 턴마다 SpawnUnit() 호출.
// 수용치(unitCapacity) 초과 시 waiting 카운터만 증가.
// 외부에서 유닛 사망 시 NotifyUnitDied() 호출 → 대기열에서 즉시 재보충.
// ============================================================
public abstract class UnitProducerBehaviour : BuildingBehaviour
{
    protected int tick;
    protected int activeCount;
    protected int waiting;

    public int ActiveCount  => activeCount;
    public int WaitingCount => waiting;
    public int Capacity     => instance?.data != null ? instance.data.unitCapacity : 0;

    public override void OnTurnEnd()
    {
        tick++;
        int interval = instance.data.productionInterval;
        if (interval <= 0) interval = 3;

        if (tick < interval) return;
        tick = 0;

        TryProduce();
    }

    private void TryProduce()
    {
        if (activeCount < Capacity)
        {
            SpawnUnit();
            activeCount++;
        }
        else
        {
            waiting++;
        }
    }

    // 해당 건물 소속 유닛이 죽었을 때 외부에서 호출.
    // 유닛 시스템 구현 후 unit.homeBuilding?.behaviour?.NotifyUnitDied() 형태로 연결.
    public void NotifyUnitDied()
    {
        if (activeCount > 0) activeCount--;

        if (waiting > 0 && activeCount < Capacity)
        {
            waiting--;
            SpawnUnit();
            activeCount++;
        }
    }

    // 자식 클래스가 실제 유닛 인스턴스를 생성/등록 (유닛 시스템 구현 후 채움)
    protected abstract void SpawnUnit();

    public override BuildingRuntimeState SaveState()
    {
        return new BuildingRuntimeState
        {
            tick        = tick,
            activeCount = activeCount,
            waiting     = waiting
        };
    }

    public override void LoadState(BuildingRuntimeState state)
    {
        if (state == null) return;
        tick        = state.tick;
        activeCount = state.activeCount;
        waiting     = state.waiting;
    }
}
