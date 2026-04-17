namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;

    /// <summary>
    /// 시작 멀리건 결과를 담는 데이터입니다.
    /// 유지 카드, 되돌린 카드, 새로 뽑은 카드를 나눠서 UI 연출에 사용합니다.
    /// </summary>
    public sealed class BattleMulliganResult
    {
        public List<BattleCard> KeptCards { get; } = new();
        public List<BattleCard> ReturnedCards { get; } = new();
        public List<BattleCard> RedrawnCards { get; } = new();
    }
}
