namespace NYH.BattleCardSystem
{
    /// <summary>
    /// 전투 덱에 보상 카드를 추가하거나 교체하려고 했을 때의 결과 코드입니다.
    ///
    /// 사용 예:
    /// - Added: 그냥 정상 추가됨
    /// - NeedsReplacement: 덱 제한 때문에 교체 대상을 먼저 골라야 함
    /// - Replaced: 기존 카드 하나를 빼고 새 카드로 교체 완료
    /// - Invalid: 입력 카드가 null이거나, 교체 대상이 덱에 없거나, 규칙상 잘못된 요청
    /// </summary>
    public enum BattleDeckAddResult
    {
        Added,
        Replaced,
        NeedsReplacement,
        Invalid,
    }
}
