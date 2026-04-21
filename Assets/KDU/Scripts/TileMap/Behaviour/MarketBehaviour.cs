using UnityEngine;

// 상점 — 상인 유닛 생산. 매 턴 활성 상인 수 × n 만큼 금도 생산.
public class MarketBehaviour : UnitProducerBehaviour
{
    ShoperPool shoperPool;

    private void Awake()
    {
        shoperPool = GetComponent<ShoperPool>();
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd(); // 상인 생산 처리

        // TODO: goldPerTurn 확정 후 수치 채움
        // ResourceManager.Instance.AddGold(activeCount * goldPerMerchant);
        GameManager.Instance.AddGold(activeCount * instance.data.goldPerTurn);
        Debug.Log($"[Market] 금 생산 예정 — 활성 상인={activeCount}");
    }

    protected override void SpawnUnit(int slotInCycle)
    {
        // TODO: 유닛 시스템 구현 후 UnitManager.Instance.Spawn(UnitType.Merchant, instance); 로 교체
        shoperPool.GetHuman(0);
        Debug.Log($"[Market] 상인 생산 — active={activeCount + 1}/{Capacity}, waiting={waiting}");
    }
}
