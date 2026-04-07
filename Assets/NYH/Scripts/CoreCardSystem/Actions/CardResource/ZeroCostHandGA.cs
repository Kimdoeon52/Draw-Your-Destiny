namespace NYH.CoreCardSystem
{
    public class ZeroCostHandGA : GameAction
    {
        public int CardAmount { get; private set; }
        public int CostAmount { get; private set; }

        // 생성자 매개변수 순서를 Effect와 통일 (cost, amount)
        public ZeroCostHandGA(int costAmount, int cardAmount)
        {
            this.CostAmount = costAmount;
            this.CardAmount = cardAmount;
        }
    }
}
