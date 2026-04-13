using UnityEngine;

// 농장 — 농부 유닛 생산. 매 턴 활성 농부 수 × n 만큼 식량도 생산.
public class FarmBehaviour : UnitProducerBehaviour
{
    public override void OnTurnEnd()
    {
        base.OnTurnEnd(); // 농부 생산 처리

        // TODO: 식량 생산량 확정 후 수치 채움
        // ResourceManager.Instance.AddFood(activeCount * foodPerFarmer);
        Debug.Log($"[Farm] 식량 생산 예정 — 활성 농부={activeCount}");
    }

    protected override void SpawnUnit()
    {
        // TODO: 유닛 시스템 구현 후 UnitManager.Instance.Spawn(UnitType.Farmer, instance); 로 교체
        Debug.Log($"[Farm] 농부 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}
