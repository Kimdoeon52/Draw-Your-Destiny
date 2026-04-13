using UnityEngine;

// 연구소 — 유닛 생산 없음. 매 턴 연구 포인트만 생성.
public class LabBehaviour : BuildingBehaviour
{
    public override void OnTurnEnd()
    {
        if (instance?.data == null) return;
        if (!instance.isActive) return; // 같은 타입 건물이 다른 노드에 이미 활성화 중
        int amount = instance.data.researchPerTurn;
        ResourceManager.Instance?.AddResearch(amount);
        Debug.Log($"[Lab] 연구 포인트 +{amount}");
    }
}
