using System;
using System.Collections.Generic;

// ============================================================
// SaveCardState — 카드 덱 전체의 저장용 데이터
//
// 문명 카드 / 전투 카드 각각 하나씩 사용.
// 드로우 파일, 손패, 버린 더미, 소멸 더미를 모두 포함.
// ============================================================
[Serializable]
public class SaveCardState
{
    public List<SaveCardEntry> drawPile = new List<SaveCardEntry>();
    public List<SaveCardEntry> hand = new List<SaveCardEntry>();
    public List<SaveCardEntry> discardPile = new List<SaveCardEntry>();
    public List<SaveCardEntry> extinctionPile = new List<SaveCardEntry>();
}
