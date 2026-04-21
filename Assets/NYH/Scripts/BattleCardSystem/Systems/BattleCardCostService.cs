namespace NYH.BattleCardSystem
{
    using UnityEngine;

    /// <summary>
    /// 전투 카드 AP 비용 계산과 지불만 담당합니다.
    /// 카드 더미 이동, 액션 실행, UI 갱신은 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleCardCostService
    {
        private readonly int maxActionPoints;

        public BattleCardCostService(int maxActionPoints)
        {
            this.maxActionPoints = Mathf.Max(0, maxActionPoints);
        }

        public int CurrentActionPoints { get; private set; }

        public void Setup(int actionPoints)
        {
            CurrentActionPoints = Mathf.Clamp(actionPoints, 0, maxActionPoints);
        }

        public int Add(int amount)
        {
            int before = CurrentActionPoints;
            CurrentActionPoints = Mathf.Clamp(CurrentActionPoints + amount, 0, maxActionPoints);
            return CurrentActionPoints - before;
        }

        public bool CanAfford(BattleCard card)
        {
            return card != null && CurrentActionPoints >= Mathf.Max(0, card.CurrentCost);
        }

        public (bool paidByActionPoints, int actionPointsSpent, int healthPenalty) TryPay(BattleCard card)
        {
            if (card == null)
            {
                return (false, 0, 0);
            }

            int cost = Mathf.Max(0, card.CurrentCost);
            if (CurrentActionPoints < cost)
            {
                return (false, 0, 0);
            }

            CurrentActionPoints -= cost;
            return (true, cost, 0);
        }
    }
}
