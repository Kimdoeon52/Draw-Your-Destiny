using UnityEngine;

// 농장 — 농부 유닛 생산. 매 턴 활성 농부 수 × n 만큼 식량도 생산.
public class FarmBehaviour : UnitProducerBehaviour
{
    FarmerPool farmPool;

    private void Awake()
    {
        farmPool = GetComponent<FarmerPool>();
    }

    public override void OnTurnEnd()
    {
        if (!instance.isActive) return;
        base.OnTurnEnd(); // 농부 생산 처리
        ResourceManager.Instance.AddFood(activeCount * instance.data.foodPerTurn);
        Debug.Log($"[Farm] 식량 생산 예정 — 활성 농부={activeCount}");
    }

    protected override void SpawnUnit(int slotInCycle)
    {
        farmPool.GetHuman(0);
        Debug.Log($"[Farm] 농부 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}
