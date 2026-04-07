namespace NYH.CoreCardSystem
{
    /// <summary>
    /// 카드 뽑기(Draw) 동작을 정의하는 데이터 클래스입니다.
    /// "카드 몇 장을 뽑을 것인가"라는 정보만 가지고 있습니다.
    /// </summary>
    public class DrawCardsGA : GameAction
    {
        // 뽑을 카드의 수
        public int Amount { get; private set; }

        /// <summary>
        /// 생성자를 통해 몇 장을 뽑을지 결정합니다.
        /// 예: new DrawCardsGA(2) -> 2장 뽑는 액션 생성
        /// </summary>
        public DrawCardsGA(int amount)
        {
            Amount = amount;
        }
    }
}
