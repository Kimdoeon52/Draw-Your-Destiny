using UnityEngine;

// 덫 공방 (TrapWorkshop) — 매 턴 덱에 랜덤 덫 카드 1장 추가 (청동기~)
// 유닛 생산 없음. BuildingBehaviour 직접 상속.
public class TrapWorkshopBehaviour : BuildingBehaviour
{
    public override void OnTurnEnd()
    {
        // TODO: 카드 시스템 연동 후 카드 풀에서 랜덤 덫 카드를 덱에 추가
        // CardSystem.Instance?.AddRandomCardToDeck(CardTag.Trap);
        Debug.Log("[TrapWorkshop] 덫 카드 추가 예정");
    }
}
