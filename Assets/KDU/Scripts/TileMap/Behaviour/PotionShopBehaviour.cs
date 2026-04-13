using UnityEngine;

// 포션 가게 (PotionBuilding) — 매 턴 덱에 랜덤 포션 카드 1장 추가 (청동기~)
// 유닛 생산 없음. BuildingBehaviour 직접 상속.
public class PotionShopBehaviour : BuildingBehaviour
{
    public override void OnTurnEnd()
    {
        // TODO: 카드 시스템 연동 후 카드 풀에서 랜덤 포션 카드를 덱에 추가
        // CardSystem.Instance?.AddRandomCardToDeck(CardTag.Potion);
        Debug.Log("[PotionShop] 포션 카드 추가 예정");
    }
}
