using System;

// ============================================================
// SaveCardEntry — 카드 1장의 저장용 데이터
//
// CardData SO 참조는 cardId(int)로 치환.
// 로드 시 CardData를 cardId로 조회하여 복원.
// ============================================================
[Serializable]
public class SaveCardEntry
{
    // CardData.cardID — SO 참조 대체
    public int cardId;

    // 현재 코스트 (버프/디버프로 변동된 값)
    public int currentCost;
}
